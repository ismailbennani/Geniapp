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
    ///     The actual data.
    /// </summary>
    public double Data { get; set; }
}
