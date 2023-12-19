using System.Net.Http.Headers;

using FastEndpoints;

using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.Extensions.Options;

using NuGetCachingProxy.Core;

namespace NuGetCachingProxy.Endpoints;

/// <summary>
///     This endpoint creates all other requests to the upstream server in proxy. This is required to replace the upstream
///     URL with this proxy server URL so the client actually fetches cached resources from us instead of directly going to
///     the upstream.
/// </summary>
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
            MediaTypeHeaderValue? contentType = response.Content.Headers.ContentType;
            await SendStreamAsync(await response.Content.ReadAsStreamAsync(ct),
                contentType: contentType?.ToString() ?? "application/octet-stream",
                cancellation: ct);
            return;
        }

        // pass whatever content to client
        if (!Equals(response.Content.Headers.ContentType, MediaTypeHeaderValue.Parse("application/json")))
        {
            MediaTypeHeaderValue? contentType = response.Content.Headers.ContentType;
            await SendStreamAsync(await response.Content.ReadAsStreamAsync(ct),
                contentType: contentType?.ToString() ?? "application/octet-stream",
                cancellation: ct);
            return;
        }

        string jsonBody = await response.Content.ReadAsStringAsync(ct);

        jsonBody = jsonBody.Replace(upstreamUrl, backendUrl);

        await SendStringAsync(jsonBody, contentType: "application/json", cancellation: ct);
    }
}