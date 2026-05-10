namespace RAMpocalypse.Server.Services;

public class LongLivedAttacksCleaner(ILobbyManager lobbyManager, TimeProvider timeProvider) : ILongLivedAttacksCleaner
{
    private readonly ILobbyManager lobbyManager = lobbyManager;
    private readonly TimeProvider timeProvider = timeProvider;
    public void PruneExpiredAttacks()
    {
        var nowMs = timeProvider.GetUtcNow().ToUnixTimeMilliseconds();

        foreach (var lobby in lobbyManager.GetActiveLobbies())
        {
            List<string> expiredIds = [];
            foreach (var kvp in lobby.LongLivedAttacks)
            {
                if (nowMs - kvp.Value.CreationTime >= kvp.Value.Lifetime)
                    expiredIds.Add(kvp.Key);
            }

            foreach (var id in expiredIds)
                lobby.LongLivedAttacks.TryRemove(id, out _);
        }
    }
}
