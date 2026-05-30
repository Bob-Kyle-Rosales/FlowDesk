namespace FlowDesk.Core.DTOs.Auth;

/// <summary>
/// Payload for POST /api/auth/register. Creates both a User (AgencyOwner) and
/// a new Organisation in one transaction. The first user of an org is always the owner.
/// </summary>
public record RegisterRequest(
    string Name,
    string Email,
    string Password,
    // Separate from the user's name — becomes the agency's brand name and slug
    string OrganisationName
);
