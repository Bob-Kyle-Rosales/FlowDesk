namespace FlowDesk.Core.DTOs.Projects;

public record UpdateProjectRequest(string Name, string? Description, string Status);
