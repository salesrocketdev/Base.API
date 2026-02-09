namespace Base.Infrastructure.Seeding;

public interface ISeeder
{
    int Order { get; }
    Task SeedAsync();
}

