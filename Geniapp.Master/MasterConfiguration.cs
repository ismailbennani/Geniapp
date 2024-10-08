using Geniapp.Infrastructure;

namespace Geniapp.Master;

public class MasterConfiguration : SharedConfiguration
{
    /// <summary>
    ///     The port to use for the admin API server.
    /// </summary>
    public int Port { get; set; }

    /// <summary>
    ///     The work to publish.
    /// </summary>
    public PublishWorkConfiguration Work { get; set; } = new();
}
