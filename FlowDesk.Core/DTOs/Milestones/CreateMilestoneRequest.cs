namespace FlowDesk.Core.DTOs.Milestones;

public record CreateMilestoneRequest(
    string Title,
    string? Description,
    int Order,
    DateTime? DueDate);
