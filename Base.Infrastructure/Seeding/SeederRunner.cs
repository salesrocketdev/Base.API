namespace Base.Infrastructure.Seeding;

public class SeederRunner
{
    private readonly IEnumerable<ISeeder> _seeders;

    public SeederRunner(IEnumerable<ISeeder> seeders)
    {
        _seeders = seeders;
    }

    public async Task RunAsync()
    {
        foreach (var seeder in _seeders.OrderBy(seeder => seeder.Order))
        {
            await seeder.SeedAsync();
        }
    }
}

