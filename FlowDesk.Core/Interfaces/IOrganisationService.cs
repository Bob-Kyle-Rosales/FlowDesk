using FlowDesk.Core.DTOs.Organisations;

namespace FlowDesk.Core.Interfaces;

public interface IOrganisationService
{
    Task<OrganisationResponse> GetMineAsync();
    Task<OrganisationResponse> UpdateMineAsync(UpdateOrganisationRequest request);
}
