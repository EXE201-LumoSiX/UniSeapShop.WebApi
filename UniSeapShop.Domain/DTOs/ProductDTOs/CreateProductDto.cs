using Microsoft.AspNetCore.Http;

namespace UniSeapShop.Domain.DTOs.ProductDTOs;

public class CreateProductDto
{
    public required string ProductName { get; set; }
    public required string Description { get; set; }
    public double OriginalPrice { get; set; }

    // Bỏ ProductImage string, thay bằng IFormFile
    public IFormFile? ProductImageFile { get; set; }

    public string UsageHistory { get; set; } = string.Empty;
    public Guid CategoryId { get; set; }
    public int Quantity { get; set; }
    public Guid SupplierId { get; set; }
    public double Discount { get; set; } = 0; // Optional discount percentage
}