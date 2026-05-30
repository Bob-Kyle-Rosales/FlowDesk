using FlowDesk.Core.Enums;

namespace FlowDesk.Core.Entities;

/// <summary>
/// A billable engagement between an agency and one client.
/// Agency staff see all projects in their org; clients only see projects where ClientId == their UserId.
/// </summary>
public class Project
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public ProjectStatus Status { get; set; } = ProjectStatus.Active;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Guid OrganisationId { get; set; }
    public Organisation Organisation { get; set; } = null!;

    // ClientId points to a User with Role == Client, not a separate Clients table
    public Guid ClientId { get; set; }
    public User Client { get; set; } = null!;

    public ICollection<Milestone> Milestones { get; set; } = new List<Milestone>();
    public ICollection<Deliverable> Deliverables { get; set; } = new List<Deliverable>();
    public ICollection<Message> Messages { get; set; } = new List<Message>();
    public ICollection<Invoice> Invoices { get; set; } = new List<Invoice>();
}
