using RAMpocalypse.Server.Configuration;
using RAMpocalypse.Server.Hubs;
using Xunit;

namespace RAMpocalypse.Tests.RateLimiting;

public class HubMethodRateLimiterFactoryTests
{
    [Theory]
    [InlineData(nameof(GameHub.UpdatePlayerPosition))]
    [InlineData(nameof(GameHub.SendMessage))]
    [InlineData(nameof(GameHub.RequestMatchmaking))]
    [InlineData(nameof(GameHub.PerformMeleeAttack))]
    public void Create_ReturnsDistinctLimitersPerMethod(string methodName)
    {
        var factory = RateLimitingTestHelpers.CreateFactory(new RateLimitingOptions());

        using var limiter = factory.Create(methodName);

        Assert.NotNull(limiter);
    }

    [Fact]
    public void Create_UpdatePlayerPosition_UsesBurstBeforeRejecting()
    {
        var options = new RateLimitingOptions
        {
            HubInvoke = new HubInvokeRateLimitOptions
            {
                PositionBurstMultiplier = 2,
            },
        };
        var factory = RateLimitingTestHelpers.CreateFactory(options, positionUpdateIntervalMs: 1_000);

        using var limiter = factory.Create(nameof(GameHub.UpdatePlayerPosition));

        Assert.True(RateLimitingTestHelpers.CanAcquire(limiter));
        Assert.True(RateLimitingTestHelpers.CanAcquire(limiter));
        Assert.False(RateLimitingTestHelpers.CanAcquire(limiter));
    }

    [Fact]
    public void Create_ControlMethod_UsesConfiguredFixedWindow()
    {
        var options = new RateLimitingOptions
        {
            HubInvoke = new HubInvokeRateLimitOptions
            {
                Control = new FixedWindowRateLimitPolicyOptions
                {
                    PermitLimit = 1,
                    WindowSeconds = 60,
                    QueueLimit = 0,
                },
            },
        };
        var factory = RateLimitingTestHelpers.CreateFactory(options);

        using var limiter = factory.Create(nameof(GameHub.RequestMatchmaking));

        Assert.True(RateLimitingTestHelpers.CanAcquire(limiter));
        Assert.False(RateLimitingTestHelpers.CanAcquire(limiter));
    }

    [Fact]
    public void Create_ChatMethod_UsesConfiguredFixedWindow()
    {
        var options = new RateLimitingOptions
        {
            HubInvoke = new HubInvokeRateLimitOptions
            {
                Chat = new FixedWindowRateLimitPolicyOptions
                {
                    PermitLimit = 1,
                    WindowSeconds = 60,
                    QueueLimit = 0,
                },
            },
        };
        var factory = RateLimitingTestHelpers.CreateFactory(options);

        using var limiter = factory.Create(nameof(GameHub.SendMessage));

        Assert.True(RateLimitingTestHelpers.CanAcquire(limiter));
        Assert.False(RateLimitingTestHelpers.CanAcquire(limiter));
    }

    [Fact]
    public void Create_GameplayMethod_UsesConfiguredFixedWindow()
    {
        var options = new RateLimitingOptions
        {
            HubInvoke = new HubInvokeRateLimitOptions
            {
                Gameplay = new FixedWindowRateLimitPolicyOptions
                {
                    PermitLimit = 1,
                    WindowSeconds = 60,
                    QueueLimit = 0,
                },
            },
        };
        var factory = RateLimitingTestHelpers.CreateFactory(options);

        using var limiter = factory.Create(nameof(GameHub.PerformMeleeAttack));

        Assert.True(RateLimitingTestHelpers.CanAcquire(limiter));
        Assert.False(RateLimitingTestHelpers.CanAcquire(limiter));
    }
}
