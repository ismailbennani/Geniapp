using Microsoft.Extensions.Configuration;

namespace Geniapp.Infrastructure.Database;

public class DatabaseConnectionStrings
{
    /// <summary>
    ///     The connection strings to the master database.
    /// </summary>
    public required string Master { get; init; }

    /// <summary>
    ///     The connection strings to the shards.
    /// </summary>
    public required IReadOnlyCollection<ShardConfiguration> Shards { get; init; }

    public static DatabaseConnectionStrings FromConfiguration(IConfiguration configuration)
    {
        string master = "";
        List<ShardConfiguration> shards = [];

        foreach (IConfigurationSection element in configuration.GetSection("ConnectionStrings").GetChildren())
        {
            if (string.Equals(element.Key, "master", StringComparison.OrdinalIgnoreCase))
            {
                master = element.Value ?? "";
            }
            else
            {
                shards.Add(
                    new ShardConfiguration
                    {
                        Name = element.Key,
                        ConnectionString = element.Value ?? ""
                    }
                );
            }
        }

        return new DatabaseConnectionStrings
        {
            Master = master,
            Shards = shards
        };
    }
}
