using Geniapp.Infrastructure;

namespace Geniapp.Worker;

public class WorkerConfiguration : SharedConfiguration
{
    /// <summary>
    ///     The minimal amount of time a worker should idle when performing work.
    /// </summary>
    public double MinWorkDurationInSeconds { get; set; }

    /// <summary>
    ///     The maximal amount of time a worker should idle when performing work.
    /// </summary>
    public double MaxWorkDurationInSeconds { get; set; }
}
