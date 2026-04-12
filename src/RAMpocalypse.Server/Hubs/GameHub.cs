using Microsoft.AspNetCore.SignalR;
using RAMpocalypse.Server.Database;
using RAMpocalypse.Server.Game;
using RAMpocalypse.Server.Services;
using RAMpocalypse.Server.Services.Results;

namespace RAMpocalypse.Server.Hubs;

public class GameHub(
    ILogger<GameHub> logger,
    IPlayerConnectionService playerConnectionService,
    IMatchmakingService matchmakingService,
    ILobbyManager lobbyManager,
    IGameService gameService,
    IPlayerFactory playerFactory,
    IGameConfig gameConfig,
    IDatabase database) : Hub<ISendMethods>
{
    private readonly ILogger<GameHub> logger = logger;
    private readonly IPlayerConnectionService playerConnectionService = playerConnectionService;
    private readonly IMatchmakingService matchmakingService = matchmakingService;
    private readonly ILobbyManager lobbyManager = lobbyManager;
    private readonly IGameService gameService = gameService;
    private readonly IPlayerFactory playerFactory = playerFactory;
    private readonly IGameConfig gameConfig = gameConfig;
    private readonly IDatabase database = database;

    public Task<string> GetPlayerId()
    {
        var player = playerConnectionService.GetPlayerByConnectionId(Context.ConnectionId);
        if (player is not null)
        {
            logger.LogWarning(
                "GetPlayerId: player already assigned to connection. PlayerId: {PlayerId}, ConnectionId: {ConnectionId}",
                player.Id, Context.ConnectionId);
            return Task.FromResult(player.Id);
        }
        player = playerFactory.CreatePlayer(Context.ConnectionId);
        playerConnectionService.AddPlayer(Context.ConnectionId, player);

        logger.LogInformation(
            "GetPlayerId called - PlayerId: {PlayerId}, ConnectionId: {ConnectionId}",
            player.Id, Context.ConnectionId);

        return Task.FromResult(player.Id);
    }

    public async Task RequestMatchmaking()
    {
        var player = playerConnectionService.GetPlayerByConnectionId(Context.ConnectionId);
        if (player is null)
        {
            logger.LogWarning(
                "RequestMatchmaking called but player not found - ConnectionId: {ConnectionId}",
                Context.ConnectionId);
            return;
        }

        logger.LogInformation(
            "RequestMatchmaking called - PlayerId: {PlayerId}, ConnectionId: {ConnectionId}",
            player.Id,
            Context.ConnectionId);

        var result = matchmakingService.RequestMatchmaking(player);

        if (result.Lobby is null) return;

        logger.LogInformation(
            "Lobby created - LobbyId: {LobbyId}",
            result.Lobby.Id);

        foreach (var lobbyPlayer in result.PlayersToNotify)
        {
            await SendLobbyStartAsync(lobbyPlayer, result.Lobby);
        }
    }

    public async Task UpdatePlayerPosition(Position newPosition)
    {
        var player = playerConnectionService.GetPlayerByConnectionId(Context.ConnectionId);
        if (player is null)
        {
            logger.LogWarning(
                "UpdatePlayerPosition called but player not found - ConnectionId: {ConnectionId}",
                Context.ConnectionId);
            return;
        }

        var lobby = lobbyManager.GetLobbyByPlayer(player);
        if (lobby is null)
        {
            logger.LogWarning(
                "UpdatePlayerPosition called but player is not in lobby - ConnectionId: {ConnectionId}",
                Context.ConnectionId);
            return;
        }

        var result = gameService.ValidateAndUpdatePosition(player, newPosition, lobby);

        logger.LogDebug(
            "UpdatePlayerPosition called - PlayerId: {PlayerId}, X: {X}, Y: {Y}, LobbyId: {LobbyId}, NeedsCorrection: {NeedsCorrection}",
            player.Id,
            result.CorrectedPosition.X,
            result.CorrectedPosition.Y,
            lobby.Id,
            result.NeedsCorrection);

        if (result.NeedsCorrection)
        {
            await Clients.Client(Context.ConnectionId).PositionCorrected(result.CorrectedPosition);
            return;
        }

        foreach (var otherPlayer in result.PlayersToNotify)
        {
            var otherConnectionId = playerConnectionService.GetConnectionIdByPlayer(otherPlayer);
            if (otherConnectionId is not null)
            {
                await Clients.Client(otherConnectionId).PlayerPositionUpdated(player.Id, result.CorrectedPosition);
            }
        }
    }

    public async Task LeaveGame()
    {
        var player = playerConnectionService.GetPlayerByConnectionId(Context.ConnectionId);
        if (player is null)
        {
            logger.LogWarning(
                "LeaveGame called but player not found - ConnectionId: {ConnectionId}",
                Context.ConnectionId);
            return;
        }

        logger.LogInformation(
            "LeaveGame called - PlayerId: {PlayerId}, ConnectionId: {ConnectionId}",
            player.Id,
            Context.ConnectionId);

        await RemovePlayerFromLobbyAsync(player);
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var player = playerConnectionService.GetPlayerByConnectionId(Context.ConnectionId);
        if (player is not null)
        {
            logger.LogInformation(
                "Player disconnected - PlayerId: {PlayerId}, ConnectionId: {ConnectionId}",
                player.Id,
                Context.ConnectionId);

            await RemovePlayerFromLobbyAsync(player);
            playerConnectionService.RemovePlayer(Context.ConnectionId);
        }

        await base.OnDisconnectedAsync(exception);
    }

    public async Task SendMessage(string message, ChatMessageType type)
    {
        if (message.Length > gameConfig.MaxMessageLength)
        {
            logger.LogWarning(
                "SendMessage called with message of length {MessageLength} which is greater than " +
                "the maximum length: {MaxMessageLength} - ConnectionId: {ConnectionId}",
                message.Length, gameConfig.MaxMessageLength, Context.ConnectionId);
            return;
        }
        message = message.Trim();
        if (message.Length == 0)
        {
            logger.LogWarning(
                "SendMessage called with empty message - ConnectionId: {ConnectionId}", Context.ConnectionId);
            return;
        }

        logger.LogInformation(
            "SendMessage called - Message: {Message}, Type: {Type}, ConnectionId: {ConnectionId}",
            message, type, Context.ConnectionId);
        var player = playerConnectionService.GetPlayerByConnectionId(Context.ConnectionId);
        if (player is null)
        {
            logger.LogWarning("SendMessage called, but player not found  - ConnectionId: {ConnectionId}", Context.ConnectionId);
            return;
        }

        var textMessage = new ChatMessage()
        {
            Id = $"msg_{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}_{Guid.NewGuid():N}",
            Text = message,
            Type = type,
            OwnerId = player.Id,
            OwnerName = player.Id[^10..],
            Timestamp = DateTime.UtcNow,
        };

        if (type == ChatMessageType.Global)
        {
            _ = database.SaveChatMessage(textMessage);
            await Clients.All.MessageReceived(textMessage);
            return;
        }

        var lobby = lobbyManager.GetLobbyByPlayer(player);
        if (lobby is null)
        {
            logger.LogWarning(
                "SendMessage to lobby called, but player is not in a lobby - ConnectionId: {ConnectionId}",
                Context.ConnectionId);
            return;
        }
        _ = database.SaveChatMessage(textMessage);
        foreach (var p in lobby.Players)
        {
            var connection = playerConnectionService.GetConnectionIdByPlayer(p);
            if (connection is not null)
                await Clients.Client(connection).MessageReceived(textMessage);
        }
    }

    public async Task PerformMeleeAttack()
    {
        var player = playerConnectionService.GetPlayerByConnectionId(Context.ConnectionId);
        if (player is null)
        {
            logger.LogWarning(
                "PerformMeleeAttack called but player not found - ConnectionId: {ConnectionId}",
                Context.ConnectionId);
            return;
        }

        var lobby = lobbyManager.GetLobbyByPlayer(player);
        if (lobby is null)
        {
            logger.LogWarning(
                "PerformMeleeAttack called but player is not in a lobby - ConnectionId: {ConnectionId}",
                Context.ConnectionId);
            return;
        }

        var result = gameService.PerformMeleeAttack(player, lobby);
        await HandleAttackAsync(result, lobby);
    }

    public async Task PerformProjectileAttack()
    {
        var player = playerConnectionService.GetPlayerByConnectionId(Context.ConnectionId);
        if (player is null)
        {
            logger.LogWarning(
                "PerformProjectileAttack called but player not found - ConnectionId: {ConnectionId}",
                Context.ConnectionId);
            return;
        }

        var lobby = lobbyManager.GetLobbyByPlayer(player);
        if (lobby is null)
        {
            logger.LogWarning(
                "PerformProjectileAttack called but player is not in a lobby - ConnectionId: {ConnectionId}",
                Context.ConnectionId);
            return;
        }

        var result = gameService.PerformProjectileAttack(player, lobby);
        await HandleAttackAsync(result, lobby);
    }

    public async Task PerformSpecialAttack()
    {
        var player = playerConnectionService.GetPlayerByConnectionId(Context.ConnectionId);
        if (player is null)
        {
            logger.LogWarning(
                "PerformSpecialAttack called but player not found - ConnectionId: {ConnectionId}",
                Context.ConnectionId);
            return;
        }

        var lobby = lobbyManager.GetLobbyByPlayer(player);
        if (lobby is null)
        {
            logger.LogWarning(
                "PerformSpecialAttack called but player is not in a lobby - ConnectionId: {ConnectionId}",
                Context.ConnectionId);
            return;
        }

        var result = gameService.PerformSpecialAttack(player, lobby);
        await HandleAttackAsync(result, lobby);
    }

    public async Task ProjectileHitPlayer(string projectileId, string hitPlayerId)
    {
        var projectileOwner = playerConnectionService.GetPlayerByConnectionId(Context.ConnectionId);
        if (projectileOwner is null)
        {
            logger.LogWarning(
                "ProjectileHitPlayer called but projectile owner not found - ConnectionId: {ConnectionId}",
                Context.ConnectionId);
            return;
        }

        var lobby = lobbyManager.GetLobbyByPlayer(projectileOwner);
        if (lobby is null)
        {
            logger.LogWarning(
                "ProjectileHitPlayer called but projectile owner is not in a lobby - ConnectionId: {ConnectionId}",
                Context.ConnectionId);
            return;
        }

        var hitPlayer = lobby.Players.FirstOrDefault(p => p.Id == hitPlayerId);
        if (hitPlayer is null)
        {
            logger.LogWarning(
                "ProjectileHitPlayer called but hit player not found - HitPlayerId: {HitPlayerId}",
                hitPlayerId);
            return;
        }

        var result = gameService.HandleProjectileHit(projectileId, hitPlayer, lobby);
        await HandleAttackAsync(result, lobby);
    }

    public async Task SpecialExplosion(string attackId)
    {
        var attackOwner = playerConnectionService.GetPlayerByConnectionId(Context.ConnectionId);
        if (attackOwner is null)
        {
            logger.LogWarning(
                "SpecialExplosion called but attack owner not found - ConnectionId: {ConnectionId}, AttackId: {AttackId}",
                Context.ConnectionId, attackId);
            return;
        }

        var lobby = lobbyManager.GetLobbyByPlayer(attackOwner);
        if (lobby is null)
        {
            logger.LogWarning(
                "SpecialExplosion called but attack owner is not in a lobby - ConnectionId: {ConnectionId}, AttackId: {AttackId}",
                Context.ConnectionId, attackId);
            return;
        }

        if (!lobby.LongLivedAttacks.TryGetValue(attackId, out var attackEntity))
        {
            logger.LogWarning(
                "SpecialExplosion called but attack id is not recognized - ConnectionId: {ConnectionId}, AttackId: {AttackId}",
                Context.ConnectionId, attackId);
            return;
        }

        if (attackEntity.OwnerId != attackOwner.Id)
        {
            logger.LogWarning(
                "SpecialExplosion called but caller does not own this attack - ConnectionId: {ConnectionId}, AttackId: {AttackId}",
                Context.ConnectionId, attackId);
            return;
        }

        var result = gameService.HandleSpecialExplosion(attackEntity, lobby);
        await HandleAttackAsync(result, lobby);
    }

    private async Task RemovePlayerFromLobbyAsync(Player player)
    {
        //In case LeaveGame was called when player was not in lobby, or when disconnecting
        matchmakingService.CancelMatchmaking(player);
        player.ResetPlayerState();
        var lobby = lobbyManager.GetLobbyByPlayer(player);
        var players = lobbyManager.RemovePlayerFromLobby(player);
        foreach (var lobbyPlayer in players)
        {
            lobbyPlayer.ResetPlayerState();
            var connectionId = playerConnectionService.GetConnectionIdByPlayer(lobbyPlayer);
            if (connectionId is not null)
            {
                await Clients.Client(connectionId).PlayerLeftLobby(player.Id);
            }
        }
        if (lobby?.Players.Count <= 1)
        {
            _ = database.SaveLobbyResult(new LobbyResult(lobby));
        }
    }

    private async Task SendLobbyStartAsync(Player player, GameLobby lobby)
    {
        var connectionId = playerConnectionService.GetConnectionIdByPlayer(player);
        if (connectionId is null)
        {
            logger.LogWarning(
                "Could not find connection ID for player {PlayerId}",
                player.Id);
            return;
        }

        logger.LogInformation(
            "Sending lobby start to player {PlayerId} (ConnectionId: {ConnectionId}) in lobby {LobbyId}",
            player.Id,
            connectionId,
            lobby.Id);

        try
        {
            await Clients.Client(connectionId).LobbyStarted(lobby.Id, lobby.Players);
            logger.LogInformation(
                "Successfully sent lobby start to player {PlayerId}",
                player.Id);
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Failed to send lobby start to player {PlayerId}",
                player.Id);
        }
    }

    private async Task HandleAttackAsync(AttackResult result, GameLobby lobby)
    {
        if (!result.Success) return;
        Player? winner = null;
        foreach (var player in result.PlayersToNotify)
        {
            var connectionId = playerConnectionService.GetConnectionIdByPlayer(player);
            if (connectionId is null) continue;

            await Clients.Client(connectionId).AttackPerformed(result.AttackEntites);
            foreach (var hit in result.HitPlayers)
            {
                if (!hit.Died)
                {
                    await Clients.Client(connectionId).PlayerDamaged(hit.Player.Id, hit.Damage, hit.NewHealth);
                    continue;
                }

                await Clients.Client(connectionId).PlayerDied(hit.Player.Id);
                winner ??= gameService.CheckWinCondition(lobby);
                if (winner is not null)
                {
                    await Clients.Client(connectionId).GameEnded(winner.Id);
                }
                else
                {
                    _ = RespawnPlayerAsync(hit.Player, lobby);
                }
            }
        }
        if (winner is not null)
        {
            _ = database.SaveLobbyResult(new LobbyResult(lobby, winner.Id));
            lobbyManager.RemoveLobby(lobby);
        }
    }

    private async Task RespawnPlayerAsync(Player deadPlayer, GameLobby lobby)
    {
        await Task.Delay(gameConfig.RespawnCooldownMs);
        if (gameService.CheckWinCondition(lobby) is not null) return;

        var respawnResult = gameService.RespawnPlayer(deadPlayer, lobby);
        foreach (var notifyPlayer in respawnResult.PlayersToNotify)
        {
            var connectionId = playerConnectionService.GetConnectionIdByPlayer(notifyPlayer);
            if (connectionId is not null)
            {
                await Clients.Client(connectionId)
                    .PlayerRespawned(deadPlayer.Id, respawnResult.RespawnPosition);
            }
        }
    }
}
