namespace FlowDesk.Core.DTOs.Deliverables;

public record DeliverableResponse(
    Guid Id,
    Guid ProjectId,
    Guid? MilestoneId,
    string Name,
    string? Description,
    string Status,
    string? FileUrl,
    int Version,
    string? RevisionNotes,
    DateTime? ApprovedAt,
    DateTime CreatedAt);
