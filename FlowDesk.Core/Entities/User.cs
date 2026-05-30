using FlowDesk.Core.Enums;

namespace FlowDesk.Core.Entities;

/// <summary>
/// Represents agency staff (AgencyOwner, AgencyMember) and their clients.
/// All three roles share this table — Role determines what they can see and do.
/// </summary>
public class User
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public UserRole Role { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Guid OrganisationId { get; set; }
    // null! tells the compiler EF Core will always populate this via Include() — it won't be null at runtime
    public Organisation Organisation { get; set; } = null!;

    public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
}
