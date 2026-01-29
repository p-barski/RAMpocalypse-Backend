using RAMpocalypse.Server.Game;
using RAMpocalypse.Server.Services.Results;

namespace RAMpocalypse.Server.Services;

public class GameService(IGameConfig gameConfig) : IGameService
{
    private readonly IGameConfig gameConfig = gameConfig;

    public PositionUpdateResult ValidateAndUpdatePosition(Player player, Position newPosition, GameLobby lobby)
    {
        var result = new PositionUpdateResult();

        // Validate movement
        var updateTime = DateTime.UtcNow;
        var timeSinceLastUpdate = (updateTime - player.LastPositionUpdateTime).TotalSeconds;
        var validation = MovementValidator.ValidateMovement(
            player,
            newPosition,
            gameConfig.GameWidth,
            gameConfig.GameHeight,
            timeSinceLastUpdate);

        result.NeedsCorrection = !validation.IsValid;
        result.CorrectedPosition = validation.CorrectedPosition;
        player.Position = validation.CorrectedPosition;
        if (validation.IsValid)
        {
            result.PlayersToNotify = lobby.Players.Where(p => p != player).ToList();
        }

        player.LastPositionUpdateTime = updateTime;

        return result;
    }

    public AttackResult PerformMeleeAttack(Player attacker, Position attackDirection, GameLobby lobby)
    {
        var result = new AttackResult
        {
            AttackerId = attacker.Id,
            AttackType = AttackType.Melee,
            AttackDirection = attackDirection
        };

        if (!attacker.IsAlive) return result;

        // Check cooldown
        var attackTime = DateTime.UtcNow;
        var timeSinceLastAttack = (attackTime - attacker.LastMeleeAttackTime).TotalMilliseconds;
        if (timeSinceLastAttack < gameConfig.MeleeCooldownMs) return result;

        attacker.LastMeleeAttackTime = attackTime;

        // Calculate attack position (in front of player)
        var attackX = attacker.Position.X + (attackDirection.X * gameConfig.MeleeRange);
        var attackY = attacker.Position.Y + (attackDirection.Y * gameConfig.MeleeRange);
        var attackPosition = new Position(attackX, attackY);
        result.AttackPosition = attackPosition;

        // Check for hits on other players
        var hitPlayerInfos = new List<HitPlayerInfo>();
        foreach (var otherPlayer in lobby.Players.Where(p => p != attacker && p.IsAlive))
        {
            var distance = Position.CalculateDistance(attackPosition, otherPlayer.Position);
            if (distance > gameConfig.MeleeRange) continue;

            var oldHealth = otherPlayer.Health;
            otherPlayer.Health = Math.Max(0, otherPlayer.Health - gameConfig.MeleeDamage);
            var died = otherPlayer.Health <= 0;

            if (died)
            {
                otherPlayer.IsAlive = false;
                otherPlayer.DeathTime = attackTime;
            }

            hitPlayerInfos.Add(new HitPlayerInfo
            {
                Player = otherPlayer,
                Damage = oldHealth - otherPlayer.Health,
                NewHealth = otherPlayer.Health,
                Died = died
            });
        }

        result.Success = true;
        result.HitPlayers = hitPlayerInfos;
        result.PlayersToNotify = lobby.Players;

        return result;
    }

    public AttackResult PerformProjectileAttack(Player attacker, Position attackDirection, GameLobby lobby)
    {
        var result = new AttackResult
        {
            AttackerId = attacker.Id,
            AttackType = AttackType.Projectile,
            AttackDirection = attackDirection,
            AttackPosition = attacker.Position,
        };

        if (!attacker.IsAlive) return result;

        // Check cooldown
        var attackTime = DateTime.UtcNow;
        var timeSinceLastAttack = (attackTime - attacker.LastProjectileAttackTime).TotalMilliseconds;
        if (timeSinceLastAttack < gameConfig.ProjectileCooldownMs) return result;

        attacker.LastProjectileAttackTime = attackTime;

        // Projectile attacks are handled client-side, so we just broadcast the attack
        result.Success = true;
        result.HitPlayers = [];
        result.PlayersToNotify = lobby.Players;

        return result;
    }

    public AttackResult PerformSpecialAttack(Player attacker, Position attackPosition, GameLobby lobby)
    {
        var result = new AttackResult
        {
            AttackerId = attacker.Id,
            AttackType = AttackType.Special,
            AttackPosition = attackPosition,
            AttackDirection = attackPosition
        };

        if (!attacker.IsAlive) return result;

        // Check cooldown
        var attackTime = DateTime.UtcNow;
        var timeSinceLastAttack = (attackTime - attacker.LastSpecialAttackTime).TotalMilliseconds;
        if (timeSinceLastAttack < gameConfig.SpecialCooldownMs) return result;

        attacker.LastSpecialAttackTime = attackTime;

        // Special attack is area-of-effect around the player
        var hitPlayerInfos = new List<HitPlayerInfo>();
        foreach (var otherPlayer in lobby.Players.Where(p => p != attacker && p.IsAlive))
        {
            var distance = Position.CalculateDistance(attacker.Position, otherPlayer.Position);
            if (distance > gameConfig.SpecialAttackRange) continue;

            var oldHealth = otherPlayer.Health;
            otherPlayer.Health = Math.Max(0, otherPlayer.Health - gameConfig.SpecialDamage);
            var died = otherPlayer.Health <= 0;

            if (died)
            {
                otherPlayer.IsAlive = false;
                otherPlayer.DeathTime = attackTime;
            }

            hitPlayerInfos.Add(new HitPlayerInfo
            {
                Player = otherPlayer,
                Damage = oldHealth - otherPlayer.Health,
                NewHealth = otherPlayer.Health,
                Died = died
            });
        }

        result.Success = true;
        result.HitPlayers = hitPlayerInfos;
        result.PlayersToNotify = lobby.Players;

        return result;
    }

    public AttackResult HandleProjectileHit(Player projectileOwner, Player hitPlayer, GameLobby lobby)
    {
        var result = new AttackResult
        {
            AttackerId = projectileOwner.Id,
            AttackType = AttackType.Projectile,
            AttackPosition = hitPlayer.Position,
            AttackDirection = hitPlayer.Position
        };

        if (!hitPlayer.IsAlive) return result;

        // Apply damage
        var oldHealth = hitPlayer.Health;
        hitPlayer.Health = Math.Max(0, hitPlayer.Health - gameConfig.ProjectileDamage);
        var died = hitPlayer.Health <= 0;

        if (died)
        {
            hitPlayer.IsAlive = false;
            hitPlayer.DeathTime = DateTime.UtcNow;
        }

        result.Success = true;
        result.HitPlayers = [new()
        {
            Player = hitPlayer,
            Damage = oldHealth - hitPlayer.Health,
            NewHealth = hitPlayer.Health,
            Died = died
        }];
        result.PlayersToNotify = lobby.Players;

        return result;
    }

    public Player? CheckWinCondition(GameLobby lobby)
    {
        var alivePlayers = lobby.Players.Where(p => p.IsAlive).ToList();
        if (alivePlayers.Count == 1)
        {
            return alivePlayers[0];
        }

        return null;
    }

    public RespawnResult RespawnPlayer(Player player, GameLobby lobby)
    {
        if (player.IsAlive)
        {
            return new RespawnResult
            {
                RespawnPosition = player.Position,
                PlayersToNotify = []
            };
        }

        // Respawn at random corner
        var (respawnPosition, _) = Position.GetRandomOppositeCorners(gameConfig.GameWidth, gameConfig.GameHeight);

        player.Position = respawnPosition;
        player.Health = player.MaxHealth;
        player.IsAlive = true;
        player.DeathTime = DateTime.MinValue;

        return new RespawnResult
        {
            RespawnPosition = respawnPosition,
            PlayersToNotify = lobby.Players.ToList()
        };
    }
}
