using System.Diagnostics.CodeAnalysis;

namespace NuGetCachingProxy.Core;

[SuppressMessage("ReSharper", "AutoPropertyCanBeMadeGetOnly.Global")]
public sealed class ServiceConfig
{
    /// <summary>
    ///     The MongoDB database name.
    /// </summary>
    public string DatabaseName { get; set; } = "package-cache";

    /// <summary>
    ///     The MongoDB connection string.
    /// </summary>
    public string ConnectionString { get; set; } = "mongodb://localhost:27017/";
    
    /// <summary>
    ///     The default upstream.
    /// </summary>
    public string UpstreamUrl { get; set; } = "https://api.nuget.org/";
}