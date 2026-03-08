using RAMpocalypse.Server.Game;
using RAMpocalypse.Server.Services.Results;

namespace RAMpocalypse.Server.Services;

public class GameService(IGameConfig gameConfig) : IGameService
{
    private readonly IGameConfig gameConfig = gameConfig;

    public PositionUpdateResult ValidateAndUpdatePosition(Player player, Position newPosition, GameLobby lobby)
    {
        var result = new PositionUpdateResult() { CorrectedPosition = player.Position };

        var updateTime = DateTime.UtcNow;
        var timeSinceLastUpdate = (updateTime - player.LastPositionUpdateTime).TotalSeconds;
        player.LastPositionUpdateTime = updateTime;
        var distanceSquared = Position.CalculateDistanceSquared(player.Position, newPosition);
        var maxAllowedDistance = gameConfig.MaxDistancePerUpdate + (gameConfig.MaxMovementSpeed * timeSinceLastUpdate);

        if (distanceSquared > maxAllowedDistance * maxAllowedDistance)
        {
            // TODO: Move player to the closest valid position
            result.NeedsCorrection = true;
            return result;
        }

        var halfWidth = player.SpriteData.Width * player.SpriteData.ScaleFactor / 2;
        var halfHeight = player.SpriteData.Height * player.SpriteData.ScaleFactor / 2;
        var correctedX = Math.Max(halfWidth, Math.Min(newPosition.X, gameConfig.GameWidth - halfWidth));
        var correctedY = Math.Max(halfHeight, Math.Min(newPosition.Y, gameConfig.GameHeight - halfHeight));

        result.CorrectedPosition = new Position(correctedX, correctedY, newPosition.Angle);
        result.NeedsCorrection = Math.Abs(correctedX - newPosition.X) > 0.1 || Math.Abs(correctedY - newPosition.Y) > 0.1;

        if (player.Position != result.CorrectedPosition)
        {
            player.Position = result.CorrectedPosition;
            result.PlayersToNotify = lobby.Players.Where(p => p != player).ToList();
        }
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

        var attackTime = DateTime.UtcNow;
        var timeSinceLastAttack = (attackTime - attacker.LastMeleeAttackTime).TotalMilliseconds;
        if (timeSinceLastAttack < gameConfig.MeleeCooldownMs) return result;

        attacker.LastMeleeAttackTime = attackTime;
        // Check for hits on other players
        var hitPlayerInfos = new List<HitPlayerInfo>();
        foreach (var otherPlayer in lobby.Players.Where(p => p != attacker && p.IsAlive))
        {
            var hitDetected = false;
            foreach (var (corner1, corner2) in otherPlayer.GetHitboxLines())
            {
                // TODO calculate distance from position of a weapon instead of player center
                var pointOnLine = Position.CalculateClosestPointOnLine(attacker.Position, corner1, corner2);
                if (Position.IsInsideHalfCircle(attacker.Position, pointOnLine, gameConfig.MeleeRange, attacker.Position.Angle))
                {
                    hitDetected = true;
                    result.AttackPosition = pointOnLine;
                    break;
                }
            }
            if (!hitDetected) continue;

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

        int xOffset = player.SpriteData.Width * player.SpriteData.ScaleFactor / 2;
        int yOffset = player.SpriteData.Height * player.SpriteData.ScaleFactor / 2;
        var (respawnPosition, _) = Position.GetRandomOppositeCorners(gameConfig.GameWidth, gameConfig.GameHeight, xOffset, yOffset);

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
