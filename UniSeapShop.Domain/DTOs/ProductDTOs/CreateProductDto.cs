using Microsoft.AspNetCore.Http;

namespace UniSeapShop.Domain.DTOs.ProductDTOs
{
    public class CreateProductDto
    {
        public string ProductName { get; set; }
        public string Description { get; set; }
        public double Price { get; set; }
        public string ProductImage { get; set; } = string.Empty;
        public string UsageHistory { get; set; } = string.Empty;
        public Guid CategoryId { get; set; }
        public int Quantity { get; set; }
        public Guid SupplierId { get; set; }
        public double Discount { get; set; } = 0;// Optional discount percentage
        public required IFormFile ImageFile { get; set; }
    }
}
