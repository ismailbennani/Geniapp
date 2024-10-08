using Microsoft.EntityFrameworkCore;

namespace Geniapp.Infrastructure.Database.MasterDatabase;

public class MasterDbContext : DbContext
{
    public MasterDbContext(DbContextOptions<MasterDbContext> options) : base(options)
    {
    }

    public DbSet<Shard> Shards { get; private set; }
    public DbSet<TenantShardAssociation> TenantShardAssociations { get; private set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Shard>().HasIndex(s => s.Name).IsUnique();
        modelBuilder.Entity<Shard>().HasMany<TenantShardAssociation>().WithOne(a => a.Shard);
        modelBuilder.Entity<TenantShardAssociation>().HasIndex(a => a.TenantId).IsUnique();
    }
}
