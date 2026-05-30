using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using FlowDesk.Core.DTOs.Auth;
using FlowDesk.Core.Entities;
using FlowDesk.Core.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace FlowDesk.Core.Services;

public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly IConfiguration _configuration;

    public AuthService(IUserRepository userRepository, IConfiguration configuration)
    {
        _userRepository = userRepository;
        _configuration = configuration;
    }

    /// <summary>
    /// Creates a new agency account with its own Organisation. The first user is always AgencyOwner.
    /// Issues tokens immediately so the client is logged in after registration.
    /// </summary>
    public async Task<(TokenPair Tokens, AuthResponse User)> RegisterAsync(RegisterRequest request)
    {
        if (await _userRepository.EmailExistsAsync(request.Email))
            throw new InvalidOperationException("Email is already in use.");

        var organisation = new Organisation
        {
            Id = Guid.NewGuid(),
            Name = request.OrganisationName,
            Slug = GenerateSlug(request.OrganisationName)
        };

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = request.Email.ToLower(),
            Name = request.Name,
            // Work factor 12 — slow enough to resist brute-force, fast enough for login UX
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password, workFactor: 12),
            Role = Enums.UserRole.AgencyOwner,
            OrganisationId = organisation.Id
        };

        await _userRepository.CreateWithOrganisationAsync(user, organisation);

        var tokens = await IssueTokensAsync(user);
        return (tokens, ToAuthResponse(user, organisation.Name));
    }

    /// <summary>
    /// Validates credentials and issues tokens. Both "user not found" and "wrong password"
    /// return the same error message intentionally — avoids leaking whether an email is registered.
    /// </summary>
    public async Task<(TokenPair Tokens, AuthResponse User)> LoginAsync(LoginRequest request)
    {
        var user = await _userRepository.GetByEmailAsync(request.Email)
            ?? throw new UnauthorizedAccessException("Invalid credentials.");

        if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            throw new UnauthorizedAccessException("Invalid credentials.");

        var tokens = await IssueTokensAsync(user);
        return (tokens, ToAuthResponse(user, user.Organisation.Name));
    }

    /// <summary>
    /// Rotates the refresh token — the old token is revoked before the new one is issued,
    /// so each refresh token is single-use. A stolen token can only be used once.
    /// </summary>
    public async Task<(TokenPair Tokens, AuthResponse User)> RefreshAsync(string refreshToken)
    {
        var stored = await _userRepository.GetRefreshTokenAsync(refreshToken)
            ?? throw new UnauthorizedAccessException("Invalid refresh token.");

        if (stored.IsRevoked || stored.ExpiresAt < DateTime.UtcNow)
            throw new UnauthorizedAccessException("Refresh token has expired or been revoked.");

        await _userRepository.RevokeRefreshTokenAsync(stored.Id);

        var tokens = await IssueTokensAsync(stored.User);
        return (tokens, ToAuthResponse(stored.User, stored.User.Organisation.Name));
    }

    /// <summary>
    /// Revokes the refresh token. The access token expires naturally after 15 minutes —
    /// there is no server-side blocklist for access tokens.
    /// </summary>
    public async Task LogoutAsync(string refreshToken)
    {
        var stored = await _userRepository.GetRefreshTokenAsync(refreshToken);
        if (stored is not null && !stored.IsRevoked)
            await _userRepository.RevokeRefreshTokenAsync(stored.Id);
    }

    /// <summary>
    /// Returns a signed JWT invite link. The token is self-verifying — no DB record needed.
    /// The frontend reads the claims to pre-fill the registration form.
    /// Email delivery is wired in Phase 4 (SendGrid).
    /// </summary>
    public Task<string> InviteAsync(InviteRequest request, Guid organisationId)
    {
        var claims = new[]
        {
            new Claim("invite_email", request.Email),
            new Claim("invite_name", request.Name),
            new Claim("invite_role", request.Role.ToString()),
            new Claim("org", organisationId.ToString()),
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(JwtSecret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            claims: claims,
            expires: DateTime.UtcNow.AddDays(7),
            signingCredentials: creds
        );

        var jwt = new JwtSecurityTokenHandler().WriteToken(token);
        var frontendUrl = _configuration["FRONTEND_URL"] ?? "http://localhost:3000";
        return Task.FromResult($"{frontendUrl}/invite?token={jwt}");
    }

    private async Task<TokenPair> IssueTokensAsync(User user)
    {
        var accessToken = GenerateAccessToken(user);
        var rawRefreshToken = GenerateRefreshToken();

        await _userRepository.AddRefreshTokenAsync(new RefreshToken
        {
            Id = Guid.NewGuid(),
            Token = rawRefreshToken,
            UserId = user.Id,
            ExpiresAt = DateTime.UtcNow.AddDays(7)
        });

        return new TokenPair(accessToken, rawRefreshToken);
    }

    private string GenerateAccessToken(User user)
    {
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            // ClaimTypes.Role is what ASP.NET Core's [Authorize(Policy)] reads
            new Claim(ClaimTypes.Role, user.Role.ToString()),
            // "org" is read by ICurrentUserService to scope every DB query to this tenant
            new Claim("org", user.OrganisationId.ToString()),
            new Claim(JwtRegisteredClaimNames.Name, user.Name),
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(JwtSecret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(15),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    // 64 random bytes = 512 bits of entropy. Not a JWT — just an opaque random string stored in the DB.
    private static string GenerateRefreshToken()
        => Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));

    private static string GenerateSlug(string name)
        => name.ToLower()
               .Replace(" ", "-")
               .Replace("'", "")
               .Trim('-');

    private static AuthResponse ToAuthResponse(User user, string organisationName)
        => new(user.Id, user.Name, user.Email, user.Role.ToString(), organisationName);

    // Property rather than a field so misconfiguration throws at call time with a clear message
    private string JwtSecret => _configuration["JWT_SECRET"]
        ?? throw new InvalidOperationException("JWT_SECRET is not configured.");
}
