using System.Net;

using FastEndpoints;

using MongoDB.Entities;

using Nefarius.Web.Caching.Models;

namespace Nefarius.Web.Caching.Endpoints;

public sealed class PackageDownloadEndpoint : Endpoint<PackageDownloadRequest>
{
    private readonly IHttpClientFactory _clientFactory;
    private readonly ILogger<PackageDownloadEndpoint> _logger;

    public PackageDownloadEndpoint(IHttpClientFactory clientFactory, ILogger<PackageDownloadEndpoint> logger)
    {
        _clientFactory = clientFactory;
        _logger = logger;
    }

    public override void Configure()
    {
        Get("/v3-flatcontainer/{PackageId}/{PackageVersion}/{PackageFileName}");
        AllowAnonymous();
    }

    public override async Task HandleAsync(PackageDownloadRequest req, CancellationToken ct)
    {
        PackageDownloadRequest? existingPackage = (await DB.Find<PackageDownloadRequest>()
                .ManyAsync(lr =>
                        lr.Eq(r => r.PackageId, req.PackageId) &
                        lr.Eq(r => r.PackageVersion, req.PackageVersion) &
                        lr.Eq(r => r.PackageFileName, req.PackageFileName)
                    , ct)
            ).FirstOrDefault();

        try
        {
            // cached entry found
            if (existingPackage is not null)
            {
                IPAddress? remoteIpAddress = HttpContext.Request.HttpContext.Connection.RemoteIpAddress;

                _logger.LogInformation("[{IpAddress}] Found cached package {Package}", remoteIpAddress,
                    existingPackage);

                // deliver cached copy of symbol blob
                using MemoryStream ms = new();
                await existingPackage.Data.DownloadAsync(ms, cancellation: ct);
                ms.Position = 0;
                await SendStreamAsync(ms, existingPackage.PackageFileName, cancellation: ct);
                return;
            }
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Failed to deliver cached package, fetching from upstream");
        }

        _logger.LogInformation("Fetching package {Package} from upstream", req);

        HttpClient client = _clientFactory.CreateClient("UpstreamNuGetServer");

        HttpResponseMessage response =
            await client.GetAsync($"/v3-flatcontainer/{req.PackageId}/{req.PackageVersion}/{req.PackageFileName}", ct);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogInformation("Requested package {Package} not found upstream", req);

            await SendNotFoundAsync(ct);
            return;
        }

        Stream upstreamContent = await response.Content.ReadAsStreamAsync(ct);

        // cache in memory because we need to return it to database AND requester
        using MemoryStream cache = new();
        await upstreamContent.CopyToAsync(cache, ct);
        cache.Position = 0;

        // save and upload to DB
        await req.SaveAsync(cancellation: ct);
        await req.Data.UploadAsync(cache, cancellation: ct);

        cache.Position = 0;

        await SendStreamAsync(cache, req.PackageFileName, cancellation: ct);
    }
}