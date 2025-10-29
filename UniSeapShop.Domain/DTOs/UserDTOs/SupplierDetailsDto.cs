namespace UniSeapShop.Domain.DTOs.UserDTOs
{
    public class SupplierDetailsDto
    {
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string? Description { get; set; } = string.Empty;
        public string? Location { get; set; } = string.Empty;
        public string? AccountBank { get; set; } = string.Empty;
        public string? AccountNumber { get; set; } = string.Empty;
        public string? AccountName { get; set; } = string.Empty;
        public float? Rating { get; set; }
    }
}
