using Microsoft.AspNetCore.Mvc;
using UniSeapShop.Application.Interfaces.Commons;
using UniSeapShop.Application.Utils;

namespace UniSeapShop.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BlobStorageTestController : ControllerBase
{
    private readonly IBlobService _blobService;
    private readonly ILogger<BlobStorageTestController> _logger;

    public BlobStorageTestController(IBlobService blobService, ILogger<BlobStorageTestController> logger)
    {
        _blobService = blobService;
        _logger = logger;
    }

    [HttpPost("upload")]
    public async Task<IActionResult> UploadFile(IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest("Không có file nào được gửi lên.");
        }

        try
        {
            _logger.LogInformation($"Bắt đầu xử lý upload file: {file.FileName}, Kích thước: {file.Length} bytes");

            // Tạo tên file với prefix để tổ chức thư mục
            string containerPrefix = "test-uploads";
            string fileName = $"{containerPrefix}/{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";

            using (var stream = file.OpenReadStream())
            {
                // Upload file lên MinIO
                await _blobService.UploadFileAsync(fileName, stream);
            }

            // Lấy URL xem trước
            string previewUrl = await _blobService.GetPreviewUrlAsync(fileName);

            // Lấy URL download có thời hạn
            string downloadUrl = await _blobService.GetFileUrlAsync(fileName);

            return Ok(new
            {
                success = true,
                message = "Upload file thành công",
                fileName = fileName,
                originalFileName = file.FileName,
                previewUrl = previewUrl,
                downloadUrl = downloadUrl
            });
        }
        catch (Exception ex)
        {
            _logger.LogError($"Lỗi khi upload file: {ex.Message}");
            return StatusCode(500, $"Lỗi khi upload file: {ex.Message}");
        }
    }

    [HttpGet("files/{fileName}")]
    public async Task<IActionResult> GetFileUrl(string fileName)
    {
        try
        {
            var downloadUrl = await _blobService.GetFileUrlAsync(fileName);
            if (string.IsNullOrEmpty(downloadUrl))
            {
                return NotFound($"Không tìm thấy file: {fileName}");
            }

            return Ok(new
            {
                fileName = fileName,
                downloadUrl = downloadUrl
            });
        }
        catch (Exception ex)
        {
            _logger.LogError($"Lỗi khi lấy URL file: {ex.Message}");
            return StatusCode(500, $"Lỗi khi lấy URL file: {ex.Message}");
        }
    }

    [HttpDelete("files/{fileName}")]
    public async Task<IActionResult> DeleteFile(string fileName)
    {
        try
        {
            await _blobService.DeleteFileAsync(fileName);
            return Ok(new
            {
                success = true,
                message = $"Đã xóa file: {fileName}"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError($"Lỗi khi xóa file: {ex.Message}");
            return StatusCode(500, $"Lỗi khi xóa file: {ex.Message}");
        }
    }

    [HttpPost("replace")]
    public async Task<IActionResult> ReplaceImage(IFormFile file, [FromForm] string? oldImageUrl)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest("Không có file nào được gửi lên.");
        }

        try
        {
            string containerPrefix = "test-replacements";
            
            using (var stream = file.OpenReadStream())
            {
                // Thay thế ảnh cũ bằng ảnh mới
                string previewUrl = await _blobService.ReplaceImageAsync(
                    stream, 
                    file.FileName, 
                    oldImageUrl, 
                    containerPrefix
                );

                return Ok(new
                {
                    success = true,
                    message = "Thay thế ảnh thành công",
                    originalFileName = file.FileName,
                    previewUrl = previewUrl
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError($"Lỗi khi thay thế ảnh: {ex.Message}");
            return StatusCode(500, $"Lỗi khi thay thế ảnh: {ex.Message}");
        }
    }
}