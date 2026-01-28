using RAMpocalypse.Server.Game;
using RAMpocalypse.Server.Services.Results;

namespace RAMpocalypse.Server.Services;

public interface IMatchmakingService
{
    MatchmakingResult RequestMatchmaking(Player player);
    void CancelMatchmaking(Player player);
}
