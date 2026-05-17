using System.Collections.Concurrent;

namespace RAMpocalypse.Server.Services;

public class ChatCooldowns(TimeProvider timeProvider) : IChatCooldowns
{
    private readonly TimeProvider timeProvider = timeProvider;
    private readonly ConcurrentDictionary<string, ChatCooldown> cooldowns = [];
    public bool TryAdd(string key) => cooldowns.TryAdd(key, new ChatCooldown(timeProvider));
    public bool TryRemove(string key) => cooldowns.TryRemove(key, out var _);
    public bool TryGetValue(string key, out ChatCooldown cooldown) => cooldowns.TryGetValue(key, out cooldown!);
}
