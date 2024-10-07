using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Geniapp.Infrastructure.Database.MasterDatabase;

public class Shard
{
    public Shard(string name)
    {
        Name = name;
    }

    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public Guid Id { get; private set; }

    /// <summary>
    ///     The unique name of the shard.
    /// </summary>
    [MaxLength(512)]
    public string Name { get; private set; }
}
