using System.Globalization;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;
using RAMpocalypse.Server.Configuration;

namespace RAMpocalypse.Server.Extensions;

public static class RateLimitingExtensions
{
    public static IServiceCollection AddCustomRateLimiting(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<RateLimitingOptions>(configuration.GetSection(RateLimitingOptions.SectionName));
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.OnRejected = OnRejectedAsync;
            options.AddPolicy("api", CreateApiPartition);
            options.AddPolicy("hubConnect", CreateHubConnectPartition);
            options.AddPolicy("staticAssets", CreateStaticAssetsPartition);
        });
        return services;
    }

    private static RateLimitPartition<string> CreateApiPartition(HttpContext httpContext)
    {
        var settings = httpContext.RequestServices.GetRequiredService<IOptionsMonitor<RateLimitingOptions>>().CurrentValue;
        return RateLimitPartition.GetFixedWindowLimiter(
            "api",
            _ => CreateFixedWindowOptions(settings.Api.PermitLimit, settings.Api.WindowSeconds, settings.Api.QueueLimit));
    }

    private static RateLimitPartition<string> CreateHubConnectPartition(HttpContext httpContext)
    {
        var settings = httpContext.RequestServices.GetRequiredService<IOptionsMonitor<RateLimitingOptions>>().CurrentValue;
        var clientIp = GetClientIpAddress(httpContext);
        return RateLimitPartition.GetFixedWindowLimiter(
            clientIp,
            _ => CreateFixedWindowOptions(
                settings.HubConnect.PermitLimit,
                settings.HubConnect.WindowSeconds,
                settings.HubConnect.QueueLimit));
    }

    private static RateLimitPartition<string> CreateStaticAssetsPartition(HttpContext httpContext)
    {
        var settings = httpContext.RequestServices.GetRequiredService<IOptionsMonitor<RateLimitingOptions>>().CurrentValue;
        var clientIp = GetClientIpAddress(httpContext);
        return RateLimitPartition.GetFixedWindowLimiter(
            clientIp,
            _ => CreateFixedWindowOptions(
                settings.StaticAssets.PermitLimit,
                settings.StaticAssets.WindowSeconds,
                settings.StaticAssets.QueueLimit));
    }

    private static FixedWindowRateLimiterOptions CreateFixedWindowOptions(int permitLimit, int windowSeconds, int queueLimit) =>
        new()
        {
            PermitLimit = permitLimit,
            Window = TimeSpan.FromSeconds(windowSeconds),
            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
            QueueLimit = queueLimit
        };

    private static string GetClientIpAddress(HttpContext httpContext) =>
        httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

    private static ValueTask OnRejectedAsync(OnRejectedContext context, CancellationToken cancellationToken)
    {
        var logger = context.HttpContext.RequestServices
            .GetRequiredService<ILoggerFactory>()
            .CreateLogger("RAMpocalypse.Server.RateLimiting");

        var policyName = context.HttpContext.GetEndpoint()?
            .Metadata.GetMetadata<EnableRateLimitingAttribute>()?.PolicyName
            ?? "unknown";

        logger.LogWarning(
            "Rate limit rejected for policy {PolicyName} from {ClientIp} on {Path}",
            policyName,
            context.HttpContext.Connection.RemoteIpAddress,
            context.HttpContext.Request.Path);

        if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
        {
            context.HttpContext.Response.Headers.RetryAfter =
                ((int)retryAfter.TotalSeconds).ToString(CultureInfo.InvariantCulture);
        }

        return ValueTask.CompletedTask;
    }
}
