using Microsoft.AspNetCore.Mvc;
using UniSeapShop.Application.Interfaces.Commons;
using UniSeapShop.Application.Utils;

namespace UniSeapShop.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BlobController : ControllerBase
{
    private readonly IBlobService _blobService;
    private readonly ILoggerService _logger;

    public BlobController(ILoggerService logger, IBlobService blobService)
    {
        _logger = logger;
        _blobService = blobService;
    }

    [HttpPost("upload")]
    public async Task<IActionResult> UploadFile(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest(ApiResult<object>.Failure("400", "No file was uploaded"));

        try
        {
            if (_blobService == null)
                return StatusCode(500,
                    ApiResult<object>.Failure("500",
                        "BlobService is not properly initialized. Check MinIO configuration."));

            var fileName = $"test-uploads/{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";

            _logger.Info($"Starting test upload of file: {file.FileName} as {fileName}");

            using (var stream = file.OpenReadStream())
            {
                await _blobService.UploadFileAsync(fileName, stream);
            }

            var previewUrl = await _blobService.GetPreviewUrlAsync(fileName);

            return Ok(ApiResult<object>.Success(new
            {
                fileName,
                previewUrl,
                originalName = file.FileName
            }, "200", "File uploaded successfully"));
        }
        catch (Exception ex)
        {
            _logger.Error($"Error uploading file: {ex.Message}");
            var statusCode = ExceptionUtils.ExtractStatusCode(ex);
            var errorResponse = ExceptionUtils.CreateErrorResponse<object>(ex);
            return StatusCode(statusCode, errorResponse);
        }
    }

    [HttpGet("preview/{fileName}")]
    public async Task<IActionResult> GetPreviewUrl(string fileName)
    {
        try
        {
            if (_blobService == null)
                return StatusCode(500,
                    ApiResult<object>.Failure("500",
                        "BlobService is not properly initialized. Check MinIO configuration."));

            var prefixedFileName = $"test-uploads/{fileName}";
            var previewUrl = await _blobService.GetPreviewUrlAsync(prefixedFileName);

            return Ok(ApiResult<object>.Success(new
            {
                fileName = prefixedFileName, previewUrl
            }));
        }
        catch (Exception ex)
        {
            _logger.Error($"Error getting preview URL: {ex.Message}");
            var statusCode = ExceptionUtils.ExtractStatusCode(ex);
            var errorResponse = ExceptionUtils.CreateErrorResponse<object>(ex);
            return StatusCode(statusCode, errorResponse);
        }
    }

    [HttpGet("list")]
    public IActionResult GetEnvironmentInfo()
    {
        var endpoint = Environment.GetEnvironmentVariable("MINIO_ENDPOINT");
        var host = Environment.GetEnvironmentVariable("MINIO_HOST");

        string suggestion = null;
        if (endpoint?.StartsWith("http://") == true || endpoint?.StartsWith("https://") == true)
            try
            {
                var uri = new Uri(endpoint);
                suggestion =
                    $"Current MINIO_ENDPOINT includes protocol which is invalid. Try using just the hostname: {uri.Host}";
                if (uri.Port != 80 && uri.Port != 443) suggestion += $":{uri.Port}";
            }
            catch
            {
                suggestion =
                    "MINIO_ENDPOINT should not include protocol (http:// or https://). Use just the hostname or IP and port.";
            }

        return Ok(ApiResult<object>.Success(new
        {
            minioEndpoint = endpoint,
            minioHost = host,
            message = "This information is helpful for debugging connection issues",
            configurationIssue = suggestion
        }));
    }

    [HttpGet("health")]
    public IActionResult HealthCheck()
    {
        var isBlobServiceAvailable = _blobService != null;

        return Ok(ApiResult<object>.Success(new
        {
            status = isBlobServiceAvailable ? "available" : "unavailable",
            message = isBlobServiceAvailable
                ? "BlobService is properly initialized"
                : "BlobService failed to initialize. Check your MinIO configuration.",
            timestamp = DateTime.UtcNow
        }));
    }
}