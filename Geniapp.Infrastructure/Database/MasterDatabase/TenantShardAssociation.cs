using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Geniapp.Infrastructure.Database.MasterDatabase;

public class TenantShardAssociation
{
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    // for EFC
    TenantShardAssociation() { }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

    public TenantShardAssociation(Guid tenantId, Shard? shard)
    {
        TenantId = tenantId;
        Shard = shard;
    }

    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public Guid Id { get; private set; }

    /// <summary>
    ///     The shard containing the tenant data.
    /// </summary>
    public Shard? Shard { get; private set; }

    /// <summary>
    ///     The tenant
    /// </summary>
    public Guid TenantId { get; private set; }
}
