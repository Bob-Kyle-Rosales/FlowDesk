namespace FlowDesk.Core.Entities;

/// <summary>
/// Persisted refresh token used to issue new access tokens without re-login.
/// Tokens are single-use — each refresh call revokes the current token and issues a new one.
/// Revoked tokens are kept (not deleted) to detect reuse attempts.
/// </summary>
public class RefreshToken
{
    public Guid Id { get; set; }
    // 64 random bytes base64-encoded — opaque string, not a JWT
    public string Token { get; set; } = string.Empty;
    public bool IsRevoked { get; set; }
    public DateTime ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
}
