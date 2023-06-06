using System.Net.Http.Headers;
using System.Reflection;

using FastEndpoints;

using MongoDB.Driver;
using MongoDB.Entities;

using Nefarius.Utilities.AspNetCore;
using Nefarius.Web.Caching.Core;

using Polly;
using Polly.Contrib.WaitAndRetry;

using Serilog;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args).Setup();

IConfigurationSection section = builder.Configuration.GetSection(nameof(ServiceConfig));

ServiceConfig? serviceConfig = section.Get<ServiceConfig>();

if (serviceConfig is null)
{
    Console.WriteLine("Missing service configuration, can't continue!");
    return;
}

builder.Services.Configure<ServiceConfig>(builder.Configuration.GetSection(nameof(ServiceConfig)));

builder.Services.AddFastEndpoints();
builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

builder.Services.AddHttpClient("UpstreamNuGetServer",
        client =>
        {
            client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue(
                Assembly.GetEntryAssembly()?.GetName().Name!,
                Assembly.GetEntryAssembly()?.GetName().Version!.ToString()));

            client.BaseAddress = new Uri(serviceConfig.UpstreamUrl);
        })
    .AddTransientHttpErrorPolicy(pb =>
        pb.WaitAndRetryAsync(Backoff.DecorrelatedJitterBackoffV2(TimeSpan.FromSeconds(3), 10)));

Log.Logger.Information("Initializing database connection");

await DB.InitAsync(serviceConfig.DatabaseName,
    MongoClientSettings.FromConnectionString(serviceConfig.ConnectionString));

WebApplication app = builder.Build().Setup();

app.UseRouting();
app.MapFastEndpoints();
app.MapReverseProxy();

app.Run();