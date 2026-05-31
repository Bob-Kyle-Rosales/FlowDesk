namespace FlowDesk.Core.DTOs.Milestones;

public record MilestoneResponse(
    Guid Id,
    Guid ProjectId,
    string Title,
    string? Description,
    string Status,
    int Order,
    DateTime? DueDate);
