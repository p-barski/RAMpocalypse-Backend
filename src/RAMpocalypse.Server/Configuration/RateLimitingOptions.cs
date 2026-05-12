namespace RAMpocalypse.Server.Configuration;

public sealed class RateLimitingOptions
{
    public const string SectionName = "RateLimiting";
    public FixedWindowRateLimitPolicyOptions Api { get; set; } = new()
    {
        PermitLimit = 10,
        WindowSeconds = 30,
        QueueLimit = 2
    };
    public FixedWindowRateLimitPolicyOptions HubConnect { get; set; } = new();
    public FixedWindowRateLimitPolicyOptions StaticAssets { get; set; } = new();
    public HubInvokeRateLimitOptions HubInvoke { get; set; } = new();
}

public sealed class HubInvokeRateLimitOptions
{
    public FixedWindowRateLimitPolicyOptions Control { get; set; } = new()
    {
        PermitLimit = 10,
        WindowSeconds = 10,
        QueueLimit = 0
    };
    public FixedWindowRateLimitPolicyOptions Chat { get; set; } = new()
    {
        PermitLimit = 10,
        WindowSeconds = 10,
        QueueLimit = 0
    };
    public FixedWindowRateLimitPolicyOptions Gameplay { get; set; } = new()
    {
        PermitLimit = 20,
        WindowSeconds = 10,
        QueueLimit = 0
    };
    public int PositionBurstMultiplier { get; set; } = 3;
}

public sealed class FixedWindowRateLimitPolicyOptions
{
    public int PermitLimit { get; set; } = 25;
    public int WindowSeconds { get; set; } = 60;
    public int QueueLimit { get; set; } = 0;
}
