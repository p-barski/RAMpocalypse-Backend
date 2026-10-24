using RAMpocalypse.Server.Game;
using RAMpocalypse.Server.Services.Results;

namespace RAMpocalypse.Server.Services;

public class MatchmakingService(
    IPlayerConnectionService playerConnectionService,
    ILobbyManager lobbyManager,
    IGameConfig gameConfig) : IMatchmakingService
{
    private readonly MatchmakingQueue playerQueue = new();
    private readonly IPlayerConnectionService playerConnectionService = playerConnectionService;
    private readonly ILobbyManager lobbyManager = lobbyManager;
    private readonly IGameConfig gameConfig = gameConfig;

    public MatchmakingResult RequestMatchmaking(Player player)
    {
        if (lobbyManager.GetLobbyByPlayer(player) != null)
        {
            return new MatchmakingResult
            {
                Lobby = null,
                PlayersToNotify = []
            };
        }

        Player? otherPlayer = null;
        while (playerQueue.TryDequeue(out var waitingPlayer))
        {
            if (playerConnectionService.GetConnectionIdByPlayer(waitingPlayer) != null &&
                lobbyManager.GetLobbyByPlayer(waitingPlayer) == null &&
                waitingPlayer.Id != player.Id)
            {
                otherPlayer = waitingPlayer;
                break;
            }
        }

        // Dimensions should be the same for both players, at least for now
        var halfWidth = player.SpriteData.Width * player.SpriteData.ScaleFactor / 2.0;
        var halfHeight = player.SpriteData.Height * player.SpriteData.ScaleFactor / 2.0;
        var (xOffset, yOffset) = Position.GetDiagonalSpawnOffset(halfWidth, halfHeight);
        if (otherPlayer != null)
        {
            var (corner1, corner2) = Position.GetRandomOppositeCorners(gameConfig.GameWidth, gameConfig.GameHeight, xOffset, yOffset);
            player.Position = corner1;
            otherPlayer.Position = corner2;
            player.SetWeaponArmTowardsCenter(gameConfig.GameWidth);
            otherPlayer.SetWeaponArmTowardsCenter(gameConfig.GameWidth);
            var lobby = lobbyManager.CreateLobby(player, otherPlayer);
            return new MatchmakingResult
            {
                Lobby = lobby,
                PlayersToNotify = [player, otherPlayer]
            };
        }
        var joinedLobby = lobbyManager.TryAddPlayerToLobby(
            player,
            gameConfig.GameWidth,
            gameConfig.GameHeight,
            xOffset,
            yOffset);
        if (joinedLobby != null)
        {
            var existingPlayers = joinedLobby.Players.Where(p => p.Id != player.Id).ToList();
            return new MatchmakingResult
            {
                Lobby = joinedLobby,
                PlayersToNotify = [player],
                ExistingPlayersToNotify = existingPlayers
            };
        }

        playerQueue.Enqueue(player);
        return new MatchmakingResult
        {
            Lobby = null,
            PlayersToNotify = []
        };
    }

    public void CancelMatchmaking(Player player)
    {
        playerQueue.Remove(player);
    }
}
