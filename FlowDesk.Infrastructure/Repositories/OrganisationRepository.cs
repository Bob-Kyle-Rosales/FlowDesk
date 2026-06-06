// FlowDesk.Infrastructure/Repositories/OrganisationRepository.cs
using FlowDesk.Core.Entities;
using FlowDesk.Core.Interfaces;
using FlowDesk.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

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

    public async Task<Organisation?> GetBySlugAsync(string slug)
        => await _context.Organisations
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(o => o.Slug == slug);

    public async Task UpdateAsync(Organisation organisation)
    {
        _context.Organisations.Update(organisation);
        await _context.SaveChangesAsync();
    }
}
