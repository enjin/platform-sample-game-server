using System.Numerics;
using System.Text.Json;
using Enjin.Platform.Sdk;
using Microsoft.Extensions.Options;

namespace PlatformSampleGameServer.Services;

// All Enjin Platform interaction lives in this single service so the sample is
// easy to read top-to-bottom.
//
// Architecture notes (matters for newcomers reading the sample):
//
//  - The Enjin Platform API token is held by this server only. The Unity
//    client authenticates against /api/auth and gets a player-scoped JWT;
//    the API token is never exposed to clients.
//
//  - All blockchain mutations in v3 are submitted via the single
//    CreateTransaction mutation with a TransactionInput union populated to
//    one of its 46 method fields (MintToken, BurnToken, TransferToken, ...).
//
//  - There are no event subscriptions in v3. After submitting a transaction
//    we poll GetTransaction until State is terminal
//    (Finalized | Failed | Abandoned | Timeout).
//
//  - The daemon wallet (configured DaemonWalletAddress) owns the collection
//    and mints tokens to player wallets. Per-player melts and transfers are
//    signed by the daemon on behalf of the player's managed wallet, addressed
//    by the player's email as externalId.
public sealed class EnjinService : IAsyncDisposable
{
    private readonly PlatformClient _client;
    private readonly EnjinOptions _opts;
    private readonly ServerState _state;
    private readonly ILogger<EnjinService> _log;
    private readonly Network _network;
    private readonly Chain _chain;

    public EnjinService(IOptions<EnjinOptions> opts, ServerState state, ILogger<EnjinService> log)
    {
        _opts = opts.Value;
        _state = state;
        _log = log;
        _network = _opts.Network;
        _chain = _opts.Chain;

        if (string.IsNullOrWhiteSpace(_opts.ApiUrl))
            throw new InvalidOperationException("Enjin:ApiUrl is not configured.");
        if (string.IsNullOrWhiteSpace(_opts.ApiToken))
            throw new InvalidOperationException("Enjin:ApiToken is not configured.");

        _client = new PlatformClient(new Uri(_opts.ApiUrl), userAgent: "platform-sample-game-server/1.0");
        _client.Auth(_opts.ApiToken);
    }

    // ------------------------------------------------------------------
    // Bootstrap: ensure the sample game's collection + resource tokens exist.
    // Invoked once at server startup before any HTTP request is served.
    // ------------------------------------------------------------------
    public async Task PrepareCollectionAsync(CancellationToken ct)
    {
        var collectionId = _state.CollectionId;
        if (collectionId is null)
        {
            // Avoid creating duplicate collections on canary if state.json was lost.
            // First look for an existing collection owned by the daemon wallet whose
            // "name" attribute matches our configured collection name.
            _log.LogInformation(
                "No collection ID on file. Checking for an existing '{Name}' collection owned by {Owner}...",
                _opts.CollectionName, _opts.DaemonWalletAddress);

            var existing = await FindExistingCollectionAsync(ct);
            if (existing is not null)
            {
                _log.LogInformation("Found existing collection {Id}; reusing.", existing);
                collectionId = existing;
            }
            else
            {
                _log.LogInformation("No matching collection found. Creating new '{Name}' collection...", _opts.CollectionName);
                collectionId = await CreateCollectionAsync(ct);
                _log.LogInformation("Created collection with ID {Id}", collectionId);
            }

            _state.SetCollectionId(collectionId.Value);
        }
        else
        {
            _log.LogInformation("Using existing collection ID {Id}", collectionId);
        }

        // Create any missing resource tokens. Order matters less than correctness;
        // we run them sequentially because the daemon wallet's nonce is shared.
        foreach (var token in _opts.ResourceTokens)
        {
            if (await TokenExistsAsync(collectionId.Value, token.Id, ct))
            {
                _log.LogInformation("Resource token #{Id} '{Name}' already exists", token.Id, token.Name);
                continue;
            }

            _log.LogInformation("Creating resource token #{Id} '{Name}'...", token.Id, token.Name);
            await CreateResourceTokenAsync(collectionId.Value, token, ct);
            _log.LogInformation("Resource token #{Id} '{Name}' ready", token.Id, token.Name);
        }
    }

    // Returns the BigInteger id of an existing collection owned by the daemon
    // wallet whose "name" attribute matches the configured collection name, or
    // null if no such collection exists. Used both before creating a new
    // collection (to avoid duplicates) and after creation (to retrieve the
    // new id, since v3 Transaction does not surface emitted events).
    private async Task<BigInteger?> FindExistingCollectionAsync(CancellationToken ct)
    {
        var query = new QueryQueryBuilder().WithGetCollections(
            new CollectionQueryBuilder()
                .WithId()
                .WithAttributes(new AttributeQueryBuilder().WithKey().WithValue()),
            _network, _chain,
            ids: null,
            address: _opts.DaemonWalletAddress);

        var resp = await _client.SendQuery(query);
        EnsureSuccess(resp, "GetCollections");

        var match = (resp.Result.Data?.GetCollections ?? Array.Empty<Collection>())
            .Where(c => c is not null)
            .Where(c => c!.Attributes?.Any(a => a is not null && a.Key == "name" && a.Value == _opts.CollectionName) == true)
            .OrderByDescending(c => c!.Id)
            .FirstOrDefault();

        return match?.Id;
    }

    private async Task<BigInteger> CreateCollectionAsync(CancellationToken ct)
    {
        var transaction = new TransactionInput
        {
            CreateCollection = new CreateCollectionInput
            {
                ForceCollapsingSupply = false,
                Attributes = new List<AttributeInput>
                {
                    new() { Key = "name", Value = _opts.CollectionName },
                    new() { Key = "banner_image", Value = _opts.CollectionBannerImage },
                    new() { Key = "media", Value = _opts.CollectionMedia },
                },
            },
        };

        var submitted = await CreateTransactionAsync(transaction, signerExternalId: null, ct);
        await WaitForFinalizationAsync(submitted.Uuid!, "collection creation", ct);

        // v3 Transaction does not surface emitted events; locate the new collection by
        // listing those owned by the daemon wallet and matching on the "name" attribute.
        var found = await FindExistingCollectionAsync(ct);
        if (found is null)
        {
            throw new InvalidOperationException(
                $"CreateCollection finalized but no collection named '{_opts.CollectionName}' " +
                $"found owned by {_opts.DaemonWalletAddress}.");
        }

        return found.Value;
    }

    // Checks whether a token entry exists in our collection by querying it directly.
    // GetToken returns null (not an error) when the (collection, token) pair is unknown.
    private async Task<bool> TokenExistsAsync(BigInteger collectionId, BigInteger tokenId, CancellationToken ct)
    {
        var query = new QueryQueryBuilder().WithGetToken(
            new TokenQueryBuilder().WithTokenId(),
            _network, _chain,
            collectionId: collectionId,
            tokenId: tokenId);

        var resp = await _client.SendQuery(query);
        EnsureSuccess(resp, "GetToken");
        return resp.Result.Data?.GetToken is not null;
    }

    private async Task CreateResourceTokenAsync(BigInteger collectionId, ResourceTokenDefinition token, CancellationToken ct)
    {
        var transaction = new TransactionInput
        {
            CreateToken = new CreateTokenInput
            {
                Recipient = _opts.DaemonWalletAddress,
                CollectionId = collectionId,
                TokenId = new BigInteger(token.Id),
                InitialSupply = BigInteger.One,
                Attributes = new List<AttributeInput>
                {
                    new() { Key = "name", Value = token.Name },
                    new() { Key = "media", Value = token.Media },
                },
            },
        };

        var submitted = await CreateTransactionAsync(transaction, signerExternalId: null, ct);
        await WaitForFinalizationAsync(submitted.Uuid!, $"create token #{token.Id} '{token.Name}'", ct);
    }

    // ------------------------------------------------------------------
    // Managed wallets
    // ------------------------------------------------------------------

    public async Task<string?> GetManagedWalletAddressAsync(string externalId, CancellationToken ct)
    {
        var query = new QueryQueryBuilder().WithGetManagedWallet(
            new ManagedWalletQueryBuilder().WithPublicKey().WithExternalId(),
            _network, _chain, externalId: externalId);

        var resp = await _client.SendQuery(query);
        EnsureSuccess(resp, "GetManagedWallet");
        // PublicKey on a managed wallet IS the account public key (hex); we resolve to
        // SS58 by calling GetAccount.
        var publicKey = resp.Result.Data?.GetManagedWallet?.PublicKey;
        if (publicKey is null) return null;
        return await ResolveAddressAsync(publicKey, ct);
    }

    // Ensures the wallet exists; returns the SS58 address. Idempotent: if the
    // wallet already exists, we just resolve and return it.
    public async Task<string> EnsureManagedWalletAsync(string externalId, CancellationToken ct)
    {
        var existing = await GetManagedWalletAddressAsync(externalId, ct);
        if (existing is not null) return existing;

        var mutation = new MutationQueryBuilder().WithCreateManagedWallet(externalId);
        var resp = await _client.SendMutation(mutation);
        EnsureSuccess(resp, "CreateManagedWallet");

        // The platform creates the wallet asynchronously: poll until it's queryable.
        for (var attempt = 1; attempt <= _opts.ManagedWalletPollMaxAttempts; attempt++)
        {
            var address = await GetManagedWalletAddressAsync(externalId, ct);
            if (address is not null) return address;
            await Task.Delay(TimeSpan.FromSeconds(_opts.ManagedWalletPollIntervalSeconds), ct);
        }

        throw new InvalidOperationException(
            $"CreateManagedWallet for externalId '{externalId}' did not become queryable " +
            $"after {_opts.ManagedWalletPollMaxAttempts} attempts.");
    }

    private async Task<string> ResolveAddressAsync(string publicKey, CancellationToken ct)
    {
        // GetAccount accepts a public key OR SS58 address; pass the public key to
        // receive the canonical SS58 form on Address.
        var query = new QueryQueryBuilder().WithGetAccount(
            new AccountQueryBuilder().WithId().WithAddress(),
            _network, _chain, address: publicKey);

        var resp = await _client.SendQuery(query);
        EnsureSuccess(resp, "GetAccount");
        var account = resp.Result.Data?.GetAccount;
        if (account?.Address is null)
        {
            throw new InvalidOperationException($"Could not resolve SS58 address for public key {publicKey}.");
        }
        return account.Address;
    }

    // ------------------------------------------------------------------
    // Wallet token listing (used by Unity backpack UI)
    // ------------------------------------------------------------------
    public async Task<Models.ManagedWalletAccountDto?> GetManagedWalletTokensAsync(string externalId, CancellationToken ct)
    {
        // Step 1: locate the managed wallet's public key.
        var mwQuery = new QueryQueryBuilder().WithGetManagedWallet(
            new ManagedWalletQueryBuilder().WithPublicKey().WithExternalId(),
            _network, _chain, externalId: externalId);

        var mwResp = await _client.SendQuery(mwQuery);
        EnsureSuccess(mwResp, "GetManagedWallet");
        var wallet = mwResp.Result.Data?.GetManagedWallet;
        if (wallet?.PublicKey is null) return null;

        // Step 2: fetch the account (resolves SS58 + lists held tokens).
        var collectionId = _state.CollectionId
            ?? throw new InvalidOperationException("Collection ID not initialised; PrepareCollection has not run.");

        var accountQuery = new QueryQueryBuilder().WithGetAccount(
            new AccountQueryBuilder()
                .WithId()
                .WithAddress()
                .WithTokens(
                    new TokenQueryBuilder()
                        .WithTokenId()
                        .WithCollection(new CollectionQueryBuilder().WithId())
                        .WithAttributes(new AttributeQueryBuilder().WithKey().WithValue())
                        .WithHolders(new TokenHolderQueryBuilder().WithAddress().WithAmount(), limit: 50, page: 1),
                    limit: 100,
                    collectionId: collectionId),
            _network, _chain, address: wallet.PublicKey);

        var accountResp = await _client.SendQuery(accountQuery);
        EnsureSuccess(accountResp, "GetAccount(tokens)");
        var account = accountResp.Result.Data?.GetAccount;
        if (account?.Address is null) return null;

        // Per-holder balance: scan Token.Holders for this address. Account.Tokens does
        // not expose a balance scalar, so we cross-reference here.
        var tokenAccounts = new List<Models.TokenAccountDto>();
        foreach (var token in account.Tokens ?? Array.Empty<Token>())
        {
            var holderBalance = token.Holders?
                .Where(h => string.Equals(h.Address, account.Address, StringComparison.OrdinalIgnoreCase))
                .Select(h => h.Amount)
                .FirstOrDefault() ?? BigInteger.Zero;

            if (holderBalance.IsZero) continue;

            var attrs = (token.Attributes ?? Enumerable.Empty<Enjin.Platform.Sdk.Attribute>())
                .Select(a => new Models.AttributeDto(a.Key ?? "", a.Value ?? ""))
                .ToList();

            tokenAccounts.Add(new Models.TokenAccountDto(
                Balance: holderBalance.ToString(),
                Token: new Models.TokenDto(
                    Collection: new Models.CollectionDto(token.Collection?.Id.ToString() ?? collectionId.ToString()),
                    TokenId: token.TokenId ?? "",
                    Attributes: attrs)));
        }

        return new Models.ManagedWalletAccountDto(
            Account: new Models.AccountDto(PublicKey: account.Id ?? wallet.PublicKey, Address: account.Address),
            TokenAccounts: tokenAccounts);
    }

    // ------------------------------------------------------------------
    // Mint / Burn / Transfer (all wait for chain finalization)
    // ------------------------------------------------------------------

    public Task<Transaction> MintTokenAsync(BigInteger tokenId, BigInteger amount, string recipientAddress, CancellationToken ct)
    {
        var input = new TransactionInput
        {
            MintToken = new MintTokenInput
            {
                Recipient = recipientAddress,
                CollectionId = RequireCollectionId(),
                TokenId = tokenId,
                Amount = amount,
            },
        };
        return SubmitAndWaitAsync(input, signerExternalId: null, $"mint token #{tokenId}", ct);
    }

    public Task<Transaction> MeltTokenAsync(BigInteger tokenId, BigInteger amount, string signerExternalId, CancellationToken ct)
    {
        var input = new TransactionInput
        {
            BurnToken = new BurnTokenInput
            {
                CollectionId = RequireCollectionId(),
                TokenId = tokenId,
                Amount = amount,
            },
        };
        return SubmitAndWaitAsync(input, signerExternalId, $"burn token #{tokenId}", ct);
    }

    public Task<Transaction> TransferTokenAsync(BigInteger tokenId, BigInteger amount, string recipientAddress, string signerExternalId, CancellationToken ct)
    {
        var input = new TransactionInput
        {
            TransferToken = new TransferTokenInput
            {
                Recipient = recipientAddress,
                CollectionId = RequireCollectionId(),
                TokenId = tokenId,
                Amount = amount,
            },
        };
        return SubmitAndWaitAsync(input, signerExternalId, $"transfer token #{tokenId}", ct);
    }

    // ------------------------------------------------------------------
    // Transaction submission + polling
    // ------------------------------------------------------------------

    private async Task<Transaction> SubmitAndWaitAsync(
        TransactionInput input, string? signerExternalId, string description, CancellationToken ct)
    {
        var submitted = await CreateTransactionAsync(input, signerExternalId, ct);
        return await WaitForFinalizationAsync(submitted.Uuid!, description, ct);
    }

    private async Task<Transaction> CreateTransactionAsync(
        TransactionInput input, string? signerExternalId, CancellationToken ct)
    {
        var mutation = new MutationQueryBuilder().WithCreateTransaction(
            new TransactionQueryBuilder().WithUuid().WithState(),
            _network, _chain,
            transaction: input,
            signerExternalId: signerExternalId);

        var resp = await _client.SendMutation(mutation);
        EnsureSuccess(resp, "CreateTransaction");
        var txn = resp.Result.Data?.CreateTransaction
            ?? throw new InvalidOperationException("CreateTransaction returned no transaction.");
        if (string.IsNullOrEmpty(txn.Uuid))
            throw new InvalidOperationException("CreateTransaction returned a transaction with no UUID.");
        return txn;
    }

    private async Task<Transaction> WaitForFinalizationAsync(string uuid, string description, CancellationToken ct)
    {
        if (_opts.TransactionInitialDelaySeconds > 0)
        {
            await Task.Delay(TimeSpan.FromSeconds(_opts.TransactionInitialDelaySeconds), ct);
        }

        var started = DateTime.UtcNow;
        TransactionStateEnum? lastLoggedState = null;
        DateTime lastStuckWarning = DateTime.UtcNow;

        while (true)
        {
            ct.ThrowIfCancellationRequested();

            var query = new QueryQueryBuilder().WithGetTransaction(
                new TransactionQueryBuilder().WithUuid().WithState().WithExtrinsicHash(),
                _network, _chain, uuid: uuid);

            var resp = await _client.SendQuery(query);
            EnsureSuccess(resp, "GetTransaction");
            var txn = resp.Result.Data?.GetTransaction
                ?? throw new InvalidOperationException($"GetTransaction returned nothing for UUID {uuid}.");

            switch (txn.State)
            {
                case TransactionStateEnum.Finalized:
                    _log.LogInformation("Transaction {Uuid} ({Desc}) finalized after {Elapsed:F0}s.",
                        uuid, description, (DateTime.UtcNow - started).TotalSeconds);
                    return txn;

                case TransactionStateEnum.Failed:
                case TransactionStateEnum.Abandoned:
                case TransactionStateEnum.Timeout:
                    throw new InvalidOperationException(
                        $"Transaction {uuid} ({description}) ended in terminal state {txn.State}.");

                default:
                    // Log only on state transitions to avoid flooding logs while polling.
                    if (lastLoggedState != txn.State)
                    {
                        _log.LogInformation(
                            "Waiting for {Desc} (uuid={Uuid}, state={State})...",
                            description, uuid, txn.State);
                        lastLoggedState = txn.State;
                        lastStuckWarning = DateTime.UtcNow;
                    }
                    else if (txn.State == TransactionStateEnum.Pending
                             && (DateTime.UtcNow - lastStuckWarning).TotalSeconds >= 60)
                    {
                        var elapsed = (DateTime.UtcNow - started).TotalSeconds;
                        _log.LogWarning(
                            "Transaction {Uuid} ({Desc}) has been Pending for {Elapsed:F0}s. " +
                            "Confirm that a wallet daemon is signing transactions for account {Daemon}.",
                            uuid, description, elapsed, _opts.DaemonWalletAddress);
                        lastStuckWarning = DateTime.UtcNow;
                    }

                    await Task.Delay(TimeSpan.FromSeconds(_opts.TransactionPollIntervalSeconds), ct);
                    break;
            }
        }
    }

    // ------------------------------------------------------------------
    // Helpers
    // ------------------------------------------------------------------

    private BigInteger RequireCollectionId() =>
        _state.CollectionId ?? throw new InvalidOperationException(
            "Collection ID not initialised; PrepareCollection has not run.");

    private static void EnsureSuccess(IPlatformResponse<QueryResponse> resp, string operation) =>
        EnsureSuccessCore(resp.IsSuccessStatusCode, resp.StatusCode, resp.Result?.Errors, operation);

    private static void EnsureSuccess(IPlatformResponse<MutationResponse> resp, string operation) =>
        EnsureSuccessCore(resp.IsSuccessStatusCode, resp.StatusCode, resp.Result?.Errors, operation);

    private static void EnsureSuccessCore(
        bool isSuccess, System.Net.HttpStatusCode status, ICollection<GraphQlQueryError>? errors, string operation)
    {
        if (!isSuccess)
        {
            throw new InvalidOperationException(
                $"{operation} returned HTTP {(int)status} {status}.");
        }
        if (errors is { Count: > 0 })
        {
            throw new InvalidOperationException(
                $"{operation} returned GraphQL errors: {string.Join("; ", errors.Select(e => e.Message))}");
        }
    }

    public ValueTask DisposeAsync()
    {
        _client.Dispose();
        return ValueTask.CompletedTask;
    }
}

// Process-local mutable state. Currently just the bootstrapped collection ID,
// persisted to state.json so we don't re-create the collection on every restart.
public sealed class ServerState
{
    private readonly string _path;
    private BigInteger? _collectionId;
    private readonly object _lock = new();

    public ServerState(IHostEnvironment env)
    {
        _path = Path.Combine(env.ContentRootPath, "state.json");
        Load();
    }

    public BigInteger? CollectionId
    {
        get { lock (_lock) { return _collectionId; } }
    }

    public void SetCollectionId(BigInteger id)
    {
        lock (_lock)
        {
            _collectionId = id;
            Persist();
        }
    }

    public void OverrideFromConfig(string? configured)
    {
        if (string.IsNullOrWhiteSpace(configured)) return;
        if (!BigInteger.TryParse(configured, out var parsed)) return;
        lock (_lock)
        {
            // Don't clobber on-disk state if it disagrees; on-disk wins because
            // the operator has been running with that one.
            _collectionId ??= parsed;
        }
    }

    private void Load()
    {
        if (!File.Exists(_path)) return;
        try
        {
            using var stream = File.OpenRead(_path);
            var doc = JsonDocument.Parse(stream);
            if (doc.RootElement.TryGetProperty("collectionId", out var prop)
                && prop.ValueKind == JsonValueKind.String
                && BigInteger.TryParse(prop.GetString(), out var parsed))
            {
                _collectionId = parsed;
            }
        }
        catch
        {
            // State file is best-effort; ignore corruption and let the bootstrap re-create.
        }
    }

    private void Persist()
    {
        var payload = new { collectionId = _collectionId?.ToString() };
        File.WriteAllText(_path, JsonSerializer.Serialize(payload, new JsonSerializerOptions { WriteIndented = true }));
    }
}
