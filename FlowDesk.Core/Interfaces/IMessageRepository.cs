using FlowDesk.Core.Entities;

namespace FlowDesk.Core.Interfaces;

public interface IMessageRepository
{
    Task<IEnumerable<Message>> GetByProjectAsync(Guid projectId, int limit = 50);
    Task<Message> CreateAsync(Message message);
    Task MarkReadAsync(Guid projectId, Guid currentUserId);
}
