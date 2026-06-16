using Amazon.S3;
using Amazon.S3.Model;
using FlowDesk.Core.Interfaces;
using Microsoft.Extensions.Configuration;

namespace FlowDesk.Infrastructure.Services;

public class FileStorageService : IFileStorageService
{
    private readonly Lazy<(IAmazonS3 S3, string Bucket, string PublicUrl)> _r2;

    public FileStorageService(IConfiguration configuration)
    {
        // Lazy so missing R2 credentials only fail the upload-url endpoint,
        // not all deliverable endpoints (create/update/approve don't need R2).
        _r2 = new Lazy<(IAmazonS3, string, string)>(() =>
        {
            var endpoint = configuration["CLOUDFLARE_R2_ENDPOINT"]
                ?? throw new InvalidOperationException("CLOUDFLARE_R2_ENDPOINT is not set.");
            var bucket = configuration["CLOUDFLARE_R2_BUCKET"]
                ?? throw new InvalidOperationException("CLOUDFLARE_R2_BUCKET is not set.");
            var publicUrl = configuration["CLOUDFLARE_R2_PUBLIC_URL"]
                ?? throw new InvalidOperationException("CLOUDFLARE_R2_PUBLIC_URL is not set.");
            var accessKey = configuration["CLOUDFLARE_R2_ACCESS_KEY"]
                ?? throw new InvalidOperationException("CLOUDFLARE_R2_ACCESS_KEY is not set.");
            var secretKey = configuration["CLOUDFLARE_R2_SECRET_KEY"]
                ?? throw new InvalidOperationException("CLOUDFLARE_R2_SECRET_KEY is not set.");

            var credentials = new Amazon.Runtime.BasicAWSCredentials(accessKey, secretKey);
            var s3Config = new AmazonS3Config
            {
                ServiceURL = endpoint,
                ForcePathStyle = true,
            };
            // Must be set after construction — setting in initializer is ignored by some SDK versions
            s3Config.AuthenticationRegion = "auto";
            var s3 = new AmazonS3Client(credentials, s3Config);

            return (s3, bucket, publicUrl);
        });
    }

    public async Task<string> UploadAsync(string folderPath, string fileName, string contentType, Stream content)
    {
        var (s3, bucket, publicUrl) = _r2.Value;
        var safeName = Path.GetFileName(fileName);
        var key = $"{folderPath}/{safeName}";

        await s3.PutObjectAsync(new PutObjectRequest
        {
            BucketName = bucket,
            Key = key,
            InputStream = content,
            ContentType = contentType,
            DisablePayloadSigning = true,
        });

        return $"{publicUrl.TrimEnd('/')}/{key}";
    }

    public Task<(string UploadUrl, string FileUrl)> GenerateUploadUrlAsync(
        string folderPath, string fileName, string contentType)
    {
        var (s3, bucket, publicUrl) = _r2.Value;

        var safeName = Path.GetFileName(fileName);
        var key = $"{folderPath}/{safeName}";

        var request = new GetPreSignedUrlRequest
        {
            BucketName = bucket,
            Key = key,
            Verb = HttpVerb.PUT,
            ContentType = contentType,
            Expires = DateTime.UtcNow.AddMinutes(15)
        };

        var uploadUrl = s3.GetPreSignedURL(request);
        var fileUrl = $"{publicUrl.TrimEnd('/')}/{key}";

        return Task.FromResult((uploadUrl, fileUrl));
    }
}
