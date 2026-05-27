# Platform Sample Game Server (C#)

REST backend for the **Enjin Farmer** Unity sample
([platform-sample-game-client-unity](https://github.com/enjin/platform-sample-game-client-unity)),
demonstrating how to integrate NFTs into a game using the
[Enjin Platform C# SDK](https://github.com/enjin/platform-csharp-sdk).

This is a rewrite of the previous Node.js server in .NET 9. The wire format
(JSON shape, route paths, JWT handling) is preserved so the existing Unity
client works unchanged.

## What this server does

- Holds the Enjin Platform API key. The Unity client never sees it.
- Bootstraps an on-chain **collection** and a fixed set of **resource tokens**
  the game uses (Gold Coin, Gold Coin Blue, Green Gem) on first run.
- Issues players a JWT after `/api/auth/register` so subsequent calls can
  identify them.
- Creates a managed wallet per player (one managed wallet per `externalId`,
  where `externalId == email`) and drips a small amount of ENJ to it so it
  can pay transaction fees.
- Brokers mint / melt / transfer mutations to the platform and waits for
  on-chain finalization before responding to the client.

## Prerequisites

- [.NET 9 SDK](https://dotnet.microsoft.com/download).
- The [Enjin Platform C# SDK](https://github.com/enjin/platform-csharp-sdk)
  v3.0.0 or later, pulled from NuGet automatically on `dotnet restore`.
  (For local development against an unreleased SDK, swap the
  `PackageReference` in `PlatformSampleGameServer.csproj` for the commented-out
  `ProjectReference` and check the SDK out as a sibling directory.)
- An Enjin Platform account and a generated API token.
- A running [Wallet Daemon](https://docs.enjin.io/products/wallet-daemon)
  configured with the same API token; its SS58 address is what you'll set as
  `DaemonWalletAddress` below.
- cENJ in the daemon's wallet (use the
  [Canary Faucet](https://faucet.canary.enjin.io/)) to pay for collection
  creation, mints, and drips to new players.

## Setup

1. **Clone the repo:**

   ```bash
   git clone https://github.com/enjin/platform-sample-game-server.git
   cd platform-sample-game-server
   ```

2. **Create a local config:**

   Copy `appsettings.Sample.json` to `appsettings.Local.json` (gitignored)
   and fill in:

   - `Jwt.Secret` &mdash; any long random string (32+ chars).
   - `Enjin.ApiToken` &mdash; your platform API token.
   - `Enjin.DaemonWalletAddress` &mdash; the SS58 address of your running
     wallet daemon, on the same network/chain (default Canary Matrix; uses
     SS58 prefix 9030).

   You can leave the rest of `appsettings.json` alone. Notable defaults:

   | Setting | Default | Meaning |
   |---|---|---|
   | `Server.Port` | `3000` | HTTP listen port. The Unity client expects 3000 by default. |
   | `Enjin.ApiUrl` | Canary GraphQL | Switch to production when you ship. |
   | `Enjin.Network` / `Enjin.Chain` | `Canary` / `Matrix` | |
   | `Enjin.ResourceTokens` | three entries | The Unity client has matching `EnjinItem` assets for `Id` 1, 2, 3. |
   | `Enjin.CollectionName` | `Enjin Sample Game` | Used to find or reuse an existing collection so you don't create a new one every run. |
   | `Enjin.Ss58Prefix` | `9030` | Matrix prefix; change if you target a different chain. |
   | `Enjin.DripEnjEnabled` / `Enjin.DripEnjAmount` | `true` / `"1"` | Each new managed wallet gets 1 ENJ from the daemon so it can pay fees. |

3. **Run the server:**

   ```bash
   dotnet run
   ```

   On first launch the server:

   1. Looks for an existing collection owned by the daemon and named
      `Enjin.CollectionName`. If absent, creates one and waits for the
      `CreateCollection` transaction to finalize (~10&ndash;20s).
   2. Creates each entry in `Enjin.ResourceTokens` as a token in that
      collection. (Skipped on subsequent runs.)
   3. Persists the resulting `collectionId` to `state.json` so subsequent
      runs reuse it.

   When you see `Server listening on http://0.0.0.0:3000` it's ready.

4. **Stamp the collection ID into the Unity client.**

   In the Unity Editor, run:

   > Enjin &rarr; Stamp Collection ID onto EnjinItem Assets

   The Editor calls `GET /api/setup/collection-id`, then writes the value
   onto every `EnjinItem` ScriptableObject (Gold Coin, Gold Coin Blue,
   Green Gem). Commit those `.asset` files. **Do this once after first
   server bootstrap, and again if the canary state ever resets and forces a
   new collection.**

   If you skip this step, the Unity backpack UI will show no tokens because
   it filters by `(collectionId, tokenId)` against your wallet's holdings.

## Optional flags

- `dotnet run -- --skip-bootstrap` &mdash; start the HTTP server without
  trying to allocate a collection. Useful when canary is misbehaving and you
  just want to inspect requests.

## API endpoints

| Method | Path | Auth | Purpose |
|---|---|---|---|
| GET  | `/api/auth/health-check` | none | Liveness probe. |
| POST | `/api/auth/register`     | none | Register *or* log in (same call). Returns a JWT. |
| GET  | `/api/wallet/get-tokens` | JWT  | Player's managed wallet + the resource-token balances they hold. |
| POST | `/api/token/mint`        | JWT  | Daemon-signed mint of `tokenId` into the caller's managed wallet. |
| POST | `/api/token/melt`        | JWT  | Player-signed burn of `tokenId` from the caller's managed wallet. |
| POST | `/api/token/transfer`    | JWT  | Player-signed transfer of `tokenId` to an SS58 address. |
| GET  | `/api/setup/collection-id` | none | One-shot read used by the Unity Editor setup menu. Returns `{"collectionId":"..."}`. |

Every JWT-protected route reads the `email` claim (the player identifier)
and uses it as the `externalId` when talking to the Enjin Platform's
managed-wallet APIs.

## Source map

| Path | Purpose |
|---|---|
| `Program.cs` | Host setup, DI registration, JWT configuration, bootstrap orchestration, port binding. |
| `Services/EnjinService.cs` | Every SDK call: collection bootstrap, managed wallet resolution, mint/melt/transfer, transaction polling, ENJ drip. Also defines `ServerState`, the persisted state (`state.json`) holding the collection id and the set of `externalId`s already dripped. |
| `Services/AuthService.cs` | Bcrypt password hashing, JWT issuance (`sub` + `email` claims). |
| `Services/SubstrateAddress.cs` | SS58 encoder (Blake2b + base58check) used to convert managed-wallet public keys returned by the platform into SS58 addresses. |
| `Services/Options.cs` | Strongly-typed config classes bound from `appsettings*.json`. |
| `Endpoints/AuthEndpoints.cs` | `/api/auth/*` minimal-API routes. |
| `Endpoints/WalletEndpoints.cs` | `/api/wallet/*` routes. |
| `Endpoints/TokenEndpoints.cs` | `/api/token/*` routes. |
| `Endpoints/SetupEndpoints.cs` | `/api/setup/*` routes (called by Unity Editor tooling). |
| `Models/Dtos.cs` | Request/response records (`*Dto`, `*Request`, `*Response`). |
| `tools/Ss58SelfTest/` | Tiny console app that verifies the SS58 encoder against published Polkadot/Substrate test vectors. Run with `dotnet run --project tools/Ss58SelfTest`. |

`appsettings.Local.json`, `state.json`, and `bin/` `obj/` are gitignored.

## Further reading

- [Enjin Platform C# SDK](https://github.com/enjin/platform-csharp-sdk)
- [Enjin Farmer setup guide](https://docs.enjin.io/guides/platform/enjin-farmer-sample-game/setup-guide)
- [Implementation breakdown](https://docs.enjin.io/guides/platform/enjin-farmer-sample-game/implementation-breakdown)
