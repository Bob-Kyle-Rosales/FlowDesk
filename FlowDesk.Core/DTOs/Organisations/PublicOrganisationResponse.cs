// FlowDesk.Core/DTOs/Organisations/PublicOrganisationResponse.cs
namespace FlowDesk.Core.DTOs.Organisations;

public record PublicOrganisationResponse(
    string Name,
    string Slug,
    string? PrimaryColor,
    string? LogoUrl);
