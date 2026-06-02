using FlowDesk.Core.DTOs.Messages;

namespace FlowDesk.Core.Interfaces;

public interface IMessageService
{
    Task<IEnumerable<MessageResponse>> GetAllAsync(Guid projectId);
    Task<MessageResponse> CreateAsync(Guid projectId, CreateMessageRequest request);
    Task MarkReadAsync(Guid projectId);
}
