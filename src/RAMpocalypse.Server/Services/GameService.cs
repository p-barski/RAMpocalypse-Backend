using RAMpocalypse.Server.Game;
using RAMpocalypse.Server.Services.Results;
using static RAMpocalypse.Server.MathUtils;

namespace RAMpocalypse.Server.Services;

public class GameService(IGameConfig gameConfig, TimeProvider timeProvider) : IGameService
{
    private readonly IGameConfig gameConfig = gameConfig;
    private readonly TimeProvider timeProvider = timeProvider;

    private string GenerateAttackId() =>
        $"attack_{timeProvider.GetUtcNow().ToUnixTimeMilliseconds()}_{Guid.NewGuid():N}";

    // Newly joined players are briefly untargetable, so they can't be hit before their own
    // client has finished the join sequence and is able to move/fight back
    private bool IsSpawnProtected(Player player, DateTime now) =>
        (now - player.JoinTime).TotalMilliseconds < gameConfig.SpawnProtectionMs;

    public PositionUpdateResult ValidateAndUpdatePosition(Player player, Position newPosition, GameLobby lobby)
    {
        var updateTime = timeProvider.GetUtcNow().UtcDateTime;
        var timeSinceLastUpdate = (updateTime - player.LastPositionUpdateTime).TotalSeconds;
        player.LastPositionUpdateTime = updateTime;
        var finalPosition = player.Position;
        if (player.IsDashing)
        {
            var currentDashDurationMs = (updateTime - player.LastDashTime).TotalMilliseconds;
            var dashDurationOverflowSeconds = Math.Max(currentDashDurationMs - gameConfig.DashDurationMs, 0) / 1000.0;
            var deltaTime = Math.Max(timeSinceLastUpdate - dashDurationOverflowSeconds, 0);
            finalPosition = player.Position + player.DashVelocity * deltaTime;
            player.IsDashing = currentDashDurationMs < gameConfig.DashDurationMs;
        }
        else
        {
            var diffVector = newPosition - player.Position;
            var xDirection = Math.Sign(diffVector.X);
            var yDirection = Math.Sign(diffVector.Y);
            var isDiagonalMovement = xDirection != 0 && yDirection != 0;
            var diagonalDivisor = Math.Max(Convert.ToInt32(isDiagonalMovement) * SQRT_2, 1);
            Position directionVector = new(xDirection / diagonalDivisor, yDirection / diagonalDivisor);
            // limit deltaTime to prevent weird teleporting
            var deltaTime = Math.Min(timeSinceLastUpdate, gameConfig.PositionUpdateIntervalMs * 2 / 1000.0);
            finalPosition = player.Position + directionVector * gameConfig.MovementSpeed * deltaTime;
        }

        // Clamp to game boundaries
        var halfScaleFactor = player.SpriteData.ScaleFactor / 2;
        var minWidth = player.SpriteData.Width * halfScaleFactor;
        var minHeight = player.SpriteData.Height * halfScaleFactor;
        var maxWidth = gameConfig.GameWidth - minWidth;
        var maxHeight = gameConfig.GameHeight - minHeight;
        finalPosition = new(Math.Clamp(finalPosition.X, minWidth, maxWidth),
            Math.Clamp(finalPosition.Y, minHeight, maxHeight), finalPosition.Angle);
        newPosition = new(Math.Clamp(newPosition.X, minWidth, maxWidth),
            Math.Clamp(newPosition.Y, minHeight, maxHeight), newPosition.Angle);

        var errorRate = gameConfig.MovementSpeed * gameConfig.PositionUpdateIntervalMs * 2 / 1000.0;
        var needsCorrection = Math.Abs(finalPosition.X - newPosition.X) > errorRate
            || Math.Abs(finalPosition.Y - newPosition.Y) > errorRate;
        if (!needsCorrection) finalPosition = newPosition;

        player.Position = finalPosition;
        return new()
        {
            NeedsCorrection = needsCorrection,
            FinalPosition = finalPosition,
            PlayersToNotify = lobby.Players.Where(p => p != player).ToList(),
        };
    }

    public bool ValidateDash(Player player, double xVelocity, double yVelocity)
    {
        var dashTime = timeProvider.GetUtcNow().UtcDateTime;
        var timeSinceLastDash = (dashTime - player.LastDashTime).TotalMilliseconds;
        if (timeSinceLastDash < gameConfig.DashCooldownMs) return false;

        var maxVelocity = gameConfig.DashSpeedMultiplier * gameConfig.MovementSpeed + 1;
        var maxTotalVelocity = maxVelocity / SQRT_2 * 2; // diagonal dash
        var xVelAbs = Math.Abs(xVelocity);
        var yVelAbs = Math.Abs(yVelocity);
        if (xVelAbs > maxVelocity || yVelAbs > maxVelocity || xVelAbs + yVelAbs > maxTotalVelocity) return false;

        player.LastDashTime = dashTime;
        player.LastPositionUpdateTime = dashTime;
        player.DashVelocity = new(xVelocity, yVelocity);
        player.IsDashing = true;
        return true;
    }

    public AttackResult PerformMeleeAttack(Player attacker, GameLobby lobby)
    {
        if (!attacker.IsAlive) return new AttackResult();

        var utcNow = timeProvider.GetUtcNow();
        var attackTime = utcNow.UtcDateTime;
        var attackTimeUnixMs = utcNow.ToUnixTimeMilliseconds();
        var timeSinceLastAnyAttack = (attackTime - attacker.LastAnyAttackTime).TotalMilliseconds;
        if (timeSinceLastAnyAttack < gameConfig.SharedAttackCooldownMs) return new AttackResult();

        var timeSinceLastAttack = (attackTime - attacker.LastMeleeAttackTime).TotalMilliseconds;
        if (timeSinceLastAttack < gameConfig.MeleeCooldownMs) return new AttackResult();

        var result = new AttackResult
        {
            AttackEntites = [new AttackEntity
            {
                Id = GenerateAttackId(),
                OwnerId = attacker.Id,
                Type = AttackType.Melee,
                CurrentPosition = new Position(-500, -500),
                VelocityVector = new Position(0, 0, 0),
                Lifetime = 0,
                CreationTime = attackTimeUnixMs,
            }]
        };

        attacker.LastMeleeAttackTime = attackTime;
        attacker.LastAnyAttackTime = attackTime;
        var attackPosition = attacker.GetAttackPosition();
        var hitPlayerInfos = new List<HitPlayerInfo>();
        foreach (var otherPlayer in lobby.Players.Where(p => p != attacker && p.IsAlive && !IsSpawnProtected(p, attackTime)))
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

        var utcNow = timeProvider.GetUtcNow();
        var attackTime = utcNow.UtcDateTime;
        var attackTimeUnixMs = utcNow.ToUnixTimeMilliseconds();
        var timeSinceLastAnyAttack = (attackTime - attacker.LastAnyAttackTime).TotalMilliseconds;
        if (timeSinceLastAnyAttack < gameConfig.SharedAttackCooldownMs) return new AttackResult();

        var timeSinceLastAttack = (attackTime - attacker.LastProjectileAttackTime).TotalMilliseconds;
        if (timeSinceLastAttack < gameConfig.ProjectileCooldownMs) return new AttackResult();

        attacker.LastProjectileAttackTime = attackTime;
        attacker.LastAnyAttackTime = attackTime;

        double sin = Math.Sin(attacker.Position.Angle);
        double cos = Math.Cos(attacker.Position.Angle);
        var velocityVector = new Position(sin * gameConfig.ProjectileSpeed, -cos * gameConfig.ProjectileSpeed);
        var attackEntity = new AttackEntity
        {
            Id = GenerateAttackId(),
            OwnerId = attacker.Id,
            Type = AttackType.Projectile,
            CurrentPosition = attacker.GetAttackPosition(sin, cos),
            VelocityVector = velocityVector,
            Lifetime = gameConfig.ProjectileLifetime,
            CreationTime = attackTimeUnixMs,
        };
        lobby.LongLivedAttacks[attackEntity.Id] = attackEntity;
        return new AttackResult()
        {
            Success = true,
            AttackEntites = [attackEntity],
            PlayersToNotify = lobby.Players,
        };
    }

    public AttackResult PerformSpecialAttack(Player attacker, GameLobby lobby)
    {
        if (!attacker.IsAlive) return new AttackResult();

        var utcNow = timeProvider.GetUtcNow();
        var attackTime = utcNow.UtcDateTime;
        var attackTimeUnixMs = utcNow.ToUnixTimeMilliseconds();
        var timeSinceLastAnyAttack = (attackTime - attacker.LastAnyAttackTime).TotalMilliseconds;
        if (timeSinceLastAnyAttack < gameConfig.SharedAttackCooldownMs) return new AttackResult();

        var timeSinceLastAttack = (attackTime - attacker.LastSpecialAttackTime).TotalMilliseconds;
        if (timeSinceLastAttack < gameConfig.SpecialCooldownMs) return new AttackResult();

        attacker.LastSpecialAttackTime = attackTime;
        attacker.LastAnyAttackTime = attackTime;

        double sin = Math.Sin(attacker.Position.Angle);
        double cos = Math.Cos(attacker.Position.Angle);
        var attackPosition = attacker.GetAttackPosition(sin, cos);
        var velocityVector = new Position(sin * gameConfig.SpecialSpeed, -cos * gameConfig.SpecialSpeed);
        var attackEntity = new AttackEntity
        {
            Id = GenerateAttackId(),
            OwnerId = attacker.Id,
            Type = AttackType.Special,
            CurrentPosition = attackPosition,
            VelocityVector = velocityVector,
            Lifetime = gameConfig.SpecialLifetime,
            CreationTime = attackTimeUnixMs,
        };
        lobby.LongLivedAttacks[attackEntity.Id] = attackEntity;
        return new AttackResult
        {
            Success = true,
            AttackEntites = [attackEntity],
            PlayersToNotify = lobby.Players,
        };
    }

    public AttackResult HandleSpecialExplosion(AttackEntity attackEntity, GameLobby lobby)
    {
        var utcNow = timeProvider.GetUtcNow();
        var explosionTime = utcNow.UtcDateTime;
        var explosionTimeUnixMs = utcNow.ToUnixTimeMilliseconds();
        var attackLifetime = explosionTimeUnixMs - attackEntity.CreationTime;
        if (attackLifetime < attackEntity.Lifetime)
            return new AttackResult();

        lobby.LongLivedAttacks.TryRemove(attackEntity.Id, out _);
        var deltaTime = (explosionTimeUnixMs - attackEntity.CreationTime) / 1000.0;
        var attackPosition = attackEntity.CurrentPosition + attackEntity.VelocityVector * deltaTime;
        var clampedX = Math.Clamp(attackPosition.X, this.gameConfig.SpecialAttackRange, gameConfig.GameWidth - this.gameConfig.SpecialAttackRange);
        var clampedY = Math.Clamp(attackPosition.Y, this.gameConfig.SpecialAttackRange, gameConfig.GameHeight - this.gameConfig.SpecialAttackRange);
        attackPosition = new Position(clampedX, clampedY);

        var hitPlayerInfos = new List<HitPlayerInfo>();
        var rangeSquared = gameConfig.SpecialAttackRange * gameConfig.SpecialAttackRange;
        foreach (var otherPlayer in lobby.Players.Where(p => p.IsAlive && !IsSpawnProtected(p, explosionTime)))
        {
            var hitDetected = false;
            foreach (var (corner1, corner2) in otherPlayer.GetHitboxLines())
            {
                var pointOnLine = Position.CalculateClosestPointOnLine(attackPosition, corner1, corner2);
                var distanceSqaured = Position.CalculateDistanceSquared(attackPosition, pointOnLine);
                if (distanceSqaured <= rangeSquared)
                {
                    hitDetected = true;
                    break;
                }
            }
            if (!hitDetected) continue;

            var oldHealth = otherPlayer.Health;
            otherPlayer.Health = Math.Max(0, otherPlayer.Health - gameConfig.SpecialDamage);
            var died = otherPlayer.Health <= 0;

            if (died)
            {
                otherPlayer.IsAlive = false;
                otherPlayer.DeathTime = explosionTime;
            }

            hitPlayerInfos.Add(new HitPlayerInfo
            {
                Player = otherPlayer,
                Damage = oldHealth - otherPlayer.Health,
                NewHealth = otherPlayer.Health,
                Died = died
            });
        }
        return new AttackResult
        {
            Success = true,
            HitPlayers = hitPlayerInfos,
            PlayersToNotify = hitPlayerInfos.Count > 0 ? lobby.Players : [],
        };
    }

    public AttackResult HandleProjectileHit(string attackId, Player hitPlayer, GameLobby lobby)
    {
        if (!lobby.LongLivedAttacks.TryGetValue(attackId, out var attackEntity))
            return new AttackResult();

        var utcNow = timeProvider.GetUtcNow();
        var now = utcNow.UtcDateTime;

        if (!hitPlayer.IsAlive || IsSpawnProtected(hitPlayer, now))
            return new AttackResult();

        var deltaTime = (utcNow.ToUnixTimeMilliseconds() - attackEntity.CreationTime) / 1000.0;
        var currentAttackPosition = attackEntity.CurrentPosition + attackEntity.VelocityVector * deltaTime;
        var halfWidth = hitPlayer.SpriteData.Width * hitPlayer.SpriteData.ScaleFactor / 2;
        var halfHeight = hitPlayer.SpriteData.Height * hitPlayer.SpriteData.ScaleFactor / 2;
        var cos = Math.Cos(-hitPlayer.Position.Angle);
        var sin = Math.Sin(-hitPlayer.Position.Angle);
        var dx = currentAttackPosition.X - hitPlayer.Position.X;
        var dy = currentAttackPosition.Y - hitPlayer.Position.Y;
        var localX = dx * cos - dy * sin;
        var localY = dx * sin + dy * cos;
        if (!(Math.Abs(localX) <= halfWidth && Math.Abs(localY) <= halfHeight))
            return new AttackResult();

        lobby.LongLivedAttacks.TryRemove(attackId, out _);

        var oldHealth = hitPlayer.Health;
        hitPlayer.Health = Math.Max(0, hitPlayer.Health - gameConfig.ProjectileDamage);
        var died = hitPlayer.Health <= 0;

        if (died)
        {
            hitPlayer.IsAlive = false;
            hitPlayer.DeathTime = now;
        }

        return new AttackResult
        {
            Success = true,
            HitPlayers = [new()
            {
                Player = hitPlayer,
                Damage = oldHealth - hitPlayer.Health,
                NewHealth = hitPlayer.Health,
                Died = died
            }],
            AttackEntites = [new AttackEntity {
                Id = attackId,
                Type = AttackType.ProjectileHit,
            }],
            PlayersToNotify = lobby.Players,
        };
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

        var xOffset = player.SpriteData.Width * player.SpriteData.ScaleFactor / 2.0;
        var yOffset = player.SpriteData.Height * player.SpriteData.ScaleFactor / 2.0;
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
