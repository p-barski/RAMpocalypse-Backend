namespace RAMpocalypse.Server.Services;

public class ChatCooldown
{
    private const int MAX_MSGS_PER_MINUTE = 30;
    private readonly TimeSpan MSG_COOLDOWN_MS = TimeSpan.FromMilliseconds(1000);
    private readonly TimeSpan ONE_MINUTE_MS = TimeSpan.FromMilliseconds(60000);
    private readonly DateTime[] previousTimestamps = new DateTime[MAX_MSGS_PER_MINUTE];
    private int currentTimestampIndex = 0;
    private DateTime lastMsgSentTimestamp = DateTime.MinValue;

    public double GetRemainingCooldown()
    {
        var now = DateTime.UtcNow;
        var secondCooldownRemaining = MSG_COOLDOWN_MS - (now - lastMsgSentTimestamp);
        var minuteCooldownRemaining = ONE_MINUTE_MS - (now - previousTimestamps[currentTimestampIndex]);
        return Math.Max(0.0, Math.Max(secondCooldownRemaining.TotalMilliseconds, minuteCooldownRemaining.TotalMilliseconds));
    }

    public void UpdateCooldowns()
    {
        var now = DateTime.UtcNow;
        previousTimestamps[currentTimestampIndex] = now;
        currentTimestampIndex = (currentTimestampIndex + 1) % MAX_MSGS_PER_MINUTE;
        lastMsgSentTimestamp = now;
    }
}
