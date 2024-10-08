using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Geniapp.Infrastructure.Database.MasterDatabase;

public class Shard
{
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    // for EFC
    Shard() { }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

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
