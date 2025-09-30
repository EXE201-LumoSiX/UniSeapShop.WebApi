namespace UniSeapShop.Domain.Enums;

public enum OrderStatus
{
    Pending, // Order created but not confirmed
    Confirmed, // Order confirmed but not yet shipped
    Processing, // Order is being processed
    Shipped, // Order has been shipped
    Delivered, // Order has been delivered
    Completed, // Order has been completed and confirmed by buyer
    Cancelled, // Order has been cancelled
    Refunded, // Order has been refunded
    Disputed // Order is under dispute
}