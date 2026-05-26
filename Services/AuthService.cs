using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using PlatformSampleGameServer.Models;

namespace PlatformSampleGameServer.Services;

// Player-facing authentication: bcrypt-hashed passwords + a short-lived JWT
// bearer token returned to the Unity client. Mirrors the original Node.js
// services/authService.js (bcryptjs + jsonwebtoken).
public sealed class AuthService
{
    public const string EmailClaim = "email";

    private readonly UserStore _users;
    private readonly JwtOptions _jwt;

    public AuthService(UserStore users, IOptions<JwtOptions> jwt)
    {
        _users = users;
        _jwt = jwt.Value;
    }

    // Register-or-login: if the email already exists, fall through to login.
    // This matches the original Node.js behaviour where /api/auth/register is
    // also the login endpoint the Unity client calls.
    public (string Token, string Email) RegisterOrLogin(string email, string password)
    {
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            throw new ArgumentException("Email and password are required.");
        }

        var existing = _users.FindByEmail(email);
        if (existing is not null)
        {
            if (!BCrypt.Net.BCrypt.Verify(password, existing.PasswordHash))
            {
                throw new UnauthorizedAccessException("Invalid password.");
            }
            return (GenerateToken(existing), existing.Email);
        }

        var hash = BCrypt.Net.BCrypt.HashPassword(password, workFactor: 10);
        var created = _users.Create(email, hash);
        return (GenerateToken(created), created.Email);
    }

    private string GenerateToken(User user)
    {
        if (string.IsNullOrWhiteSpace(_jwt.Secret))
        {
            throw new InvalidOperationException(
                "Jwt:Secret is not configured. Set it in appsettings.Development.json or an env var.");
        }

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwt.Secret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _jwt.Issuer,
            audience: _jwt.Audience,
            claims: new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Email),
                new Claim(JwtRegisteredClaimNames.Email, user.Email),
                new Claim(EmailClaim, user.Email),
            },
            expires: DateTime.UtcNow.AddHours(_jwt.ExpiryHours),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
