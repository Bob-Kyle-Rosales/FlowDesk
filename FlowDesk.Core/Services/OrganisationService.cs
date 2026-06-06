// FlowDesk.Core/Services/OrganisationService.cs
using FlowDesk.Core.DTOs.Organisations;
using FlowDesk.Core.Entities;
using FlowDesk.Core.Interfaces;

namespace FlowDesk.Core.Services;

public class OrganisationService : IOrganisationService
{
    private readonly IOrganisationRepository _repo;
    private readonly ICurrentUserService _currentUser;
    private readonly IFileStorageService _fileStorage;

    public OrganisationService(
        IOrganisationRepository repo,
        ICurrentUserService currentUser,
        IFileStorageService fileStorage)
    {
        _repo = repo;
        _currentUser = currentUser;
        _fileStorage = fileStorage;
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

    public async Task<(string UploadUrl, string FileUrl)> GetLogoUploadUrlAsync(
        string fileName, string contentType)
    {
        var orgId = _currentUser.OrganisationId!.Value;
        var uploadId = Guid.NewGuid().ToString("N")[..8];
        return await _fileStorage.GenerateUploadUrlAsync($"logos/{orgId}/{uploadId}", fileName, contentType);
    }

    public async Task<OrganisationResponse> UpdateLogoAsync(UpdateLogoRequest request)
    {
        var org = await _repo.GetByIdAsync(_currentUser.OrganisationId!.Value)
            ?? throw new KeyNotFoundException("Organisation not found.");
        org.LogoUrl = request.LogoUrl;
        await _repo.UpdateAsync(org);
        return ToResponse(org);
    }

    public async Task<PublicOrganisationResponse> GetPublicAsync(string slug)
    {
        var org = await _repo.GetBySlugAsync(slug)
            ?? throw new KeyNotFoundException($"Organisation with slug '{slug}' not found.");
        return new PublicOrganisationResponse(org.Name, org.Slug, org.PrimaryColor, org.LogoUrl);
    }

    private static OrganisationResponse ToResponse(Organisation org)
        => new(org.Id, org.Name, org.Slug, org.PrimaryColor, org.LogoUrl,
               org.Plan, org.CreatedAt, org.StripeAccountId);
}
