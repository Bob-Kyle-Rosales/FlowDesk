using FlowDesk.Core.DTOs.Deliverables;

namespace FlowDesk.Core.Interfaces;

public interface IDeliverableService
{
    Task<IEnumerable<DeliverableResponse>> GetAllByProjectAsync(Guid projectId);
    Task<DeliverableResponse> CreateAsync(Guid projectId, CreateDeliverableRequest request);
    Task<DeliverableResponse> UpdateAsync(Guid id, UpdateDeliverableRequest request);
    Task<UploadUrlResponse> GetUploadUrlAsync(Guid id, string fileName, string contentType);
    Task<DeliverableResponse> ConfirmUploadAsync(Guid id, string fileUrl);
    Task<DeliverableResponse> ApproveAsync(Guid id);
    Task<DeliverableResponse> RequestRevisionAsync(Guid id, RevisionRequest request);
}
