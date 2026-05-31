namespace FlowDesk.Core.DTOs.Projects;

public record CreateProjectRequest(string Name, string? Description, Guid ClientId);
