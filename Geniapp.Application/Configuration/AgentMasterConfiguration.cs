using Geniapp.Master;

namespace Geniapp.Application.Configuration;

class AgentMasterConfiguration
{
    /// <summary>
    ///     Is master mode enabled?
    ///     The master is a singleton responsible for scheduling work for the workers.
    ///     It also exposes admin APIs.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    ///     The port to use for the admin API server.
    /// </summary>
    public int Port { get; set; }

    /// <summary>
    ///     The work to publish.
    /// </summary>
    public PublishWorkConfiguration Work { get; set; } = new();
}
