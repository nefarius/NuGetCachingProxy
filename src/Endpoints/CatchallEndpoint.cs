using System.Net.Http.Headers;

using FastEndpoints;

using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.Extensions.Options;

using NuGetCachingProxy.Core;

namespace NuGetCachingProxy.Endpoints;

internal sealed class CatchallEndpoint(IOptions<ServiceConfig> options, IHttpClientFactory clientFactory)
    : EndpointWithoutRequest
{
    public override void Configure()
    {
        Get("{**catch-all}");
        AllowAnonymous();
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        string upstreamUrl = new Uri(options.Value.UpstreamUrl).GetLeftPart(UriPartial.Authority);

        Uri requestUrl = new(HttpContext.Request.GetDisplayUrl());
        string backendUrl = requestUrl.GetLeftPart(UriPartial.Authority);
        string requestPath = requestUrl.AbsolutePath;

        HttpClient client = clientFactory.CreateClient("UpstreamNuGetServer");

        HttpResponseMessage response = await client.GetAsync(requestPath, ct);

        // pass error to client
        if (!response.IsSuccessStatusCode)
        {
            await SendErrorsAsync((int)response.StatusCode, ct);
            return;
        }

        // pass whatever content to client
        if (!Equals(response.Content.Headers.ContentType, MediaTypeHeaderValue.Parse("application/json")))
        {
            await SendStreamAsync(await response.Content.ReadAsStreamAsync(ct), cancellation: ct);
        }

        string jsonBody = await response.Content.ReadAsStringAsync(ct);

        jsonBody = jsonBody.Replace(upstreamUrl, backendUrl);

        await SendStringAsync(jsonBody, contentType: "application/json", cancellation: ct);
    }
}