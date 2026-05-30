namespace FlowDesk.Core.DTOs.Auth;

/// <summary>
/// Returned in the response body after login, register, or refresh.
/// Tokens are NOT included here — they are written to httpOnly cookies by the controller.
/// </summary>
public record AuthResponse(
    Guid UserId,
    string Name,
    string Email,
    string Role,
    string OrganisationName
);
