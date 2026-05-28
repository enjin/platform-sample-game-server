using System.Numerics;
using System.Security.Claims;
using PlatformSampleGameServer.Models;
using PlatformSampleGameServer.Services;

namespace PlatformSampleGameServer.Endpoints;

public static class TokenEndpoints
{
    public static void MapTokenEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/token").RequireAuthorization();

        group.MapPost(
            "/mint",
            async (
                MintRequest req,
                ClaimsPrincipal user,
                EnjinService enjin,
                CancellationToken ct
            ) =>
                await Run(
                    "mint",
                    req.TokenId,
                    req.Amount,
                    user,
                    async (tokenId, amount, email) =>
                    {
                        // Mint to the player's wallet. The daemon signs implicitly.
                        var address = await enjin.EnsureManagedWalletAsync(email, ct);
                        await enjin.MintTokenAsync(tokenId, amount, address, ct);
                    }
                )
        );

        group.MapPost(
            "/melt",
            async (
                MeltRequest req,
                ClaimsPrincipal user,
                EnjinService enjin,
                CancellationToken ct
            ) =>
                await Run(
                    "melt",
                    req.TokenId,
                    req.Amount,
                    user,
                    async (tokenId, amount, email) =>
                    {
                        // Burn from the player's wallet; daemon signs on their behalf via externalId.
                        await enjin.MeltTokenAsync(tokenId, amount, email, ct);
                    }
                )
        );

        group.MapPost(
            "/transfer",
            async (
                TransferRequest req,
                ClaimsPrincipal user,
                EnjinService enjin,
                CancellationToken ct
            ) =>
                await Run(
                    "transfer",
                    req.TokenId,
                    req.Amount,
                    user,
                    async (tokenId, amount, email) =>
                    {
                        if (string.IsNullOrWhiteSpace(req.Recipient))
                            throw new ArgumentException("Recipient is required.");
                        await enjin.TransferTokenAsync(tokenId, amount, req.Recipient, email, ct);
                    }
                )
        );
    }

    private static async Task<IResult> Run(
        string operation,
        string tokenIdString,
        int amount,
        ClaimsPrincipal user,
        Func<BigInteger, BigInteger, string, Task> action
    )
    {
        var email = user.FindFirst(AuthService.EmailClaim)?.Value;
        if (string.IsNullOrEmpty(email))
            return Results.Unauthorized();

        if (!BigInteger.TryParse(tokenIdString, out var tokenId))
            return Results.Json(
                new BoolResponse(false, $"Invalid tokenId '{tokenIdString}'."),
                statusCode: 400
            );
        if (amount <= 0)
            return Results.Json(
                new BoolResponse(false, "Amount must be positive."),
                statusCode: 400
            );

        try
        {
            await action(tokenId, new BigInteger(amount), email);
            return Results.Ok(new BoolResponse(true));
        }
        catch (Exception ex)
        {
            return Results.Json(new BoolResponse(false, ex.Message), statusCode: 500);
        }
    }
}
