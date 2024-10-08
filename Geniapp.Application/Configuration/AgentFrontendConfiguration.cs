namespace Geniapp.Application.Configuration;

class AgentFrontendConfiguration
{
    /// <summary>
    ///     Is frontend mode enabled.
    ///     Frontends read data from the databases and expose them in a website.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    ///     The port to use for the admin API server.
    /// </summary>
    public int Port { get; set; }
}
