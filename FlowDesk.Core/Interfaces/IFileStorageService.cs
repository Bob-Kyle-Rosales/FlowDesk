namespace FlowDesk.Core.Interfaces;

public interface IFileStorageService
{
    Task<(string UploadUrl, string FileUrl)> GenerateUploadUrlAsync(
        Guid deliverableId, string fileName, string contentType);
}
