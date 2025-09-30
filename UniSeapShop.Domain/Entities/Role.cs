using UniSeapShop.Domain.Enums;

namespace UniSeapShop.Domain.Entities;

public class Role : BaseEntity
{
    public required string Name { get; set; }
    public required RoleType RoleType { get; set; }
    public string Description { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;

    // Navigation properties
    public List<User> Users { get; set; } = new();
}