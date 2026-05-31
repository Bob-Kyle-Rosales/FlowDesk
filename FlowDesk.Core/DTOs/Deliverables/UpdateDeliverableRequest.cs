namespace FlowDesk.Core.DTOs.Deliverables;

public record UpdateDeliverableRequest(string Name, string? Description, Guid? MilestoneId);
