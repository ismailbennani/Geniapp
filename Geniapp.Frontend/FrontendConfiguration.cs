using Geniapp.Infrastructure;

namespace Geniapp.Frontend;

public class FrontendConfiguration : SharedConfiguration
{
    /// <summary>
    ///     The port to use for the admin API server.
    /// </summary>
    public int Port { get; set; }
}
