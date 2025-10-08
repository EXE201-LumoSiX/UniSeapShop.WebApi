using Microsoft.AspNetCore.Http;

namespace UniSeapShop.Domain.DTOs.ProductDTOs
{
    public class UpdateProductDto
    {
        public string? ProductName { get; set; }
        public string? Description { get; set; }
        public string? UsageHistory { get; set; }
        public double Price { get; set; }
        public Guid CategoryId { get; set; }
        public int Quantity { get; set; }
        public IFormFile ImageFile { get; set; }
    }
}
