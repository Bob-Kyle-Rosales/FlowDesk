namespace FlowDesk.Core.DTOs.Projects;

public record ProjectResponse(
    Guid Id,
    string Name,
    string? Description,
    string Status,
    Guid ClientId,
    string ClientName,
    Guid OrganisationId,
    DateTime CreatedAt);
