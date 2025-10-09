using Microsoft.AspNetCore.Mvc;
using UniSeapShop.Application.Interfaces.Commons;
using System;
using System.Threading.Tasks;

namespace UniSeapShop.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BlobController : ControllerBase
{
    private readonly ILoggerService _logger;
    private readonly IBlobService _blobService;

    public BlobController(ILoggerService logger, IBlobService blobService)
    {
        _logger = logger;
        _blobService = blobService;
    }

    [HttpPost("upload")]
    public async Task<IActionResult> UploadFile(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest("No file was uploaded");

        try
        {
            if (_blobService == null)
            {
                return StatusCode(500, "BlobService is not properly initialized. Check MinIO configuration.");
            }

            // Generate unique filename to avoid collisions
            var fileName = $"test-uploads/{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
            
            _logger.Info($"Starting test upload of file: {file.FileName} as {fileName}");
            
            // Upload file to MinIO
            using (var stream = file.OpenReadStream())
            {
                await _blobService.UploadFileAsync(fileName, stream);
            }
            
            // Get preview URL
            var previewUrl = await _blobService.GetPreviewUrlAsync(fileName);
            
            // Return success response with the file URL
            return Ok(new
            {
                success = true,
                message = "File uploaded successfully",
                fileName = fileName,
                previewUrl = previewUrl,
                originalName = file.FileName
            });
        }
        catch (Exception ex)
        {
            _logger.Error($"Error uploading file: {ex.Message}");
            return StatusCode(500, $"Internal server error: {ex.Message}");
        }
    }

    [HttpGet("preview/{fileName}")]
    public async Task<IActionResult> GetPreviewUrl(string fileName)
    {
        try
        {
            if (_blobService == null)
            {
                return StatusCode(500, "BlobService is not properly initialized. Check MinIO configuration.");
            }

            // Add test-uploads prefix to ensure we're looking in the right folder
            var prefixedFileName = $"test-uploads/{fileName}";
            var previewUrl = await _blobService.GetPreviewUrlAsync(prefixedFileName);
            
            return Ok(new
            {
                success = true,
                fileName = prefixedFileName,
                previewUrl = previewUrl
            });
        }
        catch (Exception ex)
        {
            _logger.Error($"Error getting preview URL: {ex.Message}");
            return StatusCode(500, $"Internal server error: {ex.Message}");
        }
    }

    [HttpGet("list")]
    public IActionResult GetEnvironmentInfo()
    {
        // This endpoint returns MinIO connection info for debugging
        var endpoint = Environment.GetEnvironmentVariable("MINIO_ENDPOINT");
        var host = Environment.GetEnvironmentVariable("MINIO_HOST");
        
        // Check if the endpoint has protocol and suggest fix
        string suggestion = null;
        if (endpoint?.StartsWith("http://") == true || endpoint?.StartsWith("https://") == true)
        {
            try {
                var uri = new Uri(endpoint);
                suggestion = $"Current MINIO_ENDPOINT includes protocol which is invalid. Try using just the hostname: {uri.Host}";
                if (uri.Port != 80 && uri.Port != 443) {
                    suggestion += $":{uri.Port}";
                }
            }
            catch {
                suggestion = "MINIO_ENDPOINT should not include protocol (http:// or https://). Use just the hostname or IP and port.";
            }
        }
        
        return Ok(new
        {
            minioEndpoint = endpoint,
            minioHost = host,
            message = "This information is helpful for debugging connection issues",
            configurationIssue = suggestion
        });
    }

    [HttpGet("health")]
    public IActionResult HealthCheck()
    {
        bool isBlobServiceAvailable = _blobService != null;
        
        return Ok(new
        {
            status = isBlobServiceAvailable ? "available" : "unavailable",
            message = isBlobServiceAvailable 
                ? "BlobService is properly initialized" 
                : "BlobService failed to initialize. Check your MinIO configuration.",
            timestamp = DateTime.UtcNow
        });
    }
}