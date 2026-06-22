using PlatformSampleGameServer.Services;

// Regression test for Services/SubstrateAddress.cs.
//
// Asserts the SS58 encoder against canonical published vectors:
//
//   - Alice (//Alice sr25519 dev key, public key
//     0xd43593c715fdd31c61141abd04a99fd6822c8558854ccde39a5684e7a56da27d)
//     on the generic Substrate prefix (42), Polkadot mainnet (prefix 0),
//     and Kusama (prefix 2). These appear throughout the Substrate /
//     Polkadot documentation and in the subkey test fixtures.
//
// The three vectors collectively exercise both single-byte SS58 prefixes
// (0, 2, 42) and the unkeyed Blake2b-512 checksum, which is the property
// the production encoder depends on. (Enjin Matrixchain prefixes 9030 /
// 1110 use the two-byte SS58 prefix form, which we exercise through the
// running platform but do not assert here as we lack a published vector
// for the //Alice dev key on those chains; if the checksum function or
// base58 encoder regress, the single-byte vectors below will catch it.)
//
// Any mismatch prints the divergence and exits with a non-zero status so
// CI / a manual `dotnet run --project tools/Ss58SelfTest` will fail loudly
// if the encoder (or the underlying Blake2b implementation) regresses.

var vectors = new (string Label, string PublicKeyHex, ushort Prefix, string ExpectedAddress)[]
{
    (
        "Alice / Substrate generic (prefix 42)",
        "0xd43593c715fdd31c61141abd04a99fd6822c8558854ccde39a5684e7a56da27d",
        42,
        "5GrwvaEF5zXb26Fz9rcQpDWS57CtERHpNehXCPcNoHGKutQY"
    ),
    (
        "Alice / Polkadot mainnet (prefix 0)",
        "0xd43593c715fdd31c61141abd04a99fd6822c8558854ccde39a5684e7a56da27d",
        0,
        "15oF4uVJwmo4TdGW7VfQxNLavjCXviqxT9S1MgbjMNHr6Sp5"
    ),
    (
        "Alice / Kusama (prefix 2)",
        "0xd43593c715fdd31c61141abd04a99fd6822c8558854ccde39a5684e7a56da27d",
        2,
        "HNZata7iMYWmk5RvZRTiAsSDhV8366zq2YGb3tLH5Upf74F"
    ),
};

int failures = 0;
foreach (var v in vectors)
{
    var actual = SubstrateAddress.Encode(v.PublicKeyHex, v.Prefix);
    var ok = string.Equals(actual, v.ExpectedAddress, StringComparison.Ordinal);
    Console.WriteLine($"[{(ok ? "PASS" : "FAIL")}] {v.Label}");
    if (!ok)
    {
        Console.WriteLine($"        expected: {v.ExpectedAddress}");
        Console.WriteLine($"        actual:   {actual}");
        failures++;
    }
}

if (failures > 0)
{
    Console.Error.WriteLine(
        $"\n{failures} SS58 vector(s) failed. The encoder is producing incorrect addresses."
    );
    return 1;
}

Console.WriteLine($"\nAll {vectors.Length} SS58 vectors passed.");
return 0;
