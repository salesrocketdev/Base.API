using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Base.Domain.Entities;
using Base.Infrastructure.Data;
using System.Text.Json;

namespace Base.Infrastructure.Seeding.Seeders;

public class CompanySeeder : ISeeder
{
    private readonly ApplicationDbContext _db;
    private readonly IConfiguration _configuration;
    private readonly ILogger<CompanySeeder> _logger;

    public CompanySeeder(ApplicationDbContext db, IConfiguration configuration, ILogger<CompanySeeder> logger)
    {
        _db = db;
        _configuration = configuration;
        _logger = logger;
    }

    public int Order => 1;

    public async Task SeedAsync()
    {
        var enabled = _configuration.GetValue("Seed:Enabled", false);
        if (!enabled)
        {
            _logger.LogInformation("Company seeding is disabled");
            return;
        }

        if (await _db.Companies.AnyAsync())
        {
            _logger.LogInformation("Companies already exist in database, skipping seed");
            return;
        }

        var companyName = _configuration["Seed:CompanyName"] ?? "Base";
        _logger.LogInformation("Creating seed company with name: {CompanyName}", companyName);

        try
        {
            var company = new Company
            {
                Name = companyName,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                Settings = JsonDocument.Parse("{}")
            };

            _db.Companies.Add(company);
            await _db.SaveChangesAsync();
            _logger.LogInformation("Seed company created successfully with ID: {CompanyId}", company.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to seed company data");
            throw;
        }
    }
}


