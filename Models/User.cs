namespace PlatformSampleGameServer.Models;

// In-memory user record. Matches the original Node.js sample's in-memory store
// (src/models/user.js). Persistence is intentionally omitted: this is sample
// code, not a production user database.
public sealed record User(string Email, string PasswordHash);
