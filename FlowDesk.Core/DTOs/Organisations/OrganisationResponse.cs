// FlowDesk.Core/DTOs/Organisations/OrganisationResponse.cs
namespace FlowDesk.Core.DTOs.Organisations;

public record OrganisationResponse(
    Guid Id,
    string Name,
    string Slug,
    string? PrimaryColor,
    string Plan,
    DateTime CreatedAt,
    string? StripeAccountId);
