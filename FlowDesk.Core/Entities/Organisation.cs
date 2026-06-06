namespace FlowDesk.Core.Entities;

/// <summary>
/// The multi-tenant root. Every user, project, and invoice belongs to an Organisation.
/// All EF global query filters key off OrganisationId to prevent cross-tenant data leaks.
/// </summary>
public class Organisation
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    // URL-safe identifier used in the public client portal: /portal/{slug}
    public string Slug { get; set; } = string.Empty;
    // Branding fields — applied to the white-labeled client portal (Phase 5)
    public string? PrimaryColor { get; set; }
    public string? CustomDomain { get; set; }
    public string? LogoUrl { get; set; }
    public string Plan { get; set; } = "starter";
    public string? StripeAccountId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<User> Users { get; set; } = new List<User>();
    public ICollection<Project> Projects { get; set; } = new List<Project>();
    public ICollection<Invoice> Invoices { get; set; } = new List<Invoice>();
}
