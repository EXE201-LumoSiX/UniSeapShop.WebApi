namespace UniSeapShop.Domain.DTOs.PayoutDTOs
{
    public class PayoutDetailsDto
    {
        public Guid Id { get; set; }
        public Guid RecieverId { get; set; }
        public double TotalPrice { get; set; }
        public string Status { get; set; }
        public Guid OrderID { get; set; }
    }
}
