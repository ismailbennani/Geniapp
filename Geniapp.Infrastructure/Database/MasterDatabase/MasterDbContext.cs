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
        modelBuilder.Entity<TenantShardAssociation>().HasOne<Shard>(a => a.Shard).WithMany();
        modelBuilder.Entity<TenantShardAssociation>().HasIndex(a => a.TenantId).IsUnique();
    }
}
