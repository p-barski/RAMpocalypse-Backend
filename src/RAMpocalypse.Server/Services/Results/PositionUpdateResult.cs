using RAMpocalypse.Server.Game;

namespace RAMpocalypse.Server.Services.Results;

public class PositionUpdateResult
{
    public bool NeedsCorrection { get; set; }
    public Position CorrectedPosition { get; set; }
    public List<Player> PlayersToNotify { get; set; } = [];
}
