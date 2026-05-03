using RAMpocalypse.Server.Game;

namespace RAMpocalypse.Server.Services;

public interface IPlayerFactory
{
    void RandomizePlayersSprites(List<Player> players);
    Player CreatePlayer();
}
