using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Geniapp.Infrastructure.Database.MasterDatabase;

public class TenantShardAssociation
{
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
