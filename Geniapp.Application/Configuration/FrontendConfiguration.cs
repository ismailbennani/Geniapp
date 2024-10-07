﻿namespace Geniapp.Application.Configuration;

class FrontendConfiguration
{
    /// <summary>
    ///     Is frontend mode enabled.
    ///     Frontends read data from the databases and expose them in a website.
    /// </summary>
    public bool Enabled { get; set; } = true;
}
