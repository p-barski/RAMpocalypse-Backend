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
    IChatCooldowns chatCooldowns,
    IDatabase database,
    TimeProvider timeProvider) : Hub<ISendMethods>
{
    private readonly ILogger<GameHub> logger = logger;
    private readonly IPlayerConnectionService playerConnectionService = playerConnectionService;
    private readonly IMatchmakingService matchmakingService = matchmakingService;
    private readonly ILobbyManager lobbyManager = lobbyManager;
    private readonly IGameService gameService = gameService;
    private readonly IPlayerFactory playerFactory = playerFactory;
    private readonly IGameConfig gameConfig = gameConfig;
    private readonly IChatCooldowns chatCooldowns = chatCooldowns;
    private readonly IDatabase database = database;
    private readonly TimeProvider timeProvider = timeProvider;

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
        player = playerFactory.CreatePlayer();
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
            "Lobby created/joined - LobbyId: {LobbyId}",
            result.Lobby.Id);

        playerFactory.RandomizePlayersSprites(result.PlayersToNotify, result.ExistingPlayersToNotify);
        foreach (var lobbyPlayer in result.PlayersToNotify)
        {
            await SendLobbyStartAsync(lobbyPlayer, result.Lobby);
        }
        foreach (var existingPlayer in result.ExistingPlayersToNotify)
        {
            foreach (var newPlayer in result.PlayersToNotify)
            {
                var connectionId = playerConnectionService.GetConnectionIdByPlayer(existingPlayer);
                if (connectionId is null)
                {
                    logger.LogWarning(
                        "Could not find connection ID for player {PlayerId}",
                        existingPlayer.Id);
                    return;
                }

                try
                {
                    await Clients.Client(connectionId).PlayerJoinedLobby(newPlayer);
                }
                catch (Exception ex)
                {
                    logger.LogError(
                        ex,
                        "Failed to send player joined lobby to player {PlayerId}",
                        existingPlayer.Id);
                }
            }
        }
    }

    public Task SetPlayerName(string name)
    {
        var player = playerConnectionService.GetPlayerByConnectionId(Context.ConnectionId);
        if (player is null)
        {
            logger.LogWarning(
                "SetPlayerName called but player not found - ConnectionId: {ConnectionId}",
                Context.ConnectionId);
            return Task.CompletedTask;
        }

        name = name.Trim();
        if (name.Length > gameConfig.MaxNameLength)
        {
            logger.LogWarning(
                "SetPlayerName called with name of length {NameLength} which is greater than " +
                "the maximum length: {MaxNameLength} - ConnectionId: {ConnectionId}",
                name.Length, gameConfig.MaxNameLength, Context.ConnectionId);
            return Task.CompletedTask;
        }

        player.Name = name;
        logger.LogInformation(
            "SetPlayerName called - PlayerId: {PlayerId}, ConnectionId: {ConnectionId}",
            player.Id, Context.ConnectionId);
        return Task.CompletedTask;
    }

    public Task UpdatePlayerPosition(Position newPosition)
    {
        var player = playerConnectionService.GetPlayerByConnectionId(Context.ConnectionId);
        if (player is null)
        {
            logger.LogWarning(
                "UpdatePlayerPosition called but player not found - ConnectionId: {ConnectionId}",
                Context.ConnectionId);
            return Task.CompletedTask;
        }

        var lobby = lobbyManager.GetLobbyByPlayer(player);
        if (lobby is null)
        {
            logger.LogWarning(
                "UpdatePlayerPosition called but player is not in lobby - ConnectionId: {ConnectionId}",
                Context.ConnectionId);
            return Task.CompletedTask;
        }

        if (!player.IsAlive) return Task.CompletedTask;

        var result = gameService.ValidateAndUpdatePosition(player, newPosition, lobby);

        if (result.NeedsCorrection)
        {
            _ = Clients.Client(Context.ConnectionId).PositionCorrected(result.FinalPosition);
        }

        foreach (var otherPlayer in result.PlayersToNotify)
        {
            var otherConnectionId = playerConnectionService.GetConnectionIdByPlayer(otherPlayer);
            if (otherConnectionId is not null)
            {
                _ = Clients.Client(otherConnectionId).PlayerPositionUpdated(player.Id, result.FinalPosition);
            }
        }
        return Task.CompletedTask;
    }

    public Task<bool> Dash(double xVelocity, double yVelocity)
    {
        var player = playerConnectionService.GetPlayerByConnectionId(Context.ConnectionId);
        if (player is null)
        {
            logger.LogWarning(
                "Dash called but player not found - ConnectionId: {ConnectionId}",
                Context.ConnectionId);
            return Task.FromResult(false);
        }

        var lobby = lobbyManager.GetLobbyByPlayer(player);
        if (lobby is null)
        {
            logger.LogWarning(
                "Dash called but player is not in lobby - ConnectionId: {ConnectionId}",
                Context.ConnectionId);
            return Task.FromResult(false);
        }

        if (!player.IsAlive) return Task.FromResult(false);

        return Task.FromResult(gameService.ValidateDash(player, xVelocity, yVelocity));
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

    public override async Task OnConnectedAsync()
    {
        await base.OnConnectedAsync();
        if (!chatCooldowns.TryAdd(Context.ConnectionId))
        {
            logger.LogWarning("Could not add chat cooldown for connection: {ConnectionId}", Context.ConnectionId);
        }
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        if (!chatCooldowns.TryRemove(Context.ConnectionId))
        {
            logger.LogWarning("Could not remove chat cooldown for connection: {ConnectionId}", Context.ConnectionId);
        }
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
        if (!chatCooldowns.TryGetValue(Context.ConnectionId, out var cooldown))
        {
            logger.LogWarning("Could not get chat cooldown for connection: {ConnectionId}", Context.ConnectionId);
            return;
        }
        var remainingCooldown = cooldown.GetRemainingCooldown();
        if (remainingCooldown > 0)
        {
            logger.LogWarning("Chat is on {RemainingCooldown}ms cooldown for connection: {ConnectionId}",
                remainingCooldown, Context.ConnectionId);
            return;
        }
        cooldown.UpdateCooldowns();

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

        var utcNow = timeProvider.GetUtcNow();
        var textMessage = new ChatMessage()
        {
            Id = $"msg_{utcNow.ToUnixTimeMilliseconds()}_{Guid.NewGuid():N}",
            Text = message,
            Type = type,
            OwnerId = player.Id,
            OwnerName = string.IsNullOrWhiteSpace(player.Name) ? player.Id[^10..] : player.Name,
            Timestamp = utcNow.UtcDateTime,
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
        var utcNow = timeProvider.GetUtcNow().UtcDateTime;
        matchmakingService.CancelMatchmaking(player);
        player.ResetPlayerState(utcNow);
        var lobby = lobbyManager.GetLobbyByPlayer(player);
        var players = lobbyManager.RemovePlayerFromLobby(player);
        foreach (var lobbyPlayer in players)
        {
            lobbyPlayer.ResetPlayerState(utcNow);
            var connectionId = playerConnectionService.GetConnectionIdByPlayer(lobbyPlayer);
            if (connectionId is not null)
            {
                await Clients.Client(connectionId).PlayerLeftLobby(player.Id);
            }
        }
        if (lobby?.Players.Count <= 1)
        {
            _ = database.SaveLobbyResult(new LobbyResult(lobby) { FinishTime = utcNow });
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
            _ = database.SaveLobbyResult(new LobbyResult(lobby, winner.Id)
            {
                FinishTime = timeProvider.GetUtcNow().UtcDateTime,
            });
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
