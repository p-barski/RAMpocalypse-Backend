using RAMpocalypse.Server.Configuration;
using RAMpocalypse.Server.Hubs;
using Xunit;

namespace RAMpocalypse.Tests.RateLimiting;

public class HubInvocationRateLimiterRegistryTests
{
    private static HubInvocationRateLimiterRegistry CreateRegistry(
        int controlPermitLimit = 1,
        int chatPermitLimit = 10)
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
                Chat = new FixedWindowRateLimitPolicyOptions
                {
                    PermitLimit = chatPermitLimit,
                    WindowSeconds = 60,
                    QueueLimit = 0,
                },
            },
        };
        return new HubInvocationRateLimiterRegistry(RateLimitingTestHelpers.CreateFactory(options));
    }

    [Fact]
    public void GetLimiter_ReusesLimiterForSameConnectionAndMethod()
    {
        using var registry = CreateRegistry();

        var first = registry.GetLimiter("connection-a", nameof(GameHub.RequestMatchmaking));
        var second = registry.GetLimiter("connection-a", nameof(GameHub.RequestMatchmaking));

        Assert.Same(first, second);
        Assert.True(RateLimitingTestHelpers.CanAcquire(first));
        Assert.False(RateLimitingTestHelpers.CanAcquire(second));
    }

    [Fact]
    public void GetLimiter_UsesDifferentLimiterPerConnectionId()
    {
        using var registry = CreateRegistry();

        var connectionA = registry.GetLimiter("connection-a", nameof(GameHub.RequestMatchmaking));
        var connectionB = registry.GetLimiter("connection-b", nameof(GameHub.RequestMatchmaking));

        Assert.NotSame(connectionA, connectionB);
        Assert.True(RateLimitingTestHelpers.CanAcquire(connectionA));
        Assert.True(RateLimitingTestHelpers.CanAcquire(connectionB));
    }

    [Fact]
    public void GetLimiter_UsesDifferentLimiterPerMethodName()
    {
        using var registry = CreateRegistry(chatPermitLimit: 1);

        var matchmaking = registry.GetLimiter("connection-a", nameof(GameHub.PerformMeleeAttack));
        var chat = registry.GetLimiter("connection-a", nameof(GameHub.PerformSpecialAttack));

        Assert.NotSame(matchmaking, chat);
        Assert.True(RateLimitingTestHelpers.CanAcquire(matchmaking));
        Assert.True(RateLimitingTestHelpers.CanAcquire(chat));
    }

    [Fact]
    public void RemoveConnection_RemovesLimitersForConnection()
    {
        using var registry = CreateRegistry();

        var first = registry.GetLimiter("connection-a", nameof(GameHub.RequestMatchmaking));
        Assert.True(RateLimitingTestHelpers.CanAcquire(first));
        Assert.False(RateLimitingTestHelpers.CanAcquire(first));

        registry.RemoveConnection("connection-a");

        var second = registry.GetLimiter("connection-a", nameof(GameHub.RequestMatchmaking));
        Assert.NotSame(first, second);
        Assert.True(RateLimitingTestHelpers.CanAcquire(second));
    }
}
