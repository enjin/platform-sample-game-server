using Enjin.Platform.Sdk;

namespace PlatformSampleGameServer.Services;

// Strongly-typed configuration. Bound to the "Enjin" section of appsettings.json
// (and overrides) via IOptions<EnjinOptions>.
public sealed class EnjinOptions
{
    public string ApiUrl { get; set; } = "";
    public string ApiToken { get; set; } = "";

    // Default to the Canary test network on Matrix relay. Override per environment.
    public Network Network { get; set; } = Network.Canary;
    public Chain Chain { get; set; } = Chain.Matrix;

    // Recipient used when minting the initial supply for resource tokens (the
    // daemon wallet that holds the master copy). Per-player mints use the
    // player's managed-wallet address as recipient.
    public string DaemonWalletAddress { get; set; } = "";

    public int TransactionPollIntervalSeconds { get; set; } = 10;
    public int TransactionInitialDelaySeconds { get; set; } = 10;
    public int ManagedWalletPollIntervalSeconds { get; set; } = 1;
    public int ManagedWalletPollMaxAttempts { get; set; } = 10;

    public List<ResourceTokenDefinition> ResourceTokens { get; set; } = new();

    public string CollectionName { get; set; } = "";
    public string CollectionBannerImage { get; set; } = "";
    public string CollectionMedia { get; set; } = "";
}

public sealed class ResourceTokenDefinition
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Media { get; set; } = "";
}

public sealed class JwtOptions
{
    public string Issuer { get; set; } = "";
    public string Audience { get; set; } = "";
    public string Secret { get; set; } = "";
    public int ExpiryHours { get; set; } = 24;
}

public sealed class ServerOptions
{
    public int Port { get; set; } = 3000;
    public int RequestTimeoutSeconds { get; set; } = 3600;
}
