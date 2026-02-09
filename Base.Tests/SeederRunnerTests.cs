using Base.Infrastructure.Seeding;

namespace Base.Tests;

public class SeederRunnerTests
{
    [Fact]
    public async Task RunAsync_ExecutesSeedersByOrder()
    {
        var execution = new List<int>();
        var seeders = new ISeeder[]
        {
            new TestSeeder(2, execution),
            new TestSeeder(1, execution),
            new TestSeeder(3, execution)
        };

        var runner = new SeederRunner(seeders);

        await runner.RunAsync();

        Assert.Equal(new[] { 1, 2, 3 }, execution);
    }

    private sealed class TestSeeder : ISeeder
    {
        private readonly List<int> _execution;

        public TestSeeder(int order, List<int> execution)
        {
            Order = order;
            _execution = execution;
        }

        public int Order { get; }

        public Task SeedAsync()
        {
            _execution.Add(Order);
            return Task.CompletedTask;
        }
    }
}
