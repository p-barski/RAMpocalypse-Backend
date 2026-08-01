using System.Threading.RateLimiting;
using Microsoft.Extensions.Options;
using RAMpocalypse.Server.Configuration;
using RAMpocalypse.Server.Services;

namespace RAMpocalypse.Server.Hubs;

public sealed class HubMethodRateLimiterFactory(
    IOptionsMonitor<RateLimitingOptions> rateLimitingOptions,
    IGameConfig gameConfig)
{
    private readonly IOptionsMonitor<RateLimitingOptions> rateLimitingOptions = rateLimitingOptions;
    private readonly IGameConfig gameConfig = gameConfig;

    public RateLimiter Create(string methodName)
    {
        var options = rateLimitingOptions.CurrentValue.HubInvoke;
        return methodName switch
        {
            nameof(GameHub.UpdatePlayerPosition) => CreatePositionLimiter(options.PositionBurstMultiplier),
            nameof(GameHub.SendMessage) => CreateFixedWindowLimiter(options.Chat),
            nameof(GameHub.GetPlayerId) or nameof(GameHub.RequestMatchmaking) or nameof(GameHub.LeaveGame)
                or nameof(GameHub.SetPlayerName) =>
                CreateFixedWindowLimiter(options.Control),
            _ => CreateFixedWindowLimiter(options.Gameplay),
        };
    }

    private RateLimiter CreatePositionLimiter(int burstMultiplier)
    {
        var intervalMs = Math.Max(1, gameConfig.PositionUpdateIntervalMs);
        return new TokenBucketRateLimiter(new TokenBucketRateLimiterOptions
        {
            TokenLimit = burstMultiplier,
            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
            QueueLimit = 0,
            ReplenishmentPeriod = TimeSpan.FromMilliseconds(intervalMs),
            TokensPerPeriod = 1,
            AutoReplenishment = true,
        });
    }

    private static RateLimiter CreateFixedWindowLimiter(FixedWindowRateLimitPolicyOptions options) =>
        new FixedWindowRateLimiter(new FixedWindowRateLimiterOptions
        {
            PermitLimit = options.PermitLimit,
            Window = TimeSpan.FromSeconds(options.WindowSeconds),
            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
            QueueLimit = options.QueueLimit,
        });
}
