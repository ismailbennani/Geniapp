using Geniapp.Infrastructure.Database.MasterDatabase;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Geniapp.Infrastructure.Database;

public static class DatabaseHostingExtensions
{
    public static void ConfigureSharding(this IServiceCollection services, string masterConnectionString, IReadOnlyCollection<ShardConfiguration> shards)
    {
        services.AddDbContext<MasterDbContext>(opt => opt.UseNpgsql(masterConnectionString));

        ShardConfigurations configuration = new(shards);
        services.AddSingleton(configuration);

        services.AddScoped<FindShardOfTenant>();
        services.AddScoped<ShardContextProvider>();
    }
}
