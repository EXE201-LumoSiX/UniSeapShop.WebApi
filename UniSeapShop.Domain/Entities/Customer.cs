namespace UniSeapShop.Domain.Entities;

public class Customer : BaseEntity
{
    public Guid UserId { get; set; }
    public int LoyaltyPoint { get; set; } = 0;
    public string MembershipLevel { get; set; } = "Basic";

    // Navigation properties
    public required User User { get; set; }
    public List<Order> Orders { get; set; } = new();
    public Cart? Cart { get; set; }
}