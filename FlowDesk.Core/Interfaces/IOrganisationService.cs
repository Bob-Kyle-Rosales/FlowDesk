// FlowDesk.Core/Interfaces/IOrganisationService.cs
using FlowDesk.Core.DTOs.Organisations;

namespace FlowDesk.Core.Interfaces;

public interface IOrganisationService
{
    Task<OrganisationResponse> GetMineAsync();
    Task<OrganisationResponse> UpdateMineAsync(UpdateOrganisationRequest request);
    Task<(string UploadUrl, string FileUrl)> GetLogoUploadUrlAsync(string fileName, string contentType);
    Task<OrganisationResponse> UpdateLogoAsync(UpdateLogoRequest request);
    Task<PublicOrganisationResponse> GetPublicAsync(string slug);
}
