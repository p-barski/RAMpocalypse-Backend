using Microsoft.AspNetCore.SignalR;

namespace RAMpocalypse.Server.Hubs;

public sealed class RateLimitingHubFilter(
    HubInvocationRateLimiterRegistry registry,
    ILogger<RateLimitingHubFilter> logger) : IHubFilter
{
    private readonly HubInvocationRateLimiterRegistry registry = registry;
    private readonly ILogger<RateLimitingHubFilter> logger = logger;

    public ValueTask<object?> InvokeMethodAsync(
        HubInvocationContext invocationContext,
        Func<HubInvocationContext, ValueTask<object?>> next)
    {
        var connectionId = invocationContext.Context.ConnectionId;
        var methodName = invocationContext.HubMethodName;
        var limiter = registry.GetLimiter(connectionId, methodName);
        using var lease = limiter.AttemptAcquire(1);

        if (lease.IsAcquired) return next(invocationContext);

        logger.LogWarning("Hub method rate limit rejected for connection {ConnectionId} method {MethodName}",
            connectionId, methodName);
        return ValueTask.FromResult(HandleRateLimitRejection(methodName));
    }

    public Task OnDisconnectedAsync(
        HubLifetimeContext context,
        Exception? exception,
        Func<HubLifetimeContext, Exception?, Task> next)
    {
        registry.RemoveConnection(context.Context.ConnectionId);
        return next(context, exception);
    }

    private static object? HandleRateLimitRejection(string methodName) =>
        methodName switch
        {
            nameof(GameHub.RequestMatchmaking) => throw new HubException("rate_limited"),
            nameof(GameHub.GetPlayerId) => string.Empty,
            nameof(GameHub.Dash) => false,
            _ => null,
        };
}
