namespace Geniapp.Application.Configuration;

class WorkerConfiguration
{
    /// <summary>
    ///     Is worker mode enabled?
    ///     Workers execute work and write results in the databases.
    /// </summary>
    public bool Enabled { get; set; } = true;
}
