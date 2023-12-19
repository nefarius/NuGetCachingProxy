using System.Net;

using FastEndpoints;

using MongoDB.Entities;

using NuGetCachingProxy.Models;

namespace NuGetCachingProxy.Endpoints;

/// <summary>
///     Checks the database for a given package to exist and returns it, otherwise fetches package from upstream and caches
///     it in the database.
/// </summary>
internal sealed class PackageDownloadEndpoint(IHttpClientFactory clientFactory, ILogger<PackageDownloadEndpoint> logger)
    : Endpoint<PackageDownloadRequest>
{
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

                logger.LogInformation("[{IpAddress}] Found cached package {Package}", remoteIpAddress,
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
            logger.LogWarning(ex, "Failed to deliver cached package, fetching from upstream");
        }

        logger.LogInformation("Fetching package {Package} from upstream", req);

        HttpClient client = clientFactory.CreateClient("UpstreamNuGetServer");

        HttpResponseMessage response =
            await client.GetAsync($"/v3-flatcontainer/{req.PackageId}/{req.PackageVersion}/{req.PackageFileName}", ct);

        if (!response.IsSuccessStatusCode)
        {
            logger.LogInformation("Requested package {Package} not found upstream", req);

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