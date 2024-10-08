namespace Geniapp.Application.Configuration;

class AgentWorkerConfiguration
{
    /// <summary>
    ///     Is worker mode enabled?
    ///     Workers execute work and write results in the databases.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    ///     The minimal amount of time a worker should idle when performing work.
    /// </summary>
    public double MinWorkDurationInSeconds { get; set; }

    /// <summary>
    ///     The maximal amount of time a worker should idle when performing work.
    /// </summary>
    public double MaxWorkDurationInSeconds { get; set; }
}
