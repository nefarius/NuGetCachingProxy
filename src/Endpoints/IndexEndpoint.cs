using FastEndpoints;

using Microsoft.AspNetCore.Http.Extensions;

using Nefarius.Web.Caching.Models;

namespace Nefarius.Web.Caching.Endpoints;

public sealed class IndexEndpoint : EndpointWithoutRequest<IndexEndpointResponse>
{
    private readonly IHttpClientFactory _clientFactory;
    private readonly ILogger<IndexEndpoint> _logger;

    public IndexEndpoint(IHttpClientFactory clientFactory, ILogger<IndexEndpoint> logger)
    {
        _clientFactory = clientFactory;
        _logger = logger;
    }

    public override void Configure()
    {
        Get("/v3/index.json");
        AllowAnonymous();
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        HttpClient client = _clientFactory.CreateClient("UpstreamNuGetServer");

        IndexEndpointResponse? response = await client.GetFromJsonAsync<IndexEndpointResponse>("/v3/index.json", ct);

        if (response is null)
        {
            _logger.LogError("Index endpoint not found upstream");
            await SendNotFoundAsync(ct);
            return;
        }

        Resource packageBaseAddress = response.Resources.Single(r => r.Type.Contains("PackageBaseAddress"));

        Uri requestUrl = new(HttpContext.Request.GetDisplayUrl());
        string backendUrl = requestUrl.GetLeftPart(UriPartial.Authority);
        Uri packageUrl = new(new Uri(backendUrl), "/v3-flatcontainer/");

        // replace upstream URL with our own server
        packageBaseAddress.Id = packageUrl.ToString();

        await SendOkAsync(response, ct);
    }
}