using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using PlatformSampleGameServer.Models;
using PlatformSampleGameServer.Services;

namespace PlatformSampleGameServer.Endpoints;

public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/auth");

        // Anonymous - the Unity client polls this to confirm connectivity.
        group
            .MapGet("/health-check", () => Results.Ok(new HealthCheckResponse("OK")))
            .AllowAnonymous();

        // Register-or-login. The Unity client uses this single endpoint for both
        // first-time signup and returning sessions; behaviour matches the original
        // Node.js sample (src/routes/auth.js).
        group
            .MapPost(
                "/register",
                async (
                    AuthRequest body,
                    AuthService auth,
                    EnjinService enjin,
                    ILoggerFactory loggerFactory,
                    CancellationToken ct
                ) =>
                {
                    var log = loggerFactory.CreateLogger(
                        "PlatformSampleGameServer.Endpoints.AuthEndpoints"
                    );
                    try
                    {
                        var (token, email) = auth.RegisterOrLogin(body.Email, body.Password);

                        // Ensure a managed wallet exists for this player so subsequent
                        // operations (mint/melt/transfer) can target it immediately.
                        string? wallet = null;
                        try
                        {
                            wallet = await enjin.EnsureManagedWalletAsync(email, ct);
                        }
                        catch (Exception ex)
                        {
                            // Don't fail registration on a wallet-provisioning hiccup;
                            // the client can retry against /api/wallet/get-tokens later.
                            wallet = null;
                            log.LogWarning(ex, "EnsureManagedWallet failed for {Email}", email);
                        }

                        return Results.Ok(new AuthResponse(email, wallet, token));
                    }
                    catch (UnauthorizedAccessException ex)
                    {
                        return Results.Json(new BoolResponse(false, ex.Message), statusCode: 401);
                    }
                    catch (Exception ex)
                    {
                        return Results.Json(new BoolResponse(false, ex.Message), statusCode: 400);
                    }
                }
            )
            .AllowAnonymous();
    }
}
