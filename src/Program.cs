using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;

using Nefarius.Utilities.AspNetCore;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args).Setup();

builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));
builder.Services.AddResponseCaching(options =>
{
    options.UseCaseSensitivePaths = false;
    options.SizeLimit *= 10; // 1 GB
});

WebApplication app = builder.Build().Setup();

app.Use(async (context, next) =>
{
    IHeaderDictionary requestHeaders = context.Request.Headers;
    StringValues cacheControl = requestHeaders.CacheControl;

    if (!string.IsNullOrEmpty(cacheControl))
    {
        requestHeaders.CacheControl = new StringValues("max-stale");
    }

    await next(context);
});

app.UseResponseCaching();

app.Use(async (context, next) =>
{
    context.Response.GetTypedHeaders().CacheControl = new CacheControlHeaderValue
    {
        Public = true, MaxAge = TimeSpan.FromDays(1)
    };

    context.Response.Headers[HeaderNames.Vary] = new[] { "Accept-Encoding" };

    await next(context);
});

app.MapReverseProxy();

app.Run();