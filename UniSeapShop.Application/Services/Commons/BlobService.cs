using Amazon;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Util;
using Microsoft.Extensions.Configuration;
using UniSeapShop.Application.Interfaces.Commons;
using UniSeapShop.Application.Utils;

namespace UniSeapShop.Application.Services.Commons;

/// <summary>
///     NOTE: This service can be reused in other projects.
///     This service is used to interact with a MinIO server.
///     It requires the following environment variables to be set:
///     - MINIO_API_ENDPOINT: The S3-compatible API endpoint (e.g., "cdn.fpt-devteam.fun"). Used by MinioClient SDK.
///     - MINIO_CONSOLE_URL: The web console UI URL (e.g., "minio.fpt-devteam.fun"). For UI integration only, not used by SDK.
///     - MINIO_ACCESS_KEY: Access key for MinIO authentication
///     - MINIO_SECRET_KEY: Secret key for MinIO authentication
///     - MINIO_USE_SSL: Whether to use SSL (defaults to true for production)
/// </summary>
public class BlobService : IBlobService
{
    private readonly string _bucketName;
    private readonly IAmazonS3 _s3Client;

    public BlobService(IConfiguration configuration)
    {
        var regionName = configuration["AWS:Region"];
        var accessKey = configuration["AWS:AccessKey"];
        var secretKey = configuration["AWS:SecretKey"];
        _bucketName = configuration["AWS:BucketName"] ?? "default-bucket";

        if (string.IsNullOrEmpty(accessKey) || string.IsNullOrEmpty(secretKey))
            throw new Exception("AWS credentials are missing.");

        var region = RegionEndpoint.GetBySystemName(regionName ?? "ap-southeast-1");

        _s3Client = new AmazonS3Client(accessKey, secretKey, region);
    }

    /// <summary>
    /// Upload file to S3 bucket.
    /// Automatically creates the bucket if not exists.
    /// </summary>
    public async Task UploadFileAsync(string fileName, Stream fileStream)
    {
        try
        {
            if (!await AmazonS3Util.DoesS3BucketExistV2Async(_s3Client, _bucketName))
            {
                await _s3Client.PutBucketAsync(new PutBucketRequest
                {
                    BucketName = _bucketName,
                    UseClientRegion = true
                });
            }

            var contentType = GetContentType(fileName);

            var putRequest = new PutObjectRequest
            {
                BucketName = _bucketName,
                Key = fileName,
                InputStream = fileStream,
                ContentType = contentType
            };

            await _s3Client.PutObjectAsync(putRequest);
        }
        catch (AmazonS3Exception ex)
        {
            throw new Exception($"S3 Error: {ex.Message}", ex);
        }
        catch (Exception ex)
        {
            throw new Exception($"Upload error: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Get public URL for file (if ACL is public-read)
    /// </summary>
    public async Task<string> GetPreviewUrlAsync(string fileName)
    {
        try
        {
            // Tạo request cho AWS S3 Presigned URL
            var request = new GetPreSignedUrlRequest
            {
                BucketName = _bucketName,
                Key = fileName,
                Expires = DateTime.UtcNow.AddDays(7), // 7 ngày
                Verb = HttpVerb.GET
            };

            // AWS SDK không cần await, vì là hàm sync
            var previewUrl = _s3Client.GetPreSignedURL(request);

            return await Task.FromResult(previewUrl);
        }
        catch (Exception ex)
        {
            throw new Exception($"Error generating preview URL: {ex.Message}", ex);
        }
    }


    /// <summary>
    /// Generate presigned URL valid for limited time (default 7 days)
    /// </summary>
    public string GetPresignedUrl(string fileName, int expiryInSeconds = 7 * 24 * 60 * 60)
    {
        var request = new GetPreSignedUrlRequest
        {
            BucketName = _bucketName,
            Key = fileName,
            Expires = DateTime.UtcNow.AddSeconds(expiryInSeconds)
        };

        return _s3Client.GetPreSignedURL(request);
    }

    /// <summary>
    /// Delete file from S3
    /// </summary>
    public async Task DeleteFileAsync(string fileName)
    {
        try
        {
            await _s3Client.DeleteObjectAsync(new DeleteObjectRequest
            {
                BucketName = _bucketName,
                Key = fileName
            });
        }
        catch (AmazonS3Exception ex)
        {
            throw new Exception($"S3 Delete Error: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Replace old image with a new one.
    /// </summary>
    public async Task<string> ReplaceImageAsync(Stream newImageStream, string originalFileName, string? oldImageUrl, string containerPrefix)
    {
        try
        {
            if (!string.IsNullOrWhiteSpace(oldImageUrl))
            {
                try
                {
                    var oldFileName = Path.GetFileName(new Uri(oldImageUrl).LocalPath);
                    var fullOldPath = $"{containerPrefix}/{oldFileName}";
                    await DeleteFileAsync(fullOldPath);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to delete old image: {ex.Message}");
                }
            }

            var newFileName = $"{containerPrefix}/{Guid.NewGuid()}{Path.GetExtension(originalFileName)}";

            await UploadFileAsync(newFileName, newImageStream);

            // Return presigned URL (valid 7 days)
            return GetPresignedUrl(newFileName);
        }
        catch (Exception)
        {
            throw ErrorHelper.Internal("Lỗi khi xử lý ảnh.");
        }
    }

    private string GetContentType(string fileName)
    {
        var extension = Path.GetExtension(fileName)?.ToLower();
        return extension switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".pdf" => "application/pdf",
            ".mp4" => "video/mp4",
            _ => "application/octet-stream"
        };
    }
}