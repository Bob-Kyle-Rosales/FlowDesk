using FlowDesk.Core.DTOs.Auth;
using FlowDesk.Core.Entities;
using FlowDesk.Core.Enums;
using FlowDesk.Core.Interfaces;
using FlowDesk.Core.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace FlowDesk.Tests.Services;

public class AuthServiceTests
{
    // Pre-computed at work factor 4 so the correct-password test completes quickly.
    // The service uses work factor 12 in production, but BCrypt.Verify works with any factor.
    private static readonly string TestPasswordHash =
        BCrypt.Net.BCrypt.HashPassword("Test@12345!", workFactor: 4);

    private readonly Mock<IUserRepository> _userRepo = new();
    private readonly Mock<IOrganisationRepository> _orgRepo = new();
    private readonly Mock<IEmailService> _email = new();
    private readonly AuthService _sut;

    private static IConfiguration BuildConfig() =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["JWT_SECRET"] = "test-jwt-secret-that-is-at-least-32-characters-long",
                ["FRONTEND_URL"] = "http://localhost:3000"
            })
            .Build();

    public AuthServiceTests()
    {
        _email.Setup(e => e.SendAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        _sut = new AuthService(
            _userRepo.Object,
            _orgRepo.Object,
            BuildConfig(),
            _email.Object,
            NullLogger<AuthService>.Instance);
    }

    private User BuildUser(Guid userId, Guid orgId) => new()
    {
        Id = userId,
        Email = "test@example.com",
        Name = "Test User",
        PasswordHash = TestPasswordHash,
        Role = UserRole.AgencyOwner,
        OrganisationId = orgId,
        Organisation = new Organisation { Id = orgId, Name = "Test Agency", Slug = "test-agency" }
    };

    [Fact]
    public async Task RegisterAsync_DuplicateEmail_ThrowsInvalidOperationException()
    {
        _userRepo.Setup(r => r.EmailExistsAsync("taken@example.com")).ReturnsAsync(true);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _sut.RegisterAsync(new RegisterRequest("Name", "taken@example.com", "password", "Agency")));
    }

    [Fact]
    public async Task LoginAsync_UserNotFound_ThrowsUnauthorizedAccessException()
    {
        _userRepo.Setup(r => r.GetByEmailAsync("nobody@example.com")).ReturnsAsync((User?)null);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _sut.LoginAsync(new LoginRequest("nobody@example.com", "any-password")));
    }

    [Fact]
    public async Task LoginAsync_WrongPassword_ThrowsUnauthorizedAccessException()
    {
        var userId = Guid.NewGuid();
        var orgId = Guid.NewGuid();
        _userRepo.Setup(r => r.GetByEmailAsync("test@example.com")).ReturnsAsync(BuildUser(userId, orgId));

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _sut.LoginAsync(new LoginRequest("test@example.com", "wrong-password")));
    }

    [Fact]
    public async Task LoginAsync_ValidCredentials_ReturnsTokensAndAuthResponse()
    {
        var userId = Guid.NewGuid();
        var orgId = Guid.NewGuid();
        _userRepo.Setup(r => r.GetByEmailAsync("test@example.com")).ReturnsAsync(BuildUser(userId, orgId));
        _userRepo.Setup(r => r.AddRefreshTokenAsync(It.IsAny<RefreshToken>())).Returns(Task.CompletedTask);

        var (tokens, user) = await _sut.LoginAsync(new LoginRequest("test@example.com", "Test@12345!"));

        Assert.False(string.IsNullOrWhiteSpace(tokens.AccessToken));
        Assert.False(string.IsNullOrWhiteSpace(tokens.RefreshToken));
        Assert.Equal("test@example.com", user.Email);
        Assert.Equal("AgencyOwner", user.Role);
    }

    [Fact]
    public async Task RefreshAsync_TokenNotFound_ThrowsUnauthorizedAccessException()
    {
        _userRepo.Setup(r => r.GetRefreshTokenAsync("unknown-token")).ReturnsAsync((RefreshToken?)null);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _sut.RefreshAsync("unknown-token"));
    }

    [Fact]
    public async Task RefreshAsync_RevokedToken_ThrowsUnauthorizedAccessException()
    {
        _userRepo.Setup(r => r.GetRefreshTokenAsync("revoked-token"))
            .ReturnsAsync(new RefreshToken
            {
                Id = Guid.NewGuid(),
                Token = "revoked-token",
                IsRevoked = true,
                ExpiresAt = DateTime.UtcNow.AddDays(7),
                User = new User
                {
                    OrganisationId = Guid.NewGuid(),
                    Organisation = new Organisation()
                }
            });

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _sut.RefreshAsync("revoked-token"));
    }

    [Fact]
    public async Task RefreshAsync_ExpiredToken_ThrowsUnauthorizedAccessException()
    {
        _userRepo.Setup(r => r.GetRefreshTokenAsync("expired-token"))
            .ReturnsAsync(new RefreshToken
            {
                Id = Guid.NewGuid(),
                Token = "expired-token",
                IsRevoked = false,
                ExpiresAt = DateTime.UtcNow.AddDays(-1),
                User = new User
                {
                    OrganisationId = Guid.NewGuid(),
                    Organisation = new Organisation()
                }
            });

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _sut.RefreshAsync("expired-token"));
    }

    [Fact]
    public async Task LogoutAsync_UnknownToken_DoesNotThrow()
    {
        _userRepo.Setup(r => r.GetRefreshTokenAsync("unknown")).ReturnsAsync((RefreshToken?)null);

        var ex = await Record.ExceptionAsync(() => _sut.LogoutAsync("unknown"));

        Assert.Null(ex);
    }
}
