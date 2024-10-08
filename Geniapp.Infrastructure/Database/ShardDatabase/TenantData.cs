using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Geniapp.Infrastructure.Database.ShardDatabase;

public class TenantData
{
    public TenantData(Tenant tenant)
    {
        Tenant = tenant;
    }

    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public Guid Id { get; private set; }

    /// <summary>
    ///     The tenant that owns the data.
    /// </summary>
    public Tenant Tenant { get; private set; }

    /// <summary>
    ///     The ID of the last worker that called <see cref="PerformWork" />.
    /// </summary>
    public Guid? LastWorkerId { get; private set; }

    /// <summary>
    ///     The last modification date.
    /// </summary>
    public DateTime LastModificationDate { get; private set; }

    public void PerformWork(Guid workerId)
    {
        LastWorkerId = workerId;
        LastModificationDate = DateTime.Now;
    }
}
