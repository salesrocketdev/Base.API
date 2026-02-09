namespace Base.Domain.Entities;

public class CompanyMember : BaseEntity
{
    public int CompanyId { get; set; }
    public int UserId { get; set; }
    public string Role { get; set; } = "Owner"; // Owner, Admin, Member, etc.

    // Navigation properties
    public Company Company { get; set; } = null!;
    public User User { get; set; } = null!;
}




