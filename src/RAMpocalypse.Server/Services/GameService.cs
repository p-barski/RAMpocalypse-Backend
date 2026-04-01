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

    public AttackResult PerformMeleeAttack(Player attacker, GameLobby lobby)
    {
        if (!attacker.IsAlive) return new AttackResult();

        var attackTime = DateTime.UtcNow;
        var timeSinceLastAttack = (attackTime - attacker.LastMeleeAttackTime).TotalMilliseconds;
        if (timeSinceLastAttack < gameConfig.MeleeCooldownMs) return new AttackResult();

        var result = new AttackResult
        {
            AttackEntites = [new AttackEntity
            {
                OwnerId = attacker.Id,
                Type = AttackType.Melee,
                CurrentPosition = new Position(-500, -500),
                VelocityVector = new Position(0, 0, 0),
                Lifetime = 0,
                CreationTime = new DateTimeOffset(attackTime).ToUnixTimeMilliseconds(),
            }]
        };

        attacker.LastMeleeAttackTime = attackTime;
        var attackPosition = attacker.GetAttackPosition();
        var hitPlayerInfos = new List<HitPlayerInfo>();
        foreach (var otherPlayer in lobby.Players.Where(p => p != attacker && p.IsAlive))
        {
            var hitDetected = false;
            foreach (var (corner1, corner2) in otherPlayer.GetHitboxLines())
            {
                var pointOnLine = Position.CalculateClosestPointOnLine(attackPosition, corner1, corner2);
                if (Position.IsInsideHalfCircle(attackPosition, pointOnLine, gameConfig.MeleeRange, attacker.Position.Angle))
                {
                    hitDetected = true;
                    //TODO this can result in more distant line to be stored, but whatever for now
                    result.AttackEntites[0].CurrentPosition = pointOnLine;
                    result.AttackEntites[0].Lifetime = gameConfig.MeleeLifetime;
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
            break; // only 1 player should be hit
        }

        result.Success = true;
        result.HitPlayers = hitPlayerInfos;
        result.PlayersToNotify = lobby.Players;

        return result;
    }

    public AttackResult PerformProjectileAttack(Player attacker, GameLobby lobby)
    {
        if (!attacker.IsAlive) return new AttackResult();

        var attackTime = DateTime.UtcNow;
        var timeSinceLastAttack = (attackTime - attacker.LastProjectileAttackTime).TotalMilliseconds;
        if (timeSinceLastAttack < gameConfig.ProjectileCooldownMs) return new AttackResult();

        attacker.LastProjectileAttackTime = attackTime;

        double sin = Math.Sin(attacker.Position.Angle);
        double cos = Math.Cos(attacker.Position.Angle);
        var velocityVector = new Position(sin * gameConfig.ProjectileSpeed, -cos * gameConfig.ProjectileSpeed);
        var attackEntity = new AttackEntity
        {
            OwnerId = attacker.Id,
            Type = AttackType.Projectile,
            CurrentPosition = attacker.GetAttackPosition(sin, cos),
            VelocityVector = velocityVector,
            Lifetime = gameConfig.ProjectileLifetime,
            CreationTime = new DateTimeOffset(attackTime).ToUnixTimeMilliseconds(),
        };
        var result = new AttackResult()
        {
            Success = true,
            AttackEntites = [attackEntity],
            PlayersToNotify = lobby.Players,
        };
        return result;
    }

    public AttackResult PerformSpecialAttack(Player attacker, GameLobby lobby)
    {
        if (!attacker.IsAlive) return new AttackResult();

        var attackTime = DateTime.UtcNow;
        var timeSinceLastAttack = (attackTime - attacker.LastSpecialAttackTime).TotalMilliseconds;
        if (timeSinceLastAttack < gameConfig.SpecialCooldownMs) return new AttackResult();

        var attackPosition = attacker.GetAttackPosition();
        var result = new AttackResult
        {
            AttackEntites = [new AttackEntity {
                OwnerId = attacker.Id,
                Type = AttackType.Special,
                CurrentPosition = attackPosition,
                VelocityVector = new Position(0, 0),
                Lifetime = gameConfig.ProjectileLifetime,
                CreationTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            }]
        };

        attacker.LastSpecialAttackTime = attackTime;

        // Special attack is area-of-effect around the player
        var hitPlayerInfos = new List<HitPlayerInfo>();
        var rangeSquared = gameConfig.SpecialAttackRange * gameConfig.SpecialAttackRange;
        foreach (var otherPlayer in lobby.Players.Where(p => p != attacker && p.IsAlive))
        {
            var distance = Position.CalculateDistanceSquared(attackPosition, otherPlayer.Position);
            if (distance > rangeSquared) continue;

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

    public AttackResult HandleProjectileHit(string attackId, Player hitPlayer, GameLobby lobby)
    {
        if (!hitPlayer.IsAlive) return new AttackResult();

        var result = new AttackResult
        {
            AttackEntites = [new AttackEntity {
                Id = attackId,
                Type = AttackType.ProjectileHit,
            }]
        };

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
