using FlowDesk.Core.Entities;
using FlowDesk.Core.Interfaces;
using FlowDesk.Infrastructure.Data;

namespace FlowDesk.Infrastructure.Repositories;

public class OrganisationRepository : IOrganisationRepository
{
    private readonly AppDbContext _context;

    public OrganisationRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Organisation?> GetByIdAsync(Guid id)
        => await _context.Organisations.FindAsync(id);

    public async Task UpdateAsync(Organisation organisation)
    {
        _context.Organisations.Update(organisation);
        await _context.SaveChangesAsync();
    }
}
