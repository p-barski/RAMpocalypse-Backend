using RAMpocalypse.Server.Game;

namespace RAMpocalypse.Server.Services;

public interface IPlayerConnectionService
{
    Player? GetPlayerByConnectionId(string connectionId);
    void AddPlayer(string connectionId, Player player);
    void RemovePlayer(string connectionId);
    string? GetConnectionIdByPlayer(Player player);
}
