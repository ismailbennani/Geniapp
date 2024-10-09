namespace Geniapp.Master;

public class InitialTenantsConfiguration
{
    /// <summary>
    ///     Should all the tenants be deleted and recreated?
    /// </summary>
    public bool Recreate { get; set; }

    /// <summary>
    ///     The number of tenants to create initially.
    /// </summary>
    public int Count { get; set; }
}
