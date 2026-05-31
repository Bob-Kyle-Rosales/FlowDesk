namespace FlowDesk.Core.DTOs.Milestones;

public record UpdateMilestoneRequest(
    string Title,
    string? Description,
    int Order,
    DateTime? DueDate);
