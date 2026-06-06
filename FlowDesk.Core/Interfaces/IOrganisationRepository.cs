// FlowDesk.Core/Interfaces/IOrganisationRepository.cs
using FlowDesk.Core.Entities;

namespace FlowDesk.Core.Interfaces;

public interface IOrganisationRepository
{
    Task<Organisation?> GetByIdAsync(Guid id);
    Task<Organisation?> GetBySlugAsync(string slug);
    Task UpdateAsync(Organisation organisation);
}
