namespace Geniapp.Infrastructure.Database.MasterDatabase;

public class ShardConfigurations(IReadOnlyCollection<ShardConfiguration> configurations)
{
    readonly Dictionary<string, ShardConfiguration> _shards = configurations.ToDictionary(c => c.Name, c => c);

    public IReadOnlyCollection<ShardConfiguration> GetAllShardConfigurations() => _shards.Values;
    public ShardConfiguration? GetShardConfiguration(string name) => _shards.GetValueOrDefault(name);
}
