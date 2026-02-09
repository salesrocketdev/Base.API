using System.Text.Json;

namespace Base.Domain.Entities;

public class Company : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public JsonDocument? Settings { get; set; } // Flexible settings storage

    // Navigation properties
    public ICollection<CompanyMember> Members { get; set; } = new List<CompanyMember>();
}




