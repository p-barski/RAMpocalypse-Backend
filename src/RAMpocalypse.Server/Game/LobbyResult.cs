namespace RAMpocalypse.Server.Game;

public class LobbyResult(GameLobby lobby, string? winnerId = null)
{
    public string? WinnerId { get; set; } = winnerId;
    public DateTime CreationTime { get; set; } = lobby.CreationTime;
    public DateTime FinishTime { get; set; } = DateTime.UtcNow;
}
