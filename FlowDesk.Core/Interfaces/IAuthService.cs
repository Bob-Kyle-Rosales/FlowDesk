using FlowDesk.Core.DTOs.Auth;

namespace FlowDesk.Core.Interfaces;

public interface IAuthService
{
    Task<(TokenPair Tokens, AuthResponse User)> RegisterAsync(RegisterRequest request);
    Task<(TokenPair Tokens, AuthResponse User)> LoginAsync(LoginRequest request);
    Task<(TokenPair Tokens, AuthResponse User)> RefreshAsync(string refreshToken);
    Task LogoutAsync(string refreshToken);
    Task<string> InviteAsync(InviteRequest request, Guid organisationId);
    Task<(TokenPair Tokens, AuthResponse User)> AcceptInviteAsync(AcceptInviteRequest request);
}
