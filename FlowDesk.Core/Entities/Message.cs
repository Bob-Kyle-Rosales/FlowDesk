namespace FlowDesk.Core.Entities;

/// <summary>
/// A chat message scoped to a project. Both agency staff and the client
/// can send messages. Real-time delivery is handled by SignalR ChatHub (Phase 3).
/// </summary>
public class Message
{
    public Guid Id { get; set; }
    public string Content { get; set; } = string.Empty;
    // Optional file attachment stored in R2 (Phase 3)
    public string? FileUrl { get; set; }
    // Drives unread badge counts — updated when the recipient opens the conversation
    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Guid ProjectId { get; set; }
    public Project Project { get; set; } = null!;

    public Guid SenderId { get; set; }
    public User Sender { get; set; } = null!;
}
