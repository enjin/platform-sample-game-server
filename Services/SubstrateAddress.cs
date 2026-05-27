using System;
using System.Linq;
using Konscious.Security.Cryptography;

namespace PlatformSampleGameServer.Services;

/// <summary>
/// SS58 address encoding for Substrate-based chains.
///
/// SS58 is base58-check-style encoding wrapping (prefix-byte(s) ++ payload ++ checksum),
/// where the checksum is the first 2 bytes of Blake2b-512("SS58PRE" ++ prefix ++ payload).
///
/// Used here to convert a managed wallet's hex public key (as returned by
/// the Enjin platform's ManagedWallet.publicKey) into the canonical SS58 address
/// that GetAccount(address:) expects.
///
/// SS58 prefixes of interest:
///   - 9030: Enjin Matrixchain (Canary). Addresses start with "cx".
///   - 1110: Enjin Mainnet Matrixchain. Addresses start with "ef".
/// </summary>
public static class SubstrateAddress
{
    private static readonly byte[] Ss58Pre = System.Text.Encoding.ASCII.GetBytes("SS58PRE");
    private const string Base58Alphabet =
        "123456789ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz";

    /// <summary>
    /// Encode a 32-byte Substrate public key to an SS58 address string.
    /// Accepts hex with or without a "0x" prefix.
    /// </summary>
    public static string Encode(string publicKeyHex, ushort ss58Prefix)
    {
        if (string.IsNullOrWhiteSpace(publicKeyHex))
            throw new ArgumentException("Public key required.", nameof(publicKeyHex));

        var hex = publicKeyHex.StartsWith("0x", StringComparison.OrdinalIgnoreCase)
            ? publicKeyHex[2..]
            : publicKeyHex;
        if (hex.Length != 64)
            throw new ArgumentException(
                $"Expected 32-byte public key (64 hex chars), got {hex.Length}.", nameof(publicKeyHex));

        var payload = Convert.FromHexString(hex);
        return Encode(payload, ss58Prefix);
    }

    /// <summary>
    /// Encode a 32-byte Substrate public key (raw bytes) to an SS58 address string.
    /// </summary>
    public static string Encode(byte[] publicKey, ushort ss58Prefix)
    {
        if (publicKey is null) throw new ArgumentNullException(nameof(publicKey));
        if (publicKey.Length != 32)
            throw new ArgumentException($"Expected 32 bytes, got {publicKey.Length}.", nameof(publicKey));

        // Encode prefix. <64 fits in one byte; >=64 uses the two-byte form per
        // https://docs.substrate.io/reference/address-formats/.
        byte[] prefixBytes;
        if (ss58Prefix < 64)
        {
            prefixBytes = new[] { (byte)ss58Prefix };
        }
        else if (ss58Prefix < 0x4000)
        {
            // Two-byte form: bits are split as 0b01aaaaaa | bbbbbbbb_bb
            // = ((p & 0x3F) | 0x40) ++ ((p >> 8) | ((p & 0xFC00) >> 2))
            byte lower = (byte)(((ss58Prefix & 0x00FC) >> 2) | 0x40);
            byte upper = (byte)(((ss58Prefix >> 8) & 0xFF) | ((ss58Prefix & 0x0003) << 6));
            prefixBytes = new[] { lower, upper };
        }
        else
        {
            throw new ArgumentOutOfRangeException(nameof(ss58Prefix),
                "SS58 prefixes >= 16384 are reserved.");
        }

        // checksum = first 2 bytes of Blake2b-512("SS58PRE" || prefix || payload)
        var hashInput = new byte[Ss58Pre.Length + prefixBytes.Length + publicKey.Length];
        Buffer.BlockCopy(Ss58Pre, 0, hashInput, 0, Ss58Pre.Length);
        Buffer.BlockCopy(prefixBytes, 0, hashInput, Ss58Pre.Length, prefixBytes.Length);
        Buffer.BlockCopy(publicKey, 0, hashInput, Ss58Pre.Length + prefixBytes.Length, publicKey.Length);

        using var blake = new HMACBlake2B(512);
        // HMACBlake2B with no key is plain Blake2b. The constructor takes the output size in bits.
        var hash = blake.ComputeHash(hashInput);

        var full = new byte[prefixBytes.Length + publicKey.Length + 2];
        Buffer.BlockCopy(prefixBytes, 0, full, 0, prefixBytes.Length);
        Buffer.BlockCopy(publicKey, 0, full, prefixBytes.Length, publicKey.Length);
        full[^2] = hash[0];
        full[^1] = hash[1];

        return Base58Encode(full);
    }

    private static string Base58Encode(byte[] data)
    {
        // Count leading zero bytes
        int zeros = 0;
        while (zeros < data.Length && data[zeros] == 0) zeros++;

        // Convert big-endian bytes to base58 by repeated division.
        // Operate on a copy because we mutate during division.
        var input = (byte[])data.Clone();
        var encoded = new System.Collections.Generic.List<char>(data.Length * 138 / 100 + 1);

        int startAt = zeros;
        while (startAt < input.Length)
        {
            int remainder = 0;
            for (int i = startAt; i < input.Length; i++)
            {
                int num = (remainder << 8) | input[i];
                input[i] = (byte)(num / 58);
                remainder = num % 58;
            }
            encoded.Add(Base58Alphabet[remainder]);
            if (input[startAt] == 0) startAt++;
        }

        for (int i = 0; i < zeros; i++) encoded.Add(Base58Alphabet[0]);
        encoded.Reverse();
        return new string(encoded.ToArray());
    }
}
