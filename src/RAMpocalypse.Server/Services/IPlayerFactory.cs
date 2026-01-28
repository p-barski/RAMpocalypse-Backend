using RAMpocalypse.Server.Game;

namespace RAMpocalypse.Server.Services;

public interface IPlayerFactory
{
    Player CreatePlayer(string connectionId);
}
