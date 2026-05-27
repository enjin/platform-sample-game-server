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

    /// <summary>
    /// SS58 prefix used to encode wallet public keys into addresses. Defaults to
    /// 9030 (Enjin Matrixchain Canary). Use 1110 for Enjin Mainnet Matrixchain.
    /// </summary>
    public ushort Ss58Prefix { get; set; } = 9030;

    /// <summary>
    /// If true, the server transfers <see cref="DripEnjAmount"/> ENJ from the daemon
    /// wallet to every newly created managed wallet exactly once. This is required
    /// because the platform exposes no fuel-tank API on canary; managed wallets need
    /// their own ENJ to cover transaction fees and storage reserves (e.g. for token
    /// holding records minted into them).
    /// </summary>
    public bool DripEnjEnabled { get; set; } = true;

    /// <summary>
    /// Amount of ENJ to drip to each new managed wallet, expressed in WHOLE ENJ
    /// (integer; the platform scales by 10^18 on-chain). Default 1.
    /// Note: the TransferEnj mutation does not support fractional amounts.
    /// </summary>
    public string DripEnjAmount { get; set; } = "1";

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
