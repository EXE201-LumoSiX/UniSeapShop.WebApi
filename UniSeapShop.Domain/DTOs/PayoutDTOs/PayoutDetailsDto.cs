namespace UniSeapShop.Domain.DTOs.PayoutDTOs
{
    public class PayoutDetailsDto
    {
        public Guid Id { get; set; }
        public string RecieverName { get; set; }
        public double TotalPrice { get; set; }
        public string Status { get; set; }
        public string? AccountBank { get; set; } = string.Empty;
        public string? AccountNumber { get; set; } = string.Empty;
        public string? AccountName { get; set; } = string.Empty;
        public Guid OrderID { get; set; }
    }
}
