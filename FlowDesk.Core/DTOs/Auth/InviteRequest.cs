using FlowDesk.Core.Enums;

namespace FlowDesk.Core.DTOs.Auth;

/// <summary>
/// Payload for POST /api/auth/invite. Role must be Client or AgencyMember —
/// AgencyOwner cannot be assigned via invite (only via registration).
/// </summary>
public record InviteRequest(string Email, string Name, UserRole Role);
