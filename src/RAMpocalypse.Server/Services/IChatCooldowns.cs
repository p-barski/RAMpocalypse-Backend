namespace RAMpocalypse.Server.Services;

public interface IChatCooldowns
{
    bool TryAdd(string key);
    bool TryRemove(string key);
    bool TryGetValue(string key, out ChatCooldown cooldown);
}
