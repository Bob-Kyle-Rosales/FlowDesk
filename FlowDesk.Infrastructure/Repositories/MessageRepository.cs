using FlowDesk.Core.Entities;
using FlowDesk.Core.Interfaces;
using FlowDesk.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FlowDesk.Infrastructure.Repositories;

public class MessageRepository : IMessageRepository
{
    private readonly AppDbContext _context;

    public MessageRepository(AppDbContext context) => _context = context;

    public async Task<IEnumerable<Message>> GetByProjectAsync(Guid projectId, int limit = 50)
    {
        // OrderByDescending + Take gets the last N rows efficiently;
        // the in-memory reverse restores chronological order for display.
        var messages = await _context.Messages
            .Include(m => m.Sender)
            .Where(m => m.ProjectId == projectId)
            .Where(m => _context.Projects.Any(p => p.Id == m.ProjectId))
            .OrderByDescending(m => m.CreatedAt)
            .Take(limit)
            .ToListAsync();

        return messages.OrderBy(m => m.CreatedAt);
    }

    public async Task<Message> CreateAsync(Message message)
    {
        await _context.Messages.AddAsync(message);
        await _context.SaveChangesAsync();
        return await _context.Messages
            .Include(m => m.Sender)
            .FirstAsync(m => m.Id == message.Id);
    }

    public async Task MarkReadAsync(Guid projectId, Guid currentUserId)
    {
        await _context.Messages
            .Where(m => m.ProjectId == projectId
                     && m.SenderId != currentUserId
                     && !m.IsRead)
            .ExecuteUpdateAsync(s => s.SetProperty(m => m.IsRead, true));
    }
}
