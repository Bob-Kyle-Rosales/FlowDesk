using FlowDesk.Core.DTOs.Organisations;
using FlowDesk.Core.Entities;
using FlowDesk.Core.Interfaces;

namespace FlowDesk.Core.Services;

public class OrganisationService : IOrganisationService
{
    private readonly IOrganisationRepository _repo;
    private readonly ICurrentUserService _currentUser;

    public OrganisationService(IOrganisationRepository repo, ICurrentUserService currentUser)
    {
        _repo = repo;
        _currentUser = currentUser;
    }

    public async Task<OrganisationResponse> GetMineAsync()
    {
        var org = await _repo.GetByIdAsync(_currentUser.OrganisationId!.Value)
            ?? throw new KeyNotFoundException("Organisation not found.");
        return ToResponse(org);
    }

    public async Task<OrganisationResponse> UpdateMineAsync(UpdateOrganisationRequest request)
    {
        var org = await _repo.GetByIdAsync(_currentUser.OrganisationId!.Value)
            ?? throw new KeyNotFoundException("Organisation not found.");

        org.Name = request.Name;
        org.PrimaryColor = request.PrimaryColor;

        await _repo.UpdateAsync(org);
        return ToResponse(org);
    }

    private static OrganisationResponse ToResponse(Organisation org)
        => new(org.Id, org.Name, org.Slug, org.PrimaryColor, org.LogoUrl, org.Plan, org.CreatedAt, org.StripeAccountId);
}
