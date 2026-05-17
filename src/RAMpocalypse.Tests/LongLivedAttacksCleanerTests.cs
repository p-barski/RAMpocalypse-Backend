using NSubstitute;
using RAMpocalypse.Server.Game;
using RAMpocalypse.Server.Services;
using Xunit;

namespace RAMpocalypse.Tests;

public class LongLivedAttacksCleanerTests
{
    private static AttackEntity CreateAttack(string id, AttackType type, long creationTimeMs, long lifetimeMs) =>
        new()
        {
            Id = id,
            OwnerId = "owner",
            Type = type,
            CurrentPosition = new Position(0, 0),
            VelocityVector = new Position(0, 0),
            Lifetime = lifetimeMs,
            CreationTime = creationTimeMs,
        };

    private static GameLobby CreateLobbyWithAttack(AttackEntity attack)
    {
        var lobby = new GameLobby($"lobby_{attack.Id}", MaxNumberOfPlayers.Two, DateTime.UtcNow);
        lobby.LongLivedAttacks[attack.Id] = attack;
        return lobby;
    }

    private static LongLivedAttacksCleaner CreateSut(ILobbyManager lobbyManager, TimeProvider timeProvider) =>
        new(lobbyManager, timeProvider);

    [Fact]
    public void PruneExpiredAttacks_RemovesProjectile_WhenLifetimeElapsed()
    {
        const long creation = 1000;
        const long lifetime = 500;
        var timeProvider = new TestTimeProvider(creation + lifetime);
        var attack = CreateAttack("id", AttackType.Projectile, creation, lifetime);
        var lobby = CreateLobbyWithAttack(attack);
        var lobbyManager = Substitute.For<ILobbyManager>();
        lobbyManager.GetActiveLobbies().Returns([lobby]);

        CreateSut(lobbyManager, timeProvider).PruneExpiredAttacks();

        Assert.Empty(lobby.LongLivedAttacks);
    }

    [Fact]
    public void PruneExpiredAttacks_RemovesSpecial_WhenLifetimeElapsed()
    {
        const long creation = 2000;
        const long lifetime = 750;
        var timeProvider = new TestTimeProvider(creation + lifetime);
        var attack = CreateAttack("id", AttackType.Special, creation, lifetime);
        var lobby = CreateLobbyWithAttack(attack);
        var lobbyManager = Substitute.For<ILobbyManager>();
        lobbyManager.GetActiveLobbies().Returns([lobby]);

        CreateSut(lobbyManager, timeProvider).PruneExpiredAttacks();

        Assert.Empty(lobby.LongLivedAttacks);
    }

    [Fact]
    public void PruneExpiredAttacks_KeepsProjectile_UntilLifetimeBoundary()
    {
        const long creation = 10_000;
        const long lifetime = 400;
        var timeProvider = new TestTimeProvider(creation + lifetime - 1);
        var attack = CreateAttack("id", AttackType.Projectile, creation, lifetime);
        var lobby = CreateLobbyWithAttack(attack);
        var lobbyManager = Substitute.For<ILobbyManager>();
        lobbyManager.GetActiveLobbies().Returns([lobby]);

        CreateSut(lobbyManager, timeProvider).PruneExpiredAttacks();

        Assert.Single(lobby.LongLivedAttacks);
        Assert.True(lobby.LongLivedAttacks.TryGetValue(attack.Id, out var kept) && ReferenceEquals(kept, attack));
    }

    [Fact]
    public void PruneExpiredAttacks_KeepsSpecial_UntilLifetimeBoundary()
    {
        const long creation = 50_000;
        const long lifetime = 1200;
        var timeProvider = new TestTimeProvider(creation + lifetime - 1);
        var attack = CreateAttack("id", AttackType.Special, creation, lifetime);
        var lobby = CreateLobbyWithAttack(attack);
        var lobbyManager = Substitute.For<ILobbyManager>();
        lobbyManager.GetActiveLobbies().Returns([lobby]);

        CreateSut(lobbyManager, timeProvider).PruneExpiredAttacks();

        Assert.Single(lobby.LongLivedAttacks);
        Assert.True(lobby.LongLivedAttacks.TryGetValue(attack.Id, out var kept) && ReferenceEquals(kept, attack));
    }

    [Fact]
    public void PruneExpiredAttacks_MixedAcrossTypes_RemovesOnlyExpiredEntries()
    {
        const long baseTime = 1_000_000;
        var expiredProjectile = CreateAttack("xp", AttackType.Projectile, baseTime, 100);
        var freshSpecial = CreateAttack("fs", AttackType.Special, baseTime, 10_000);
        var expiredSpecial = CreateAttack("xs", AttackType.Special, baseTime, 500);
        var timeProvider = new TestTimeProvider(baseTime + 501);
        var lobby = new GameLobby("lobby_mixed", MaxNumberOfPlayers.Two, DateTime.UtcNow);
        lobby.LongLivedAttacks[expiredProjectile.Id] = expiredProjectile;
        lobby.LongLivedAttacks[freshSpecial.Id] = freshSpecial;
        lobby.LongLivedAttacks[expiredSpecial.Id] = expiredSpecial;

        var lobbyManager = Substitute.For<ILobbyManager>();
        lobbyManager.GetActiveLobbies().Returns([lobby]);

        CreateSut(lobbyManager, timeProvider).PruneExpiredAttacks();

        Assert.Single(lobby.LongLivedAttacks);
        Assert.True(lobby.LongLivedAttacks.ContainsKey(freshSpecial.Id));
    }
}
