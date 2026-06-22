using System.Numerics;
using PlatformSampleGameServer.Endpoints;

// Regression test for Endpoints/TokenEndpoints.TryParseAmount.
//
// Mint / melt / transfer amounts arrive on the wire as decimal strings because
// token amounts are BigIntegers on-chain (Enjin Platform C# SDK v3). The parser
// must:
//
//   - accept positive integers of arbitrary size, preserving the exact value
//     (a value above long range is the property an int/long parser would lose);
//   - reject zero and negative amounts;
//   - reject non-integer / non-numeric input (decimals, words, empty, null).
//
// Any mismatch prints the divergence and exits non-zero so CI / a manual
// `dotnet run --project tools/TransferAmountSelfTest` fails loudly if the
// amount validation regresses.

// 2^128, well beyond int and long range — the headline BigInteger case.
var huge = BigInteger.Pow(2, 128);

var vectors = new (
    string Label,
    string? Input,
    bool ExpectOk,
    BigInteger ExpectedAmount,
    string? ExpectedError
)[]
{
    ("small positive", "5", true, 5, null),
    ("above int range", "3000000000", true, 3_000_000_000L, null),
    ("above long range (2^128)", huge.ToString(), true, huge, null),
    ("leading/trailing whitespace tolerated", "  42  ", true, 42, null),
    ("zero rejected", "0", false, default, "Amount must be positive."),
    ("negative rejected", "-1", false, default, "Amount must be positive."),
    ("decimal rejected", "1.5", false, default, "Invalid amount '1.5'."),
    ("non-numeric rejected", "abc", false, default, "Invalid amount 'abc'."),
    ("empty rejected", "", false, default, "Invalid amount ''."),
    ("null rejected", null, false, default, "Invalid amount ''."),
};

int failures = 0;
foreach (var v in vectors)
{
    var ok = TokenEndpoints.TryParseAmount(v.Input, out var amount, out var error);

    var passed =
        ok == v.ExpectOk && (v.ExpectOk ? amount == v.ExpectedAmount : error == v.ExpectedError);

    Console.WriteLine($"[{(passed ? "PASS" : "FAIL")}] {v.Label}");
    if (!passed)
    {
        Console.WriteLine($"        input:    {v.Input ?? "<null>"}");
        Console.WriteLine(
            $"        expected: ok={v.ExpectOk} amount={v.ExpectedAmount} error={v.ExpectedError ?? "<null>"}"
        );
        Console.WriteLine($"        actual:   ok={ok} amount={amount} error={error ?? "<null>"}");
        failures++;
    }
}

if (failures > 0)
{
    Console.Error.WriteLine(
        $"\n{failures} amount vector(s) failed. Transfer amount validation has regressed."
    );
    return 1;
}

Console.WriteLine($"\nAll {vectors.Length} transfer-amount vectors passed.");
return 0;
