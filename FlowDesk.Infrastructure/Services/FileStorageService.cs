using Amazon.S3;
using Amazon.S3.Model;
using FlowDesk.Core.Interfaces;
using Microsoft.Extensions.Configuration;

namespace FlowDesk.Infrastructure.Services;

public class FileStorageService : IFileStorageService
{
    private readonly IAmazonS3 _s3;
    private readonly string _bucket;
    private readonly string _publicUrl;

    public FileStorageService(IConfiguration configuration)
    {
        var endpoint = configuration["CLOUDFLARE_R2_ENDPOINT"]
            ?? throw new InvalidOperationException("CLOUDFLARE_R2_ENDPOINT is not set.");
        _bucket = configuration["CLOUDFLARE_R2_BUCKET"]
            ?? throw new InvalidOperationException("CLOUDFLARE_R2_BUCKET is not set.");
        _publicUrl = configuration["CLOUDFLARE_R2_PUBLIC_URL"]
            ?? throw new InvalidOperationException("CLOUDFLARE_R2_PUBLIC_URL is not set.");

        var accessKey = configuration["CLOUDFLARE_R2_ACCESS_KEY"]
            ?? throw new InvalidOperationException("CLOUDFLARE_R2_ACCESS_KEY is not set.");
        var secretKey = configuration["CLOUDFLARE_R2_SECRET_KEY"]
            ?? throw new InvalidOperationException("CLOUDFLARE_R2_SECRET_KEY is not set.");

        _s3 = new AmazonS3Client(accessKey, secretKey, new AmazonS3Config
        {
            ServiceURL = endpoint,
            ForcePathStyle = true
        });
    }

    public Task<(string UploadUrl, string FileUrl)> GenerateUploadUrlAsync(
        Guid deliverableId, string fileName, string contentType)
    {
        var key = $"deliverables/{deliverableId}/{fileName}";

        var request = new GetPreSignedUrlRequest
        {
            BucketName = _bucket,
            Key = key,
            Verb = HttpVerb.PUT,
            ContentType = contentType,
            Expires = DateTime.UtcNow.AddMinutes(15)
        };

        var uploadUrl = _s3.GetPreSignedURL(request);
        var fileUrl = $"{_publicUrl.TrimEnd('/')}/{key}";

        return Task.FromResult((uploadUrl, fileUrl));
    }
}
