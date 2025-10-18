using System.Text.RegularExpressions;
using Minio;
using Minio.DataModel.Args;
using Minio.Exceptions;
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
    private readonly string _bucketName = "uniseapshop-bucket";
    private readonly ILoggerService _logger;
    private readonly IMinioClient _minioClient;

    public BlobService(ILoggerService logger)
    {
        _logger = logger;

        // Cần cấu hình các biến môi trường sau:
        // - MINIO_ENDPOINT (vd: 103.211.201.162:9000)
        // - MINIO_ACCESS_KEY
        // - MINIO_SECRET_KEY
        var endpoint = Environment.GetEnvironmentVariable("MINIO_ENDPOINT") ?? "103.211.201.162:9000";
        var accessKey = Environment.GetEnvironmentVariable("MINIO_ACCESS_KEY");
        var secretKey = Environment.GetEnvironmentVariable("MINIO_SECRET_KEY");

        _logger.Info("Initializing BlobService...");
        _logger.Info($"Connecting to MinIO at: {endpoint}");

        try
        {
            // Kết nối MinIO không dùng SSL (vì đang dùng IP:port hoặc HTTP)
            _minioClient = new MinioClient()
                .WithEndpoint(endpoint)
                .WithCredentials(accessKey, secretKey)
                .WithSSL(false)
                .Build();

            _logger.Success("MinIO client initialized successfully.");
        }
        catch (Exception ex)
        {
            _logger.Error($"Failed to initialize MinIO client: {ex.Message}");
            throw;
        }
    }

    public async Task UploadFileAsync(string fileName, Stream fileStream)
    {
        _logger.Info($"Starting file upload: {fileName}");

        try
        {
            // Kiểm tra bucket tồn tại, nếu chưa thì tạo mới
            var beArgs = new BucketExistsArgs().WithBucket(_bucketName);
            var found = await _minioClient.BucketExistsAsync(beArgs);
            _logger.Info($"Checking if bucket '{_bucketName}' exists: {found}");

            if (!found)
            {
                _logger.Warn($"Bucket '{_bucketName}' not found. Creating a new one...");
                var mbArgs = new MakeBucketArgs().WithBucket(_bucketName);
                await _minioClient.MakeBucketAsync(mbArgs);
                _logger.Success($"Bucket '{_bucketName}' created successfully.");
            }

            // Lấy content type dựa trên phần mở rộng của file
            var contentType = GetContentType(fileName);

            var putObjectArgs = new PutObjectArgs()
                .WithBucket(_bucketName)
                .WithObject(fileName)
                .WithStreamData(fileStream)
                .WithObjectSize(fileStream.Length)
                .WithContentType(contentType);

            await _minioClient.PutObjectAsync(putObjectArgs);
            _logger.Success($"File '{fileName}' uploaded successfully.");
        }
        catch (MinioException minioEx)
        {
            _logger.Error($"MinIO Error during upload: {minioEx.Message}");
            throw;
        }
        catch (Exception ex)
        {
            _logger.Error($"Unexpected error during file upload: {ex.Message}");
            throw;
        }
    }

    public async Task<string> GetPreviewUrlAsync(string fileName)
    {
        // Biến MINIO_HOST phải trỏ tới reverse proxy HTTPS, vd: https://minio.fpt-devteam.fun
        var minioHost = Environment.GetEnvironmentVariable("MINIO_HOST") ?? "https://minio.fpt-devteam.fun";

        // Sử dụng Base64 encoding thay vì URL encoding để phù hợp với định dạng API
        var base64File = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(fileName));

        // URL được định dạng đúng với API reverse proxy
        var previewUrl =
            $"{minioHost}/api/v1/buckets/{_bucketName}/objects/download?preview=true&prefix={base64File}&version_id=null";
        _logger.Info($"Preview URL generated: {previewUrl}");

        return previewUrl;
    }

    public async Task<string> GetFileUrlAsync(string fileName)
    {
        try
        {
            var args = new PresignedGetObjectArgs()
                .WithBucket(_bucketName)
                .WithObject(fileName)
                .WithExpiry(7 * 24 * 60 * 60);

            var fileUrl = await _minioClient.PresignedGetObjectAsync(args);
            _logger.Success($"Presigned file URL generated: {fileUrl}");
            return fileUrl;
        }
        catch (Exception ex)
        {
            _logger.Error($"Error generating file URL: {ex.Message}");
            return null;
        }
    }

    public async Task DeleteFileAsync(string fileName)
    {
        _logger.Info($"Deleting file: {fileName}");

        try
        {
            var removeObjectArgs = new RemoveObjectArgs()
                .WithBucket(_bucketName)
                .WithObject(fileName);

            await _minioClient.RemoveObjectAsync(removeObjectArgs);
            _logger.Success($"File '{fileName}' deleted successfully.");
        }
        catch (MinioException minioEx)
        {
            _logger.Error($"MinIO Error during delete: {minioEx.Message}");
            throw;
        }
        catch (Exception ex)
        {
            _logger.Error($"Unexpected error during file delete: {ex.Message}");
            throw;
        }
    }

    public async Task<string> ReplaceImageAsync(Stream newImageStream, string originalFileName, string? oldImageUrl,
        string containerPrefix)
    {
        try
        {
            // Xóa ảnh cũ nếu có
            if (!string.IsNullOrWhiteSpace(oldImageUrl))
                try
                {
                    var oldFileName = Path.GetFileName(new Uri(oldImageUrl).LocalPath);
                    var fullOldPath = $"{containerPrefix}/{oldFileName}";
                    await DeleteFileAsync(fullOldPath);
                    _logger.Info($"[ReplaceImageAsync] Deleted old image: {fullOldPath}");
                }
                catch (Exception ex)
                {
                    _logger.Warn($"[ReplaceImageAsync] Failed to delete old image: {ex.Message}");
                }

            // Upload ảnh mới
            var newFileName = $"{containerPrefix}/{Guid.NewGuid()}{Path.GetExtension(originalFileName)}";
            _logger.Info($"[ReplaceImageAsync] Uploading new image: {newFileName}");

            await UploadFileAsync(newFileName, newImageStream);

            var previewUrl = await GetPreviewUrlAsync(newFileName);
            _logger.Success($"[ReplaceImageAsync] Uploaded and generated preview URL: {previewUrl}");
            return previewUrl;
        }
        catch (Exception ex)
        {
            _logger.Error($"[ReplaceImageAsync] Error occurred: {ex.Message}");
            throw ErrorHelper.Internal("Lỗi khi xử lý ảnh.");
        }
    }


    private string GetContentType(string fileName)
    {
        _logger.Info($"Determining content type for file: {fileName}");
        var extension = Path.GetExtension(fileName)?.ToLower();

        return extension switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".pdf" => "application/pdf",
            ".mp4" => "video/mp4",
            _ => "application/octet-stream" // fallback nếu định dạng không rõ
        };
    }
}