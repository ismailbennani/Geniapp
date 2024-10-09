using Geniapp.Infrastructure.Database.MasterDatabase;
using Geniapp.Infrastructure.Database.ShardDatabase;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Geniapp.Infrastructure.Database;

public static class DatabaseHostingExtensions
{
    public static void ConfigureSharding(this IServiceCollection services, DatabaseConnectionStrings databaseConnectionStrings)
    {
        AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);
        services.AddDbContext<MasterDbContext>(opt => opt.UseNpgsql(databaseConnectionStrings.Master));

        ShardConfigurations configuration = new(databaseConnectionStrings.Shards);
        services.AddSingleton(configuration);

        services.AddScoped<FindShardOfTenant>();
        services.AddScoped<ShardContextProvider>();
        services.AddScoped<CreateTenantsService>();
    }

    public static async Task RecreateShardsAsync(this IServiceProvider services)
    {
        using IServiceScope scope = services.GetRequiredService<IServiceScopeFactory>().CreateScope();

        ILogger logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("ShardingBootstrap");

        ShardConfigurations shardConfigurations = scope.ServiceProvider.GetRequiredService<ShardConfigurations>();
        IReadOnlyCollection<ShardConfiguration> shards = shardConfigurations.GetAllShardConfigurations();

        ShardContextProvider shardContextProvider = scope.ServiceProvider.GetRequiredService<ShardContextProvider>();

        logger.LogInformation("Recreating master DB and shards DBs...");
        foreach (ShardConfiguration shard in shards)
        {
            ShardDbContext? context = await shardContextProvider.GetShardContextAsync(shard.Name);
            if (context == null)
            {
                throw new InvalidOperationException($"Could not get context of shard {shard.Name}.");
            }

            await using ShardDbContext _ = context;
            await context.Database.EnsureDeletedAsync();
            await context.Database.EnsureCreatedAsync();

            logger.LogInformation("Recreated shard {Name}.", shard.Name);
        }

        await using MasterDbContext masterContext = scope.ServiceProvider.GetRequiredService<MasterDbContext>();
        await masterContext.Database.EnsureDeletedAsync();
        await masterContext.Database.EnsureCreatedAsync();

        logger.LogInformation("Recreated master.");

        await using IDbContextTransaction transaction = await masterContext.Database.BeginTransactionAsync();

        foreach (ShardConfiguration shard in shards)
        {
            Shard shardEntity = new(shard.Name);
            masterContext.Add(shardEntity);
            logger.LogInformation("Configured shard {Name}.", shard.Name);
        }

        await masterContext.SaveChangesAsync();
        await transaction.CommitAsync();
    }
}
