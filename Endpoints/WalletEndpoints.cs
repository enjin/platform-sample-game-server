using System.Security.Claims;
using PlatformSampleGameServer.Models;
using PlatformSampleGameServer.Services;

namespace PlatformSampleGameServer.Endpoints;

public static class WalletEndpoints
{
    public static void MapWalletEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/wallet").RequireAuthorization();

        // GET /api/wallet/get-tokens - returns the player's managed wallet account
        // plus the tokens they hold in our collection. Shape matches the Unity
        // client's PlatformModels.ManagedWalletAccount.
        group.MapGet(
            "/get-tokens",
            async (ClaimsPrincipal user, EnjinService enjin, CancellationToken ct) =>
            {
                var email = user.FindFirst(AuthService.EmailClaim)?.Value;
                if (string.IsNullOrEmpty(email))
                    return Results.Unauthorized();

                try
                {
                    var account = await enjin.GetManagedWalletTokensAsync(email, ct);
                    if (account is null)
                    {
                        return Results.Json(
                            new BoolResponse(false, $"No managed wallet for {email}"),
                            statusCode: 404
                        );
                    }
                    return Results.Ok(account);
                }
                catch (Exception ex)
                {
                    return Results.Json(new BoolResponse(false, ex.Message), statusCode: 500);
                }
            }
        );
    }
}
