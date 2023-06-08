using System.Text.Json.Serialization;

namespace Nefarius.Web.Caching.Models;

public sealed class Context
{
    [JsonPropertyName("@vocab")]
    public string Vocab { get; set; }

    [JsonPropertyName("comment")]
    public string Comment { get; set; }
}

public sealed class Resource
{
    [JsonPropertyName("@id")]
    public string Id { get; set; }

    [JsonPropertyName("@type")]
    public string Type { get; set; }

    [JsonPropertyName("comment")]
    public string Comment { get; set; }

    [JsonPropertyName("clientVersion")]
    public string ClientVersion { get; set; }
}

public sealed class IndexEndpointResponse
{
    [JsonPropertyName("version")]
    public string Version { get; set; }

    [JsonPropertyName("resources")]
    public List<Resource> Resources { get; set; }

    [JsonPropertyName("@context")]
    public Context Context { get; set; }
}