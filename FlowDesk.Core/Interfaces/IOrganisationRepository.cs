using FlowDesk.Core.Entities;

namespace FlowDesk.Core.Interfaces;

public interface IOrganisationRepository
{
    Task<Organisation?> GetByIdAsync(Guid id);
    Task UpdateAsync(Organisation organisation);
}
