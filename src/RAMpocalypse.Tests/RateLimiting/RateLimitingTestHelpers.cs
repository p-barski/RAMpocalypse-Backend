using System.Threading.RateLimiting;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NSubstitute;
using RAMpocalypse.Server.Configuration;
using RAMpocalypse.Server.Hubs;
using RAMpocalypse.Server.Services;

namespace RAMpocalypse.Tests.RateLimiting;

internal static class RateLimitingTestHelpers
{
    internal static HubMethodRateLimiterFactory CreateFactory(
        RateLimitingOptions options,
        int positionUpdateIntervalMs = 16)
    {
        var gameConfig = Substitute.For<IGameConfig>();
        gameConfig.PositionUpdateIntervalMs.Returns(positionUpdateIntervalMs);
        var monitor = Substitute.For<IOptionsMonitor<RateLimitingOptions>>();
        monitor.CurrentValue.Returns(options);
        return new HubMethodRateLimiterFactory(monitor, gameConfig);
    }

    internal static HubInvocationContext CreateInvocationContext(string connectionId, string methodName)
    {
        var hubContext = Substitute.For<HubCallerContext>();
        hubContext.ConnectionId.Returns(connectionId);

        var method = typeof(GameHub).GetMethod(methodName)
            ?? throw new InvalidOperationException($"Hub method {methodName} was not found.");

        return new HubInvocationContext(
            hubContext,
            new ServiceCollection().BuildServiceProvider(),
            Substitute.For<Hub>(),
            method,
            []);
    }

    internal static bool CanAcquire(RateLimiter limiter) => limiter.AttemptAcquire(1).IsAcquired;
}
