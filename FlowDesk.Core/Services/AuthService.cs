using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using FlowDesk.Core.DTOs.Auth;
using FlowDesk.Core.Entities;
using FlowDesk.Core.Enums;
using FlowDesk.Core.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;

namespace FlowDesk.Core.Services;

public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly IOrganisationRepository _orgRepo;
    private readonly IConfiguration _configuration;
    private readonly IEmailService _email;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        IUserRepository userRepository,
        IOrganisationRepository orgRepo,
        IConfiguration configuration,
        IEmailService email,
        ILogger<AuthService> logger)
    {
        _userRepository = userRepository;
        _orgRepo = orgRepo;
        _configuration = configuration;
        _email = email;
        _logger = logger;
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
        return (tokens, ToAuthResponse(user, organisation.Name, organisation.Slug));
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
        return (tokens, ToAuthResponse(user, user.Organisation.Name, user.Organisation.Slug));
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
        return (tokens, ToAuthResponse(stored.User, stored.User.Organisation.Name, stored.User.Organisation.Slug));
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
    /// Returns a signed JWT invite link and sends the invite email via SendGrid.
    /// The token is self-verifying — no DB record needed.
    /// The frontend reads the claims to pre-fill the registration form.
    /// Email send failures are logged but do not block the response.
    /// </summary>
    public async Task<string> InviteAsync(InviteRequest request, Guid organisationId)
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
        var inviteLink = $"{frontendUrl}/invite?token={jwt}";

        var safeName = System.Net.WebUtility.HtmlEncode(request.Name);
        var html = $"""
            <p>Hi {safeName},</p>
            <p>You have been invited to join a workspace on <strong>FlowDesk</strong>.</p>
            <p>Click the button below to accept your invitation. The link expires in 7&nbsp;days.</p>
            <p style="margin:24px 0">
              <a href="{inviteLink}"
                 style="background:#7c3aed;color:#fff;padding:12px 24px;border-radius:6px;text-decoration:none;font-weight:600">
                Accept Invitation
              </a>
            </p>
            <p style="font-size:12px;color:#6b7280">
              If you were not expecting this invitation, you can ignore this email.
            </p>
            """;

        try
        {
            await _email.SendAsync(request.Email, request.Name, "You're invited to FlowDesk", html);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send invite email to {Email}", request.Email);
        }

        return inviteLink;
    }

    public async Task<(TokenPair Tokens, AuthResponse User)> AcceptInviteAsync(AcceptInviteRequest request)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(JwtSecret));

        ClaimsPrincipal principal;
        try
        {
            principal = tokenHandler.ValidateToken(request.Token, new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = key,
                ValidateIssuer = false,
                ValidateAudience = false,
                ClockSkew = TimeSpan.Zero
            }, out _);
        }
        catch
        {
            throw new UnauthorizedAccessException("Invalid or expired invite token.");
        }

        var email = principal.FindFirst("invite_email")?.Value
            ?? throw new UnauthorizedAccessException("Invalid invite token.");
        var roleStr = principal.FindFirst("invite_role")?.Value
            ?? throw new UnauthorizedAccessException("Invalid invite token.");
        var orgStr = principal.FindFirst("org")?.Value
            ?? throw new UnauthorizedAccessException("Invalid invite token.");

        if (await _userRepository.EmailExistsAsync(email))
            throw new InvalidOperationException("Email already registered.");

        var orgId = Guid.Parse(orgStr);
        var org = await _orgRepo.GetByIdAsync(orgId)
            ?? throw new InvalidOperationException("Organisation not found.");

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = email.ToLower(),
            Name = request.Name,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password, workFactor: 12),
            Role = Enum.Parse<UserRole>(roleStr),
            OrganisationId = orgId
        };

        await _userRepository.CreateAsync(user);

        var tokens = await IssueTokensAsync(user);
        return (tokens, ToAuthResponse(user, org.Name, org.Slug));
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

    private static AuthResponse ToAuthResponse(User user, string organisationName, string organisationSlug)
        => new(user.Id, user.Name, user.Email, user.Role.ToString(), organisationName, organisationSlug);

    // Property rather than a field so misconfiguration throws at call time with a clear message
    private string JwtSecret => _configuration["JWT_SECRET"]
        ?? throw new InvalidOperationException("JWT_SECRET is not configured.");
}
