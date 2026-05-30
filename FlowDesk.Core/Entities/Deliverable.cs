using FlowDesk.Core.Enums;

namespace FlowDesk.Core.Entities;

/// <summary>
/// A file or artifact the agency uploads for client review.
/// Clients can Approve it or request a Revision — each revision cycle increments Version.
/// </summary>
public class Deliverable
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DeliverableStatus Status { get; set; } = DeliverableStatus.Pending;
    // R2 object URL — set after the agency uploads the file (Phase 2)
    public string? FileUrl { get; set; }
    // Incremented each time the agency re-uploads after a revision request
    public int Version { get; set; } = 1;
    public string? RevisionNotes { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Guid ProjectId { get; set; }
    public Project Project { get; set; } = null!;

    // Optional — deliverables can exist outside a milestone
    public Guid? MilestoneId { get; set; }
    public Milestone? Milestone { get; set; }

    // Tracks who approved it, populated when Status → Approved
    public Guid? ApprovedById { get; set; }
    public User? ApprovedBy { get; set; }
}
