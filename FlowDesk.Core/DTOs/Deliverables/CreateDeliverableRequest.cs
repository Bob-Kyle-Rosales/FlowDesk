namespace FlowDesk.Core.DTOs.Deliverables;

public record CreateDeliverableRequest(string Name, string? Description, Guid? MilestoneId);
