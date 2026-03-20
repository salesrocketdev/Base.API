using System.Text;

namespace Base.Infrastructure.Configuration;

public static class LocalEnvironmentBootstrap
{
    private const string DefaultConnectionKey = "ConnectionStrings__DefaultConnection";
    private const string LegacyDefaultConnectionKey = "DEFAULT_CONNECTION";

    public static void Initialize(string? startDirectory = null)
    {
        LoadDotEnvIfPresent(startDirectory);
        EnsureDerivedDatabaseVariables();
    }

    public static string? GetDefaultConnectionString()
    {
        Initialize();

        return Environment.GetEnvironmentVariable(DefaultConnectionKey)
               ?? Environment.GetEnvironmentVariable(LegacyDefaultConnectionKey);
    }

    private static void EnsureDerivedDatabaseVariables()
    {
        var existing = Environment.GetEnvironmentVariable(DefaultConnectionKey)
                       ?? Environment.GetEnvironmentVariable(LegacyDefaultConnectionKey);

        if (!string.IsNullOrWhiteSpace(existing))
        {
            return;
        }

        var user = Environment.GetEnvironmentVariable("POSTGRES_USER");
        var password = Environment.GetEnvironmentVariable("POSTGRES_PASSWORD");
        var database = Environment.GetEnvironmentVariable("POSTGRES_DB");

        if (string.IsNullOrWhiteSpace(user) || string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(database))
        {
            return;
        }

        var isRunningInContainer = string.Equals(
            Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER"),
            "true",
            StringComparison.OrdinalIgnoreCase);

        var host = Environment.GetEnvironmentVariable("POSTGRES_HOST");
        if (string.IsNullOrWhiteSpace(host))
        {
            host = isRunningInContainer ? "db" : "localhost";
        }

        var port = Environment.GetEnvironmentVariable("POSTGRES_PORT");
        if (string.IsNullOrWhiteSpace(port))
        {
            port = "5432";
        }

        var sslMode = Environment.GetEnvironmentVariable("POSTGRES_SSL_MODE")
                      ?? Environment.GetEnvironmentVariable("POSTGRES_SSLMODE");
        if (string.IsNullOrWhiteSpace(sslMode))
        {
            sslMode = "Disable";
        }

        var builder = new StringBuilder();
        builder.Append("Host=").Append(host)
            .Append(";Port=").Append(port)
            .Append(";Database=").Append(database)
            .Append(";Username=").Append(user)
            .Append(";Password=").Append(password)
            .Append(";Ssl Mode=").Append(sslMode);

        var connectionString = builder.ToString();
        Environment.SetEnvironmentVariable(DefaultConnectionKey, connectionString);
        Environment.SetEnvironmentVariable(LegacyDefaultConnectionKey, connectionString);
    }

    private static void LoadDotEnvIfPresent(string? startDirectory = null)
    {
        foreach (var candidate in GetDotEnvCandidates(startDirectory))
        {
            if (!File.Exists(candidate))
            {
                continue;
            }

            foreach (var rawLine in File.ReadAllLines(candidate))
            {
                var line = rawLine.Trim();
                if (string.IsNullOrWhiteSpace(line) || line.StartsWith('#'))
                {
                    continue;
                }

                var separatorIndex = line.IndexOf('=');
                if (separatorIndex <= 0)
                {
                    continue;
                }

                var key = line[..separatorIndex].Trim();
                if (string.IsNullOrWhiteSpace(key) || !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(key)))
                {
                    continue;
                }

                var value = line[(separatorIndex + 1)..].Trim();
                Environment.SetEnvironmentVariable(key, TrimWrappingQuotes(value));
            }

            return;
        }
    }

    private static IEnumerable<string> GetDotEnvCandidates(string? startDirectory)
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var current = new DirectoryInfo(startDirectory ?? Directory.GetCurrentDirectory());

        while (current is not null)
        {
            var candidate = Path.Combine(current.FullName, ".env");
            if (seen.Add(candidate))
            {
                yield return candidate;
            }

            current = current.Parent;
        }
    }

    private static string TrimWrappingQuotes(string value)
    {
        if (value.Length >= 2
            && ((value[0] == '"' && value[^1] == '"')
                || (value[0] == '\'' && value[^1] == '\'')))
        {
            return value[1..^1];
        }

        return value;
    }
}
