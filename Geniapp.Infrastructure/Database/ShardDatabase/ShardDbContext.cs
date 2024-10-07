using Microsoft.EntityFrameworkCore;

namespace Geniapp.Infrastructure.Database.ShardDatabase;

public class ShardDbContext : DbContext
{
    readonly string _connectionString;

    public ShardDbContext(string connectionString, DbContextOptions options) : base(options)
    {
        _connectionString = connectionString;
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder) => optionsBuilder.UseNpgsql(_connectionString);

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TenantData>().HasOne<Tenant>(d => d.Tenant).WithOne();
        modelBuilder.Entity<TenantData>().HasIndex(a => a.Tenant).IsUnique();
    }
}
