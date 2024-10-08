using Microsoft.EntityFrameworkCore;

namespace Geniapp.Infrastructure.Database.ShardDatabase;

public class ShardDbContext : DbContext
{
    readonly string _connectionString;

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    // for design time
    public ShardDbContext() { }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

    public ShardDbContext(string connectionString)
    {
        _connectionString = connectionString;
    }

    public DbSet<Tenant> Tenants { get; private set; }
    public DbSet<TenantData> TenantsData { get; private set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder) => optionsBuilder.UseNpgsql(_connectionString);

    protected override void OnModelCreating(ModelBuilder modelBuilder) => modelBuilder.Entity<Tenant>().HasMany<TenantData>().WithOne(d => d.Tenant);
}
