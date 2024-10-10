using Geniapp.Infrastructure.Database.MasterDatabase;
using Geniapp.Infrastructure.Database.ShardDatabase;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace Geniapp.Infrastructure.Database;

public static class DatabaseHostingExtensions
{
    public static void ConfigureSharding(this IServiceCollection services, DatabaseConnectionStrings databaseConnectionStrings, ILogger? logger = null)
    {
        AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);
        services.AddDbContext<MasterDbContext>(opt => opt.UseNpgsql(databaseConnectionStrings.Master));

        List<ShardConfiguration> shards = [];
        foreach (ShardConfiguration shard in databaseConnectionStrings.Shards)
        {
            if (string.IsNullOrWhiteSpace(shard.ConnectionString))
            {
                logger?.LogWarning("Shard {Name} is not configured properly: connection string is null or whitespace. It will be ignored.", shard.Name);
                continue;
            }

            shards.Add(shard);
        }

        ShardConfigurations configuration = new(shards);
        services.AddSingleton(configuration);

        logger?.LogInformation("Found shards: {Shards}.", string.Join(", ", databaseConnectionStrings.Shards.Select(s => s.Name)));

        services.AddScoped<FindShardOfTenant>();
        services.AddScoped<ShardContextProvider>();
        services.AddScoped<TenantsService>();
    }

    public static async Task EnsureDatabasesCreatedAsync(this IServiceProvider services)
    {
        using IServiceScope scope = services.GetRequiredService<IServiceScopeFactory>().CreateScope();

        ILogger logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("ShardingBootstrap");

        logger.LogInformation("Ensure creation and migration of master...");
        await using MasterDbContext masterContext = scope.ServiceProvider.GetRequiredService<MasterDbContext>();

        await using (IDbContextTransaction transaction = await masterContext.Database.BeginTransactionAsync())
        {
            await masterContext.Database.MigrateAsync();
            await masterContext.SaveChangesAsync();
            await transaction.CommitAsync();
        }

        ShardConfigurations shardConfigurations = scope.ServiceProvider.GetRequiredService<ShardConfigurations>();
        IReadOnlyCollection<ShardConfiguration> shards = shardConfigurations.GetAllShardConfigurations();

        ShardContextProvider shardContextProvider = scope.ServiceProvider.GetRequiredService<ShardContextProvider>();
        foreach (ShardConfiguration shard in shards)
        {
            logger.LogInformation("Ensure creation and migration of shard {Name}...", shard.Name);

            ShardDbContext? context = await shardContextProvider.GetShardContextAsync(shard.Name);
            if (context == null)
            {
                throw new InvalidOperationException($"Could not get context of shard {shard.Name}.");
            }

            await using ShardDbContext _ = context;
            await using IDbContextTransaction transaction = await context.Database.BeginTransactionAsync();

            await context.Database.MigrateAsync();

            Shard? existingShard = await masterContext.Shards.AsNoTracking().SingleOrDefaultAsync(s => s.Name == shard.Name);
            if (existingShard == null)
            {
                Shard shardEntity = new(shard.Name);
                masterContext.Add(shardEntity);
                logger.LogInformation("Created shard {Name}.", shard.Name);
            }
            else
            {
                logger.LogInformation("Shard {Name} exists already.", shard.Name);
            }

            await masterContext.SaveChangesAsync();
            await transaction.CommitAsync();
        }
    }

    public static async Task RecreateDatabasesAsync(this IServiceProvider services)
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
