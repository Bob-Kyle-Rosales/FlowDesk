namespace FlowDesk.Core.Interfaces;

public interface IFileStorageService
{
    Task<(string UploadUrl, string FileUrl)> GenerateUploadUrlAsync(
        string folderPath, string fileName, string contentType);

    Task<string> UploadAsync(string folderPath, string fileName, string contentType, Stream content);
}
