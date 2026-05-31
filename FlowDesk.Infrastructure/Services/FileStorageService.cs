using Amazon.S3;
using Amazon.S3.Model;
using FlowDesk.Core.Interfaces;
using Microsoft.Extensions.Configuration;

namespace FlowDesk.Infrastructure.Services;

public class FileStorageService : IFileStorageService
{
    private readonly IConfiguration _configuration;
    private IAmazonS3? _s3;
    private string? _bucket;
    private string? _publicUrl;

    public FileStorageService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public Task<(string UploadUrl, string FileUrl)> GenerateUploadUrlAsync(
        Guid deliverableId, string fileName, string contentType)
    {
        EnsureInitialized();

        var safeName = Path.GetFileName(fileName);
        var key = $"deliverables/{deliverableId}/{safeName}";

        var request = new GetPreSignedUrlRequest
        {
            BucketName = _bucket,
            Key = key,
            Verb = HttpVerb.PUT,
            ContentType = contentType,
            Expires = DateTime.UtcNow.AddMinutes(15)
        };

        var uploadUrl = _s3!.GetPreSignedURL(request);
        var fileUrl = $"{_publicUrl!.TrimEnd('/')}/{key}";

        return Task.FromResult((uploadUrl, fileUrl));
    }

    // Initialized on first use so missing R2 credentials only fail the upload-url endpoint,
    // not all deliverable endpoints (create/update/approve don't need R2).
    private void EnsureInitialized()
    {
        if (_s3 is not null) return;

        var endpoint = _configuration["CLOUDFLARE_R2_ENDPOINT"]
            ?? throw new InvalidOperationException("CLOUDFLARE_R2_ENDPOINT is not set.");
        _bucket = _configuration["CLOUDFLARE_R2_BUCKET"]
            ?? throw new InvalidOperationException("CLOUDFLARE_R2_BUCKET is not set.");
        _publicUrl = _configuration["CLOUDFLARE_R2_PUBLIC_URL"]
            ?? throw new InvalidOperationException("CLOUDFLARE_R2_PUBLIC_URL is not set.");

        var accessKey = _configuration["CLOUDFLARE_R2_ACCESS_KEY"]
            ?? throw new InvalidOperationException("CLOUDFLARE_R2_ACCESS_KEY is not set.");
        var secretKey = _configuration["CLOUDFLARE_R2_SECRET_KEY"]
            ?? throw new InvalidOperationException("CLOUDFLARE_R2_SECRET_KEY is not set.");

        _s3 = new AmazonS3Client(accessKey, secretKey, new AmazonS3Config
        {
            ServiceURL = endpoint,
            ForcePathStyle = true
        });
    }
}
