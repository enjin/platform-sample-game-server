using System.IdentityModel.Tokens.Jwt;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using PlatformSampleGameServer.Endpoints;
using PlatformSampleGameServer.Services;

var builder = WebApplication.CreateBuilder(args);

// Don't remap inbound JWT claim names (e.g. "email" -> long XMLSOAP URI).
// We want claims to round-trip with their original short names so endpoints
// can read them by their well-known JWT names.
JwtSecurityTokenHandler.DefaultMapInboundClaims = false;

// ----- Configuration -----
builder.Configuration.AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: true);
builder.Configuration.AddEnvironmentVariables();

builder.Services.Configure<ServerOptions>(builder.Configuration.GetSection("Server"));
builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection("Jwt"));
builder.Services.Configure<EnjinOptions>(builder.Configuration.GetSection("Enjin"));

// ----- Services -----
builder.Services.AddSingleton<UserStore>();
builder.Services.AddSingleton<AuthService>();
builder.Services.AddSingleton<ServerState>();
builder.Services.AddSingleton<EnjinService>();

builder.Services.AddCors(o =>
    o.AddDefaultPolicy(p => p.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod())
);

// JSON: camelCase property names + serialize BigInteger-as-string so the
// Unity client's SerializableBigInteger wrapper can parse it. Records with
// PascalCase properties will be emitted as camelCase by default; this is
// what the Unity client expects (its JsonUtility uses the field name verbatim,
// and its fields are camelCase).
builder.Services.ConfigureHttpJsonOptions(o =>
{
    o.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    o.SerializerOptions.PropertyNameCaseInsensitive = true;
});

// ----- JWT auth -----
var jwt = builder.Configuration.GetSection("Jwt").Get<JwtOptions>() ?? new JwtOptions();
if (string.IsNullOrWhiteSpace(jwt.Secret))
{
    // Emit a generated dev secret if none is configured so the server still boots
    // for local development. Production must set Jwt:Secret explicitly.
    jwt.Secret = Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N");
    Console.WriteLine(
        "[warn] Jwt:Secret not configured; generated a transient dev secret. "
            + "Set Jwt:Secret in appsettings or env for stable sessions across restarts."
    );

    // Propagate the generated secret into the bound JwtOptions so AuthService
    // (which resolves IOptions<JwtOptions>) signs tokens with the same key that
    // the JwtBearer middleware validates against. Without this, AuthService
    // would either throw on an empty Secret or sign with a different key.
    builder.Services.PostConfigure<JwtOptions>(o =>
    {
        if (string.IsNullOrWhiteSpace(o.Secret))
            o.Secret = jwt.Secret;
    });
}

builder
    .Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opts =>
    {
        opts.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = !string.IsNullOrEmpty(jwt.Issuer),
            ValidIssuer = jwt.Issuer,
            ValidateAudience = !string.IsNullOrEmpty(jwt.Audience),
            ValidAudience = jwt.Audience,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Secret)),
            ClockSkew = TimeSpan.FromMinutes(1),
        };
    });
builder.Services.AddAuthorization();

// ----- Kestrel: port + request timeout -----
var serverOptions =
    builder.Configuration.GetSection("Server").Get<ServerOptions>() ?? new ServerOptions();
builder.WebHost.ConfigureKestrel(k =>
{
    k.ListenAnyIP(serverOptions.Port);
});

// Long-running on-chain operations (mint/melt/transfer) can legitimately take
// several minutes while we poll for finalization. Configure a per-request
// execution timeout via the ASP.NET Core RequestTimeouts middleware so the
// server cancels requests that exceed Server:RequestTimeoutSeconds instead of
// letting them run indefinitely. (KeepAliveTimeout is intentionally left at
// its default; it controls idle connection lifetime, not request duration.)
builder.Services.AddRequestTimeouts(o =>
{
    o.DefaultPolicy = new Microsoft.AspNetCore.Http.Timeouts.RequestTimeoutPolicy
    {
        Timeout = TimeSpan.FromSeconds(serverOptions.RequestTimeoutSeconds),
    };
});

var app = builder.Build();

app.UseCors();
app.UseRequestTimeouts();
app.UseAuthentication();
app.UseAuthorization();

// ----- Routes -----
app.MapAuthEndpoints();
app.MapWalletEndpoints();
app.MapTokenEndpoints();
app.MapSetupEndpoints();

// ----- Bootstrap the collection + resource tokens before serving any requests -----
// Pass --skip-bootstrap (or set Enjin:SkipBootstrap=true) to start the server
// without creating/verifying the on-chain collection. Useful for local smoke tests
// where you don't want to mutate canary.
var skipBootstrap =
    args.Contains("--skip-bootstrap")
    || string.Equals(
        builder.Configuration["Enjin:SkipBootstrap"],
        "true",
        StringComparison.OrdinalIgnoreCase
    );

using (var scope = app.Services.CreateScope())
{
    var sp = scope.ServiceProvider;
    var state = sp.GetRequiredService<ServerState>();
    // Allow the operator to seed state.json on first run by supplying a
    // collection id via configuration. Configuration:AddEnvironmentVariables
    // accepts the standard double-underscore form (Enjin__CollectionId), but
    // the legacy Node.js sample used the flat ENJIN_COLLECTION_ID name, so
    // we accept that too as a fallback for operators migrating from the old
    // server.
    var collectionIdSeed =
        builder.Configuration["Enjin:CollectionId"]
        ?? Environment.GetEnvironmentVariable("ENJIN_COLLECTION_ID");
    state.OverrideFromConfig(collectionIdSeed);

    var log = sp.GetRequiredService<ILogger<Program>>();

    if (skipBootstrap)
    {
        log.LogWarning(
            "Bootstrap skipped (--skip-bootstrap). On-chain operations may fail until a collection ID is provided."
        );
    }
    else
    {
        var enjin = sp.GetRequiredService<EnjinService>();
        log.LogInformation(
            "Preparing collection and resource tokens. This may take a few minutes on first run."
        );
        try
        {
            await enjin.PrepareCollectionAsync(CancellationToken.None);
        }
        catch (Exception ex)
        {
            log.LogCritical(ex, "Failed to prepare collection. Server will not start.");
            return;
        }
    }

    log.LogInformation("----------------------------------------");
    log.LogInformation("Collection ID: {Id}", state.CollectionId?.ToString() ?? "(unset)");
    log.LogInformation("Server listening on http://0.0.0.0:{Port}", serverOptions.Port);
    log.LogInformation("----------------------------------------");
}

await app.RunAsync();
