using PlatformSampleGameServer.Models;
using PlatformSampleGameServer.Services;

namespace PlatformSampleGameServer.Endpoints;

/// <summary>
/// Setup-time endpoints. Called by the Unity Editor (and any other tooling)
/// once when standing the game up against a new server / new canary state, to
/// bake server-allocated identifiers into client-side configuration assets.
///
/// Intentionally unauthenticated: setup happens before any player account
/// exists, the values exposed here are not secret (they end up in shipped
/// client builds anyway), and gating this behind a JWT would be a chicken-
/// and-egg problem for fresh installs.
/// </summary>
public static class SetupEndpoints
{
    public static void MapSetupEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/setup");

        // GET /api/setup/collection-id - returns the on-chain collection id
        // this server has bootstrapped. The Unity Editor menu "Enjin > Stamp
        // Collection ID onto EnjinItem Assets" calls this and writes the
        // value onto every EnjinItem ScriptableObject.
        group.MapGet("/collection-id", (EnjinService enjin) =>
        {
            var id = enjin.CollectionId;
            if (id is null)
            {
                return Results.Json(
                    new BoolResponse(false, "Collection id not initialised. " +
                                            "Did the server finish bootstrap?"),
                    statusCode: 503);
            }
            return Results.Ok(new CollectionIdResponse(id.Value.ToString()));
        });
    }
}
