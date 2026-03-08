using System.Collections.Concurrent;
using RAMpocalypse.Server.Game;
using RAMpocalypse.Server.Services.Results;

namespace RAMpocalypse.Server.Services;

public class MatchmakingService(
    IPlayerConnectionService playerConnectionService,
    ILobbyManager lobbyManager,
    IGameConfig gameConfig) : IMatchmakingService
{
    private readonly ConcurrentQueue<Player> waitingPlayers = [];
    private readonly IPlayerConnectionService playerConnectionService = playerConnectionService;
    private readonly ILobbyManager lobbyManager = lobbyManager;
    private readonly IGameConfig gameConfig = gameConfig;

    public MatchmakingResult RequestMatchmaking(Player player)
    {
        // Check if player is already in a lobby (prevent duplicate requests)
        if (lobbyManager.GetLobbyByPlayer(player) != null)
        {
            return new MatchmakingResult
            {
                Lobby = null,
                PlayersToNotify = []
            };
        }

        // Try to find another waiting player
        Player? otherPlayer = null;
        while (waitingPlayers.TryDequeue(out var waitingPlayer))
        {
            if (playerConnectionService.GetConnectionIdByPlayer(waitingPlayer) != null &&
                lobbyManager.GetLobbyByPlayer(waitingPlayer) == null &&
                waitingPlayer.Id != player.Id) // Ensure we're not matching with ourselves
            {
                otherPlayer = waitingPlayer;
                break;
            }
        }

        if (otherPlayer == null)
        {
            // No match found - add to waiting queue
            waitingPlayers.Enqueue(player);
            return new MatchmakingResult
            {
                Lobby = null,
                PlayersToNotify = []
            };
        }

        // Dimensions should be the same for both players
        int xOffset = player.SpriteData.Width * player.SpriteData.ScaleFactor / 2;
        int yOffset = player.SpriteData.Height * player.SpriteData.ScaleFactor / 2;
        var (corner1, corner2) = Position.GetRandomOppositeCorners(gameConfig.GameWidth, gameConfig.GameHeight, xOffset, yOffset);
        player.Position = corner1;
        otherPlayer.Position = corner2;
        var lobby = lobbyManager.CreateLobby(player, otherPlayer);
        return new MatchmakingResult
        {
            Lobby = lobby,
            PlayersToNotify = [player, otherPlayer]
        };
    }

    public void CancelMatchmaking(Player player)
    {
        // Remove player from queue if present
        // Since ConcurrentQueue doesn't support removal, we'll need to rebuild the queue
        // For now, we'll just let it be removed naturally when dequeued
        // In a production system, you might want to use a different data structure
    }
}
