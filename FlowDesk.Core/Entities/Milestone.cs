using FlowDesk.Core.Enums;

namespace FlowDesk.Core.Entities;

/// <summary>
/// A named phase within a project. Milestones are displayed in Order sequence
/// and roll up into the project's overall progress percentage.
/// </summary>
public class Milestone
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public MilestoneStatus Status { get; set; } = MilestoneStatus.Pending;
    // Agency-controlled sort order — not derived from creation date so it can be reordered
    public int Order { get; set; }
    public DateTime? DueDate { get; set; }

    public Guid ProjectId { get; set; }
    public Project Project { get; set; } = null!;

    public ICollection<Deliverable> Deliverables { get; set; } = new List<Deliverable>();
}
