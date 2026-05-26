using System.Collections.Concurrent;
using PlatformSampleGameServer.Models;

namespace PlatformSampleGameServer.Services;

// Process-local user registry. Matches the original Node.js sample's in-memory
// model (src/models/user.js). Lost on process restart; this is intentional for
// a sample. Replace with a real persistence layer for production use.
public sealed class UserStore
{
    private readonly ConcurrentDictionary<string, User> _users = new(StringComparer.OrdinalIgnoreCase);

    public User? FindByEmail(string email) =>
        _users.TryGetValue(email, out var user) ? user : null;

    public User Create(string email, string passwordHash)
    {
        var user = new User(email, passwordHash);
        if (!_users.TryAdd(email, user))
        {
            throw new InvalidOperationException($"User '{email}' already exists.");
        }
        return user;
    }
}
