using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;
using RAMpocalypse.Server.Game;

namespace RAMpocalypse.Server.Hubs;

public class GameHub(ILogger<GameHub> logger) : Hub
{
    private readonly ILogger<GameHub> _logger = logger;
    private static readonly ConcurrentDictionary<string, Player> ConnectionToPlayerMap = [];
    private static readonly ConcurrentDictionary<Player, GameLobby> PlayerToLobbyMap = [];
    private static readonly ConcurrentDictionary<string, GameLobby> Lobbies = [];
    private static readonly ConcurrentQueue<Player> WaitingPlayers = [];
    private const MaxNumberOfPlayers LOBBY_SIZE = MaxNumberOfPlayers.Two;
    private const int GAME_WIDTH = 1920;
    private const int GAME_HEIGHT = 1080;
    private const float PLAYER_WIDTH = 64f; // Approximate player width in game world units
    private const float PLAYER_HEIGHT = 32f; // Approximate player height in game world units

    // Attack constants
    private const int MELEE_COOLDOWN_MS = 500; // 0.5 seconds
    private const int PROJECTILE_COOLDOWN_MS = 1000; // 1 second
    private const int SPECIAL_COOLDOWN_MS = 3000; // 3 seconds
    private const float MELEE_RANGE = 300f; // Melee attack range
    private const float PROJECTILE_SPEED = 800f; // Projectiles per second
    private const int MELEE_DAMAGE = 25;
    private const int PROJECTILE_DAMAGE = 15;
    private const int SPECIAL_DAMAGE = 40;
    private const int RESPAWN_COOLDOWN_MS = 3000; // 3 seconds

    public Task<string> Connect()
    {
        // Assign random sprite variant (1-4 for now, can be expanded)
        var random = new Random();
        var spriteVariant = random.Next(1, 5);
        var player = new Player(GeneratePlayerId(), new Position(0, 0), spriteVariant);
        ConnectionToPlayerMap[Context.ConnectionId] = player;

        _logger.LogInformation($"Connect called - PlayerId: {player.Id}, ConnectionId: {Context.ConnectionId}");

        return Task.FromResult(player.Id);
    }

    public async Task<bool> RequestMatchmaking()
    {
        // Verify player exists in ConnectionToPlayerMap
        if (!ConnectionToPlayerMap.TryGetValue(Context.ConnectionId, out var player))
        {
            _logger.LogWarning($"RequestMatchmaking called but player not found - ConnectionId: {Context.ConnectionId}");
            return false;
        }

        // Check if player is already in a lobby (prevent duplicate requests)
        if (PlayerToLobbyMap.ContainsKey(player))
        {
            _logger.LogWarning($"RequestMatchmaking called but player already in lobby - PlayerId: {player.Id}, ConnectionId: {Context.ConnectionId}");
            return false;
        }

        _logger.LogInformation($"RequestMatchmaking called - PlayerId: {player.Id}, ConnectionId: {Context.ConnectionId}");

        // Try to find another waiting player
        Player? otherPlayer = null;
        while (WaitingPlayers.TryDequeue(out var waitingPlayer))
        {
            // Verify the waiting player is still connected and not in a lobby
            // Get the active player object from ConnectionToPlayerMap to ensure we use the correct instance
            var activeWaitingPlayer = ConnectionToPlayerMap.Values.FirstOrDefault(p => p.Id == waitingPlayer.Id);
            if (activeWaitingPlayer != null &&
                !PlayerToLobbyMap.ContainsKey(activeWaitingPlayer) &&
                activeWaitingPlayer.Id != player.Id) // Ensure we're not matching with ourselves
            {
                otherPlayer = activeWaitingPlayer;
                break;
            }
        }

        if (otherPlayer == null)
        {
            // No match found - add to waiting queue
            WaitingPlayers.Enqueue(player);
            _logger.LogInformation($"Player added to waiting queue - PlayerId: {player.Id}");
            return true;
        }
        // Match found - create lobby with both players
        var lobby = new GameLobby(GenerateLobbyId(), LOBBY_SIZE);

        // Initialize player positions at opposite corners
        var (corner1, corner2) = Position.GetRandomOppositeCorners(GAME_WIDTH, GAME_HEIGHT);
        player.Position = corner1;
        otherPlayer.Position = corner2;

        // Add players to lobby
        lobby.AddPlayer(player);
        lobby.AddPlayer(otherPlayer);

        // Map players to lobby
        PlayerToLobbyMap[player] = lobby;
        PlayerToLobbyMap[otherPlayer] = lobby;

        // Store lobby
        Lobbies[lobby.Id] = lobby;

        _logger.LogInformation($"Lobby created - LobbyId: {lobby.Id}, Player1: {player.Id}, Player2: {otherPlayer.Id}");

        // Send lobby start events to both players
        await SendLobbyStart(player, lobby);
        await SendLobbyStart(otherPlayer, lobby);

        return true;
    }

    public async Task UpdatePlayerPosition(Position newPosition)
    {
        if (!ConnectionToPlayerMap.TryGetValue(Context.ConnectionId, out var player))
        {
            _logger.LogWarning($"UpdatePlayerPosition called but player not found - ConnectionId: {Context.ConnectionId}");
            return;
        }

        // Only update position if player is in a lobby
        if (!PlayerToLobbyMap.TryGetValue(player, out var lobby))
        {
            return; // Player not in lobby, don't broadcast
        }

        // Validate movement
        var timeSinceLastUpdate = (float)(DateTime.UtcNow - player.LastPositionUpdateTime).TotalSeconds;
        var validation = MovementValidator.ValidateMovement(
            player.Position,
            newPosition,
            GAME_WIDTH,
            GAME_HEIGHT,
            PLAYER_WIDTH,
            PLAYER_HEIGHT,
            timeSinceLastUpdate);

        if (!validation.IsValid)
        {
            _logger.LogWarning($"Invalid movement detected - PlayerId: {player.Id}, Reason: {validation.Reason}, " +
                               $"Old: ({player.Position.X}, {player.Position.Y}), New: ({newPosition.X}, {newPosition.Y}), Corrected: " +
                               $"({validation.CorrectedPosition.X}, {validation.CorrectedPosition.Y})");

            // Send position correction back to client
            var connectionId = ConnectionToPlayerMap.FirstOrDefault(kvp => kvp.Value == player).Key;
            if (connectionId != null)
            {
                await Clients.Client(connectionId).SendAsync("PositionCorrected", validation.CorrectedPosition.X,
                                                             validation.CorrectedPosition.Y, validation.Reason);
            }

            // Use corrected position
            player.Position = validation.CorrectedPosition;
        }
        else
        {
            player.Position = validation.CorrectedPosition;
        }

        player.LastPositionUpdateTime = DateTime.UtcNow;

        _logger.LogInformation($"UpdatePlayerPosition called - PlayerId: {player.Id}, X: {player.Position.X}, Y: " +
                               $"{player.Position.Y}, LobbyId: {lobby.Id}, Valid: {validation.IsValid}");

        // Send validated position to other players in the same lobby
        var otherPlayers = lobby.Players.Where(p => p != player).ToList();
        foreach (var otherPlayer in otherPlayers)
        {
            var otherConnectionId = ConnectionToPlayerMap.FirstOrDefault(kvp => kvp.Value == otherPlayer).Key;
            if (otherConnectionId != null)
            {
                await Clients.Client(otherConnectionId).SendAsync("PlayerPositionUpdated", player.Id, player.Position.X, player.Position.Y);
            }
        }
    }

    public async Task LeaveGame()
    {
        if (!ConnectionToPlayerMap.TryGetValue(Context.ConnectionId, out var player))
        {
            _logger.LogWarning("LeaveGame called but player not found - ConnectionId: {ConnectionId}",
                Context.ConnectionId);
            return;
        }

        _logger.LogInformation($"LeaveGame called - PlayerId: {player.Id}, ConnectionId: {Context.ConnectionId}");

        await RemovePlayerFromLobby(player);
        ConnectionToPlayerMap.TryRemove(Context.ConnectionId, out _);
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        if (ConnectionToPlayerMap.TryGetValue(Context.ConnectionId, out var player))
        {
            _logger.LogInformation($"Player disconnected - PlayerId: {player.Id}, ConnectionId: {Context.ConnectionId}");
            await RemovePlayerFromLobby(player);
            ConnectionToPlayerMap.TryRemove(Context.ConnectionId, out _);
        }

        await base.OnDisconnectedAsync(exception);
    }

    private async Task RemovePlayerFromLobby(Player player)
    {
        if (!PlayerToLobbyMap.TryRemove(player, out var lobby))
        {
            return;
        }
        lobby.Players.Remove(player);

        // Notify other players in lobby
        foreach (var otherPlayerId in lobby.Players)
        {
            var otherConnectionId = ConnectionToPlayerMap.FirstOrDefault(kvp => kvp.Value == otherPlayerId).Key;
            if (otherConnectionId != null)
            {
                await Clients.Client(otherConnectionId).SendAsync("PlayerLeftLobby", player.Id);
            }
        }

        // Remove lobby if empty
        if (lobby.Players.Count == 0)
        {
            Lobbies.TryRemove(lobby.Id, out _);
        }
    }

    private async Task SendLobbyStart(Player player, GameLobby lobby)
    {
        // Find connection ID for this player
        string? connectionId = null;
        foreach (var kvp in ConnectionToPlayerMap)
        {
            // Compare by ID since Player objects might be different instances
            if (kvp.Value.Id == player.Id)
            {
                connectionId = kvp.Key;
                break;
            }
        }

        if (connectionId != null)
        {
            // Send own position, other player's position, and all player IDs
            _logger.LogInformation($"Sending lobby start to player {player.Id} (ConnectionId: {connectionId}) in lobby {lobby.Id}");
            try
            {
                await Clients.Client(connectionId).SendAsync("LobbyStarted", lobby.Id, lobby.Players);
                _logger.LogInformation($"Successfully sent lobby start to player {player.Id}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to send lobby start to player {player.Id}");
            }
        }
        else
        {
            _logger.LogWarning($"Could not find connection ID for player {player.Id}. Available connections: {string.Join(", ", ConnectionToPlayerMap.Keys)}");
        }
    }

    private static string GeneratePlayerId()
    {
        return $"player_{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}_{Guid.NewGuid():N}";
    }

    private static string GenerateLobbyId()
    {
        return $"lobby_{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}_{Guid.NewGuid():N}";
    }
    public async Task SendMessage(string user, string message)
    {
        _logger.LogInformation($"SendMessage called - User: {user}, Message: {message}, ConnectionId: {Context.ConnectionId}");
        await Clients.All.SendAsync("ReceiveMessage", user, message);
    }

    public async Task PerformMeleeAttack(Position attackDirection)
    {
        if (!ConnectionToPlayerMap.TryGetValue(Context.ConnectionId, out var player))
        {
            _logger.LogWarning($"PerformMeleeAttack called but player not found - ConnectionId: {Context.ConnectionId}");
            return;
        }

        if (!PlayerToLobbyMap.TryGetValue(player, out var lobby))
        {
            return; // Player not in lobby
        }

        if (!player.IsAlive)
        {
            return; // Dead players can't attack
        }

        // Check cooldown
        var timeSinceLastAttack = (DateTime.UtcNow - player.LastMeleeAttackTime).TotalMilliseconds;
        if (timeSinceLastAttack < MELEE_COOLDOWN_MS)
        {
            _logger.LogWarning($"Melee attack on cooldown - PlayerId: {player.Id}, TimeSinceLastAttack: {timeSinceLastAttack}ms");
            return;
        }

        player.LastMeleeAttackTime = DateTime.UtcNow;

        // Calculate attack position (in front of player)
        var attackX = player.Position.X + (attackDirection.X * MELEE_RANGE);
        var attackY = player.Position.Y + (attackDirection.Y * MELEE_RANGE);
        var attackPosition = new Position(attackX, attackY);

        // Check for hits on other players
        var hitPlayers = new List<Player>();
        foreach (var otherPlayer in lobby.Players)
        {
            if (otherPlayer == player || !otherPlayer.IsAlive)
                continue;

            var distance = CalculateDistance(attackX, attackY, otherPlayer.Position.X, otherPlayer.Position.Y);
            if (distance <= MELEE_RANGE)
            {
                hitPlayers.Add(otherPlayer);
            }
        }

        // Apply damage to hit players
        foreach (var hitPlayer in hitPlayers)
        {
            hitPlayer.Health = Math.Max(0, hitPlayer.Health - MELEE_DAMAGE);
            _logger.LogInformation($"Melee attack hit - Attacker: {player.Id}, Target: {hitPlayer.Id}, Damage: {MELEE_DAMAGE}, NewHealth: {hitPlayer.Health}");

            // Check if player died
            if (hitPlayer.Health <= 0)
            {
                hitPlayer.IsAlive = false;
                await NotifyPlayerDied(hitPlayer, lobby);
            }
            else
            {
                // Notify damage
                var connectionId = ConnectionToPlayerMap.FirstOrDefault(kvp => kvp.Value == hitPlayer).Key;
                if (connectionId != null)
                {
                    await Clients.Client(connectionId).SendAsync("PlayerDamaged", hitPlayer.Id, MELEE_DAMAGE, hitPlayer.Health);
                    await Clients.Client(Context.ConnectionId).SendAsync("PlayerDamaged", hitPlayer.Id, MELEE_DAMAGE, hitPlayer.Health);
                }
            }
        }

        // Broadcast attack to all players in lobby
        foreach (var lobbyPlayer in lobby.Players)
        {
            var connectionId = ConnectionToPlayerMap.FirstOrDefault(kvp => kvp.Value == lobbyPlayer).Key;
            if (connectionId != null)
            {
                await Clients.Client(connectionId).SendAsync("AttackPerformed", player.Id, (int)AttackType.Melee, attackPosition, attackDirection);
            }
        }
    }

    public async Task PerformProjectileAttack(Position attackDirection)
    {
        if (!ConnectionToPlayerMap.TryGetValue(Context.ConnectionId, out var player))
        {
            _logger.LogWarning($"PerformProjectileAttack called but player not found - ConnectionId: {Context.ConnectionId}");
            return;
        }

        if (!PlayerToLobbyMap.TryGetValue(player, out var lobby))
        {
            return; // Player not in lobby
        }

        if (!player.IsAlive)
        {
            return; // Dead players can't attack
        }

        // Check cooldown
        var timeSinceLastAttack = (DateTime.UtcNow - player.LastProjectileAttackTime).TotalMilliseconds;
        if (timeSinceLastAttack < PROJECTILE_COOLDOWN_MS)
        {
            _logger.LogWarning($"Projectile attack on cooldown - PlayerId: {player.Id}, TimeSinceLastAttack: {timeSinceLastAttack}ms");
            return;
        }

        player.LastProjectileAttackTime = DateTime.UtcNow;

        // Broadcast projectile creation to all players in lobby
        foreach (var lobbyPlayer in lobby.Players)
        {
            var connectionId = ConnectionToPlayerMap.FirstOrDefault(kvp => kvp.Value == lobbyPlayer).Key;
            if (connectionId != null)
            {
                await Clients.Client(connectionId).SendAsync("AttackPerformed", player.Id, (int)AttackType.Projectile, player.Position, attackDirection);
            }
        }
    }

    public async Task PerformSpecialAttack(Position attackPosition)
    {
        if (!ConnectionToPlayerMap.TryGetValue(Context.ConnectionId, out var player))
        {
            _logger.LogWarning("PerformSpecialAttack called but player not found - ConnectionId: {ConnectionId}", Context.ConnectionId);
            return;
        }

        if (!PlayerToLobbyMap.TryGetValue(player, out var lobby))
        {
            return; // Player not in lobby
        }

        if (!player.IsAlive)
        {
            return; // Dead players can't attack
        }

        // Check cooldown
        var timeSinceLastAttack = (DateTime.UtcNow - player.LastSpecialAttackTime).TotalMilliseconds;
        if (timeSinceLastAttack < SPECIAL_COOLDOWN_MS)
        {
            _logger.LogWarning($"Special attack on cooldown - PlayerId: {player.Id}, TimeSinceLastAttack: {timeSinceLastAttack}ms");
            return;
        }

        player.LastSpecialAttackTime = DateTime.UtcNow;

        // Special attack is area-of-effect around the player
        var specialRange = 400f;
        var hitPlayers = new List<Player>();
        foreach (var otherPlayer in lobby.Players)
        {
            if (otherPlayer == player || !otherPlayer.IsAlive)
                continue;

            var distance = CalculateDistance(player.Position.X, player.Position.Y, otherPlayer.Position.X, otherPlayer.Position.Y);
            if (distance <= specialRange)
            {
                hitPlayers.Add(otherPlayer);
            }
        }

        // Apply damage to hit players
        foreach (var hitPlayer in hitPlayers)
        {
            hitPlayer.Health = Math.Max(0, hitPlayer.Health - SPECIAL_DAMAGE);
            _logger.LogInformation($"Special attack hit - Attacker: {player.Id}, Target: {hitPlayer.Id}, Damage: {SPECIAL_DAMAGE}, NewHealth: {hitPlayer.Health}");

            // Check if player died
            if (hitPlayer.Health <= 0)
            {
                hitPlayer.IsAlive = false;
                await NotifyPlayerDied(hitPlayer, lobby);
            }
            else
            {
                // Notify damage
                foreach (var lobbyPlayer in lobby.Players)
                {
                    var connectionId = ConnectionToPlayerMap.FirstOrDefault(kvp => kvp.Value == lobbyPlayer).Key;
                    if (connectionId != null)
                    {
                        await Clients.Client(connectionId).SendAsync("PlayerDamaged", hitPlayer.Id, SPECIAL_DAMAGE, hitPlayer.Health);
                    }
                }
            }
        }

        // Broadcast attack to all players in lobby
        foreach (var lobbyPlayer in lobby.Players)
        {
            var connectionId = ConnectionToPlayerMap.FirstOrDefault(kvp => kvp.Value == lobbyPlayer).Key;
            if (connectionId != null)
            {
                await Clients.Client(connectionId).SendAsync("AttackPerformed", player.Id, (int)AttackType.Special, attackPosition, attackPosition);
            }
        }
    }

    public async Task ProjectileHitPlayer(string projectileOwnerId, string hitPlayerId)
    {
        if (!ConnectionToPlayerMap.TryGetValue(Context.ConnectionId, out var projectileOwner))
        {
            return;
        }

        if (projectileOwner.Id != projectileOwnerId)
        {
            return; // Only the projectile owner can report hits
        }

        if (!PlayerToLobbyMap.TryGetValue(projectileOwner, out var lobby))
        {
            return;
        }

        var hitPlayer = lobby.Players.FirstOrDefault(p => p.Id == hitPlayerId);
        if (hitPlayer == null || !hitPlayer.IsAlive)
        {
            return;
        }

        // Apply damage
        hitPlayer.Health = Math.Max(0, hitPlayer.Health - PROJECTILE_DAMAGE);
        _logger.LogInformation($"Projectile hit - Attacker: {projectileOwner.Id}, Target: {hitPlayer.Id}, Damage: {PROJECTILE_DAMAGE}, NewHealth: {hitPlayer.Health}");

        // Check if player died
        if (hitPlayer.Health <= 0)
        {
            hitPlayer.IsAlive = false;
            await NotifyPlayerDied(hitPlayer, lobby);
        }
        else
        {
            // Notify damage
            var connectionId = ConnectionToPlayerMap.FirstOrDefault(kvp => kvp.Value == hitPlayer).Key;
            if (connectionId != null)
            {
                await Clients.Client(connectionId).SendAsync("PlayerDamaged", hitPlayer.Id, PROJECTILE_DAMAGE, hitPlayer.Health);
            }
        }
    }

    private static float CalculateDistance(float x1, float y1, float x2, float y2)
    {
        var dx = x2 - x1;
        var dy = y2 - y1;
        return (float)Math.Sqrt(dx * dx + dy * dy);
    }

    private async Task NotifyPlayerDied(Player deadPlayer, GameLobby lobby)
    {
        _logger.LogInformation($"Player died - PlayerId: {deadPlayer.Id}");
        deadPlayer.DeathTime = DateTime.UtcNow;

        // Notify all players in lobby
        foreach (var lobbyPlayer in lobby.Players)
        {
            var connectionId = ConnectionToPlayerMap.FirstOrDefault(kvp => kvp.Value == lobbyPlayer).Key;
            if (connectionId != null)
            {
                await Clients.Client(connectionId).SendAsync("PlayerDied", deadPlayer.Id);
            }
        }

        // Check for win condition
        var alivePlayers = lobby.Players.Where(p => p.IsAlive).ToList();
        if (alivePlayers.Count == 1)
        {
            var winner = alivePlayers[0];
            _logger.LogInformation($"Game ended - Winner: {winner.Id}");

            // Notify all players
            foreach (var lobbyPlayer in lobby.Players)
            {
                var connectionId = ConnectionToPlayerMap.FirstOrDefault(kvp => kvp.Value == lobbyPlayer).Key;
                if (connectionId != null)
                {
                    await Clients.Client(connectionId).SendAsync("GameEnded", winner.Id, lobby.Players);
                }
            }
        }
        else
        {
            // Schedule respawn
            _ = Task.Run(async () =>
            {
                await Task.Delay(RESPAWN_COOLDOWN_MS);
                await RespawnPlayer(deadPlayer, lobby);
            });
        }
    }

    private async Task RespawnPlayer(Player player, GameLobby lobby)
    {
        if (player.IsAlive)
        {
            return; // Already alive
        }

        // Respawn at random corner
        var (corner1, corner2) = Position.GetRandomOppositeCorners(GAME_WIDTH, GAME_HEIGHT);
        var random = new Random();
        var respawnPosition = random.Next(0, 2) == 0 ? corner1 : corner2;

        player.Position = respawnPosition;
        player.Health = player.MaxHealth;
        player.IsAlive = true;
        player.DeathTime = DateTime.MinValue;

        _logger.LogInformation($"Player respawned - PlayerId: {player.Id}, Position: ({respawnPosition.X}, {respawnPosition.Y})");

        // Notify all players in lobby
        foreach (var lobbyPlayer in lobby.Players)
        {
            var connectionId = ConnectionToPlayerMap.FirstOrDefault(kvp => kvp.Value == lobbyPlayer).Key;
            if (connectionId != null)
            {
                await Clients.Client(connectionId).SendAsync("PlayerRespawned", player.Id, respawnPosition.X, respawnPosition.Y);
            }
        }
    }
}
