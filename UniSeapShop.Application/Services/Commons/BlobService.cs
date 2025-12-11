using Microsoft.Extensions.Configuration;
using System.Net.Http.Headers;
using UniSeapShop.Application.Interfaces.Commons;
using UniSeapShop.Application.Utils;

namespace UniSeapShop.Application.Services.Commons;

public class BlobService : IBlobService
{
    private readonly string _apiKey;
    private readonly string _bucket;
    private readonly HttpClient _http;

    public BlobService(IConfiguration configuration)
    {
        _apiKey = configuration["Firebase:ApiKey"]
                  ?? throw new Exception("Firebase ApiKey missing.");

        _bucket = configuration["Firebase:Bucket"]
                  ?? throw new Exception("Firebase Bucket missing.");

        _http = new HttpClient();
    }

    /// <summary>
    /// Upload file to Firebase Storage.
    /// </summary>
    public async Task UploadFileAsync(string fileName, Stream fileStream)
    {
        try
        {
            string url =
                $"https://firebasestorage.googleapis.com/v0/b/{_bucket}/o?name={Uri.EscapeDataString(fileName)}&uploadType=media&key={_apiKey}";

            var content = new StreamContent(fileStream);
            content.Headers.ContentType = new MediaTypeHeaderValue(GetContentType(fileName));

            var response = await _http.PostAsync(url, content);
            var body = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                throw new Exception($"Firebase upload error: {body}");
        }
        catch (Exception ex)
        {
            throw new Exception($"Upload error: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Firebase public download URL
    /// </summary>
    public async Task<string> GetPreviewUrlAsync(string fileName)
    {
        string url =
            $"https://firebasestorage.googleapis.com/v0/b/{_bucket}/o/{Uri.EscapeDataString(fileName)}?alt=media";

        return await Task.FromResult(url);
    }

    /// <summary>
    /// Firebase does not support presigned URL unless using service account.
    /// => We just return the public URL.
    /// </summary>
    public string GetPresignedUrl(string fileName, int expiryInSeconds = 7 * 24 * 60 * 60)
    {
        return $"https://firebasestorage.googleapis.com/v0/b/{_bucket}/o/{Uri.EscapeDataString(fileName)}?alt=media";
    }

    /// <summary>
    /// Delete file from Firebase Storage
    /// </summary>
    public async Task DeleteFileAsync(string fileName)
    {
        try
        {
            string url =
                $"https://firebasestorage.googleapis.com/v0/b/{_bucket}/o/{Uri.EscapeDataString(fileName)}?key={_apiKey}";

            var request = new HttpRequestMessage(HttpMethod.Delete, url);
            var response = await _http.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                string body = await response.Content.ReadAsStringAsync();
                throw new Exception($"Firebase delete error: {body}");
            }
        }
        catch (Exception ex)
        {
            throw new Exception($"Delete error: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Replace image: delete old then upload new
    /// </summary>
    public async Task<string> ReplaceImageAsync(
        Stream newImageStream,
        string originalFileName,
        string? oldImageUrl,
        string containerPrefix)
    {
        try
        {
            // Delete old image (if exists)
            if (!string.IsNullOrWhiteSpace(oldImageUrl))
            {
                try
                {
                    var oldFileName = Path.GetFileName(new Uri(oldImageUrl).LocalPath);
                    var fullOldPath = $"{containerPrefix}/{oldFileName}";
                    await DeleteFileAsync(fullOldPath);
                }
                catch { }
            }

            var newFileName = $"{containerPrefix}/{Guid.NewGuid()}{Path.GetExtension(originalFileName)}";

            await UploadFileAsync(newFileName, newImageStream);

            // return public link
            return GetPresignedUrl(newFileName);
        }
        catch (Exception)
        {
            throw ErrorHelper.Internal("Lỗi khi xử lý ảnh.");
        }
    }

    private string GetContentType(string fileName)
    {
        var ext = Path.GetExtension(fileName)?.ToLower();
        return ext switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".webp" => "image/webp",
            _ => "application/octet-stream"
        };
    }
}
