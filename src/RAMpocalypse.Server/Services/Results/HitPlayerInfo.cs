using RAMpocalypse.Server.Game;

namespace RAMpocalypse.Server.Services.Results;

public class HitPlayerInfo
{
    public required Player Player { get; set; }
    public int Damage { get; set; }
    public int NewHealth { get; set; }
    public bool Died { get; set; }
}
