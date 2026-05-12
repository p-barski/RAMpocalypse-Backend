using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using RAMpocalypse.Server.Configuration;
using RAMpocalypse.Server.Hubs;
using Xunit;

namespace RAMpocalypse.Tests.RateLimiting;

public class RateLimitingHubFilterTests
{
    private static HubInvocationRateLimiterRegistry CreateRegistry(
        int controlPermitLimit = 1,
        int positionBurstMultiplier = 3,
        int positionUpdateIntervalMs = 16)
    {
        var options = new RateLimitingOptions
        {
            HubInvoke = new HubInvokeRateLimitOptions
            {
                Control = new FixedWindowRateLimitPolicyOptions
                {
                    PermitLimit = controlPermitLimit,
                    WindowSeconds = 60,
                    QueueLimit = 0,
                },
                PositionBurstMultiplier = positionBurstMultiplier,
            },
        };

        return new HubInvocationRateLimiterRegistry(
            RateLimitingTestHelpers.CreateFactory(options, positionUpdateIntervalMs));
    }

    [Fact]
    public async Task InvokeMethodAsync_AllowsInvocationWhenLeaseIsAvailable()
    {
        using var registry = CreateRegistry(controlPermitLimit: 2);
        var filter = new RateLimitingHubFilter(registry, NullLogger<RateLimitingHubFilter>.Instance);
        var invocationContext = RateLimitingTestHelpers.CreateInvocationContext(
            "connection-a",
            nameof(GameHub.RequestMatchmaking));
        var nextCalled = false;

        var result = await filter.InvokeMethodAsync(
            invocationContext,
            _ =>
            {
                nextCalled = true;
                return ValueTask.FromResult<object?>(true);
            });

        Assert.True(nextCalled);
        Assert.Equal(true, result);
    }

    [Fact]
    public async Task InvokeMethodAsync_RequestMatchmaking_ThrowsHubExceptionWhenRejected()
    {
        using var registry = CreateRegistry();
        var filter = new RateLimitingHubFilter(registry, NullLogger<RateLimitingHubFilter>.Instance);
        var invocationContext = RateLimitingTestHelpers.CreateInvocationContext(
            "connection-a",
            nameof(GameHub.RequestMatchmaking));

        await filter.InvokeMethodAsync(invocationContext, _ => ValueTask.FromResult<object?>(true));

        await Assert.ThrowsAsync<HubException>(() =>
            filter.InvokeMethodAsync(invocationContext, _ => ValueTask.FromResult<object?>(true)).AsTask());
    }

    [Fact]
    public async Task InvokeMethodAsync_GetPlayerId_ReturnsEmptyStringWhenRejected()
    {
        using var registry = CreateRegistry();
        var filter = new RateLimitingHubFilter(registry, NullLogger<RateLimitingHubFilter>.Instance);
        var invocationContext = RateLimitingTestHelpers.CreateInvocationContext(
            "connection-a",
            nameof(GameHub.GetPlayerId));

        await filter.InvokeMethodAsync(invocationContext, _ => ValueTask.FromResult<object?>("player-id"));

        var result = await filter.InvokeMethodAsync(invocationContext, _ => ValueTask.FromResult<object?>("player-id"));

        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public async Task InvokeMethodAsync_UpdatePlayerPosition_ReturnsNullWhenRejected()
    {
        using var registry = CreateRegistry(controlPermitLimit: 10, positionBurstMultiplier: 1, positionUpdateIntervalMs: 60_000);
        var filter = new RateLimitingHubFilter(registry, NullLogger<RateLimitingHubFilter>.Instance);
        var invocationContext = RateLimitingTestHelpers.CreateInvocationContext(
            "connection-a",
            nameof(GameHub.UpdatePlayerPosition));

        await filter.InvokeMethodAsync(invocationContext, _ => ValueTask.FromResult<object?>(null));

        var result = await filter.InvokeMethodAsync(invocationContext, _ => ValueTask.FromResult<object?>(null));

        Assert.Null(result);
    }

    [Fact]
    public async Task OnDisconnectedAsync_RemovesConnectionLimiterFromRegistry()
    {
        using var registry = CreateRegistry();
        var filter = new RateLimitingHubFilter(registry, NullLogger<RateLimitingHubFilter>.Instance);
        var invocationContext = RateLimitingTestHelpers.CreateInvocationContext(
            "connection-a",
            nameof(GameHub.RequestMatchmaking));
        var exhaustedLimiter = registry.GetLimiter("connection-a", nameof(GameHub.RequestMatchmaking));

        await filter.InvokeMethodAsync(invocationContext, _ => ValueTask.FromResult<object?>(true));
        Assert.False(RateLimitingTestHelpers.CanAcquire(exhaustedLimiter));

        var hubContext = Substitute.For<HubCallerContext>();
        hubContext.ConnectionId.Returns("connection-a");
        var lifetimeContext = new HubLifetimeContext(
            hubContext,
            new ServiceCollection().BuildServiceProvider(),
            Substitute.For<Hub>());

        await filter.OnDisconnectedAsync(lifetimeContext, exception: null, (_, _) => Task.CompletedTask);

        var limiter = registry.GetLimiter("connection-a", nameof(GameHub.RequestMatchmaking));
        Assert.NotSame(exhaustedLimiter, limiter);
        Assert.True(RateLimitingTestHelpers.CanAcquire(limiter));
    }
}
