namespace Geniapp.Infrastructure.Database;

public class ShardConfiguration
{
    public required string Name { get; set; }
    public required string ConnectionString { get; set; }
}
