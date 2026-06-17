namespace PlatformSampleGameServer.Models;

// Wire-format DTOs. Field names use camelCase via System.Text.Json
// default policy + explicit JsonPropertyName where needed, to match the
// shapes the Unity client (Assets/Enjin Integration/Scripts/...) expects.
// BigInteger-valued fields are serialized as decimal strings so the
// client's SerializableBigInteger wrapper can parse them.

// ---- Request bodies ----

public sealed record AuthRequest(string Email, string Password);

public sealed record MintRequest(string TokenId, string Amount);

public sealed record MeltRequest(string TokenId, string Amount);

public sealed record TransferRequest(string TokenId, string Amount, string Recipient);

// ---- Response bodies ----

public sealed record HealthCheckResponse(string Status);

public sealed record AuthResponse(string Email, string? Wallet, string Token);

public sealed record BoolResponse(bool Success, string? Message = null);

// Mirrors PlatformModels.ManagedWalletAccount on the Unity side.
public sealed record ManagedWalletAccountDto(
    AccountDto Account,
    IReadOnlyList<TokenAccountDto> TokenAccounts
);

// Returned by GET /api/setup/collection-id. The Unity Editor calls this once
// during studio setup to stamp the on-chain collection id onto each
// EnjinItem ScriptableObject; the running game itself never calls it. The
// server allocates the collection during bootstrap (Program.cs) and persists
// the id in state.json, so this endpoint is a stable read after that.
public sealed record CollectionIdResponse(string CollectionId);

public sealed record AccountDto(string PublicKey, string Address);

public sealed record TokenAccountDto(string Balance, TokenDto Token);

public sealed record TokenDto(
    CollectionDto Collection,
    string TokenId,
    IReadOnlyList<AttributeDto> Attributes
);

public sealed record CollectionDto(string CollectionId);

public sealed record AttributeDto(string Key, string Value);
