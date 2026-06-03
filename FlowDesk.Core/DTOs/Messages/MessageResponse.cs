namespace FlowDesk.Core.DTOs.Messages;

public record MessageResponse(
    Guid Id,
    string Content,
    Guid SenderId,
    string SenderName,
    bool IsRead,
    DateTime CreatedAt,
    string? FileUrl);
