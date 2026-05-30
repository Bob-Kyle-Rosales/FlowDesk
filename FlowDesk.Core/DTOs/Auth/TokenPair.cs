namespace FlowDesk.Core.DTOs.Auth;

/// <summary>
/// Internal token pair passed from AuthService to the controller.
/// The controller writes both values to httpOnly cookies — they are never serialised to JSON.
/// </summary>
public record TokenPair(string AccessToken, string RefreshToken);
