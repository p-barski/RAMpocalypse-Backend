using Microsoft.Extensions.Logging;
using NSubstitute;
using RAMpocalypse.Server.Game;
using RAMpocalypse.Server.Services;
using Xunit;

namespace RAMpocalypse.Tests;

public class PlayerFactoryTests
{
    private static SpriteData CreatePlayerSprite(string url) =>
        new(url, type: SpriteType.Player);

    private static SpriteData CreateWeaponSprite(string url) =>
        new(url, type: SpriteType.Weapon);

    private static PlayerFactory CreateFactory(List<SpriteData>? playerSprites = null, List<SpriteData>? weaponSprites = null)
    {
        var spriteInfo = Substitute.For<ISpriteInfo>();
        playerSprites ??= [CreatePlayerSprite("fallback_player.png")];
        weaponSprites ??= [CreateWeaponSprite("fallback_weapon.png")];
        spriteInfo.GetAllOfType(SpriteType.Player).Returns(playerSprites);
        spriteInfo.GetAllOfType(SpriteType.Weapon).Returns(weaponSprites);
        return new PlayerFactory(Substitute.For<ILogger<PlayerFactory>>(), spriteInfo, TimeProvider.System);
    }

    private static Player CreateTestPlayer()
    {
        var player = new Player("test_id", CreatePlayerSprite("player_test.png"));
        player.SubEntities.Add(new SubEntity(new Position(0, 0), CreateWeaponSprite("weapon_test.png"), "weapon_test_id"));
        return player;
    }

    [Fact]
    public void CreatePlayer_PlayerIdsAreUnique()
    {
        var sut = CreateFactory();

        var player1 = sut.CreatePlayer();
        var player2 = sut.CreatePlayer();

        Assert.NotEqual(player1.Id, player2.Id);
    }

    [Fact]
    public void CreatePlayer_HasId_SpriteData_AndWeapon()
    {
        var sut = CreateFactory();

        var player = sut.CreatePlayer();

        Assert.False(string.IsNullOrEmpty(player.Id));
        Assert.NotNull(player.SpriteData);
        Assert.Single(player.SubEntities);
        Assert.Equal(SpriteType.Weapon, player.SubEntities[0].SpriteData.Type);
        Assert.False(string.IsNullOrEmpty(player.SubEntities[0].Id));
    }

    [Fact]
    public void RandomizePlayersSprites_SinglePlayer_AssignsNewSprites()
    {
        var playerSprite = CreatePlayerSprite("player_1.png");
        var weaponSprite = CreateWeaponSprite("weapon_1.png");
        var sut = CreateFactory([playerSprite], [weaponSprite]);
        var player = CreateTestPlayer();

        sut.RandomizePlayersSprites([player]);

        Assert.Equal(playerSprite.URL, player.SpriteData.URL);
        Assert.Equal(weaponSprite.URL, player.SubEntities[0].SpriteData.URL);
    }

    [Fact]
    public void RandomizePlayersSprites_MoreSpritesThanPlayers_SpritesDoNotRepeat()
    {
        var sut = CreateFactory(
            playerSprites: [
                CreatePlayerSprite("player_1.png"),
                CreatePlayerSprite("player_2.png"),
                CreatePlayerSprite("player_3.png"),
            ],
            weaponSprites: [
                CreateWeaponSprite("weapon_1.png"),
                CreateWeaponSprite("weapon_2.png"),
                CreateWeaponSprite("weapon_3.png"),
            ]
        );
        var players = new List<Player>
        {
            CreateTestPlayer(),
            CreateTestPlayer(),
        };

        sut.RandomizePlayersSprites(players);

        var urls = players.Select(p => p.SpriteData.URL).ToList();
        Assert.Equal(urls.Count, urls.Distinct().Count());
        urls = players.Select(p => p.SubEntities[0].SpriteData.URL).ToList();
        Assert.Equal(urls.Count, urls.Distinct().Count());
    }

    [Fact]
    public void RandomizePlayersSprites_MorePlayersThanSprites_AllSpritesAreNew()
    {
        var playerSprite = CreatePlayerSprite("player_1.png");
        var weaponSprite = CreateWeaponSprite("weapon_1.png");
        var sut = CreateFactory([playerSprite], [weaponSprite]);
        var players = new List<Player>
        {
            CreateTestPlayer(),
            CreateTestPlayer(),
            CreateTestPlayer(),
        };

        sut.RandomizePlayersSprites(players);

        Assert.All(players, p => Assert.Equal(playerSprite, p.SpriteData));
        Assert.All(players, p => Assert.Equal(weaponSprite, p.SubEntities[0].SpriteData));
    }

    [Fact]
    public void RandomizePlayersSprites_EmptyPlayerList_DoesNotThrow()
    {
        var sut = CreateFactory();

        var exception = Record.Exception(() => sut.RandomizePlayersSprites([]));

        Assert.Null(exception);
    }

    [Fact]
    public void RandomizePlayersSprites_PlayerWithNoWeapon_DoesNotThrow_AndCreatesWeapon()
    {
        List<Player> players = [new("id", new("url"))];
        var sut = CreateFactory();

        var exception = Record.Exception(() => sut.RandomizePlayersSprites(players));

        Assert.Null(exception);
        Assert.NotNull(players[0].SubEntities.ElementAtOrDefault(0));
    }

    [Fact]
    public void RandomizePlayersSprites_SpriteTakenByExistingPlayer_JoinerGetsDifferentOne()
    {
        var playerSprite1 = CreatePlayerSprite("player_1.png");
        var playerSprite2 = CreatePlayerSprite("player_2.png");
        var weaponSprite = CreateWeaponSprite("weapon_1.png");
        var sut = CreateFactory([playerSprite1, playerSprite2], [weaponSprite]);
        var existingPlayer = CreateTestPlayer();
        existingPlayer.SpriteData = playerSprite1;
        var joiner = CreateTestPlayer();

        sut.RandomizePlayersSprites([joiner], [existingPlayer]);

        Assert.Equal(playerSprite2.URL, joiner.SpriteData.URL);
    }

    [Fact]
    public void RandomizePlayersSprites_AllSpritesTakenByExistingPlayers_JoinerFallsBackToReusingOne()
    {
        var playerSprite = CreatePlayerSprite("player_1.png");
        var weaponSprite = CreateWeaponSprite("weapon_1.png");
        var sut = CreateFactory([playerSprite], [weaponSprite]);
        var existingPlayer = CreateTestPlayer();
        existingPlayer.SpriteData = playerSprite;
        var joiner = CreateTestPlayer();

        var exception = Record.Exception(() => sut.RandomizePlayersSprites([joiner], [existingPlayer]));

        Assert.Null(exception);
        Assert.Equal(playerSprite.URL, joiner.SpriteData.URL);
    }

    [Fact]
    public void RandomizePlayersSprites_WeaponOffsetAdjustedBasedOnSpriteSize()
    {
        SpriteData playerSprite = new("player_1.png", 30, 20, 3.0, SpriteType.Player);
        SpriteData weaponSprite = new("weapon_1.png", 10, 16, 2.0, SpriteType.Weapon);
        var sut = CreateFactory([playerSprite], [weaponSprite]);
        var players = new List<Player>
        {
            CreateTestPlayer(),
            CreateTestPlayer(),
            CreateTestPlayer(),
        };

        sut.RandomizePlayersSprites(players);

        var weaponOffsets = players.Select(p => p.SubEntities[0].Position).ToList();
        Assert.All(weaponOffsets, offset =>
        {
            Assert.Equal(35, offset.X);
            Assert.Equal(0, offset.Y);
            Assert.Equal(0, offset.Angle);
        });
    }
}
