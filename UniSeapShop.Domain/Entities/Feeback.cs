namespace UniSeapShop.Domain.Entities;

public class Feeback : BaseEntity
{
    public Guid OrderDetailId { get; set; }
    public string Content { get; set; } = string.Empty;
    public int Rating { get; set; }
    public string FeedbackImage { get; set; } = string.Empty;

    // Navigation properties
    public required OrderDetail OrderDetail { get; set; }
}