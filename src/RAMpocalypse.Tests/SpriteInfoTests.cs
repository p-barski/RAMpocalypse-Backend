using Microsoft.Extensions.Options;
using NSubstitute;
using RAMpocalypse.Server.Game;
using RAMpocalypse.Server.Services;
using Xunit;

namespace RAMpocalypse.Tests;

public class SpriteInfoTests
{
    private const string SERVER_URL = "https://example.com/";

    private static SpriteInfo CreateSpriteInfo(List<SpriteData> data)
    {
        var monitor = Substitute.For<IOptionsMonitor<SpriteInfoJson>>();
        monitor.CurrentValue.Returns(new SpriteInfoJson { Data = data });
        return new(monitor, SERVER_URL);
    }

    [Fact]
    public void TryGetValue_ExistingKey_ReturnsTrueAndSprite()
    {
        var sprite = new SpriteData("player_1.png", type: SpriteType.Player);
        var sut = CreateSpriteInfo([sprite]);

        var result = sut.TryGetValue("player_1.png", out var found);

        Assert.True(result);
        Assert.NotNull(found);
        Assert.Equal($"{SERVER_URL}player_1.png", found.URL);
    }

    [Fact]
    public void TryGetValue_ExistingKey_UrlHasServerUrlPrefix()
    {
        var sprite = new SpriteData("player_1.png", type: SpriteType.Player);
        var sut = CreateSpriteInfo([sprite]);

        sut.TryGetValue("player_1.png", out var found);

        Assert.StartsWith(SERVER_URL, found!.URL);
    }

    [Fact]
    public void TryGetValue_NonExistingKey_ReturnsFalse()
    {
        var sprite = new SpriteData("player_1.png", type: SpriteType.Player);
        var sut = CreateSpriteInfo([sprite]);

        var result = sut.TryGetValue("nonexistent.png", out var found);

        Assert.False(result);
        Assert.Null(found);
    }

    [Fact]
    public void GetAllOfType_ReturnsOnlyMatchingType()
    {
        var sut = CreateSpriteInfo([
            new SpriteData("player_1.png", type: SpriteType.Player),
            new SpriteData("player_2.png", type: SpriteType.Player),
            new SpriteData("weapon_1.png", type: SpriteType.Weapon),
        ]);

        var result = sut.GetAllOfType(SpriteType.Player);

        Assert.Equal(2, result.Count);
        Assert.All(result, s => Assert.Equal(SpriteType.Player, s.Type));
    }

    [Fact]
    public void GetAllOfType_NoMatchingType_ReturnsFallback()
    {
        var sut = CreateSpriteInfo([
            new SpriteData("weapon_1.png", type: SpriteType.Weapon),
        ]);

        var result = sut.GetAllOfType(SpriteType.Player);

        Assert.Single(result);
        Assert.Contains("FallbackPlayer", result[0].URL);
        Assert.Equal(SpriteType.Player, result[0].Type);
    }

    [Fact]
    public void OnChange_UpdatesCache()
    {
        Action<SpriteInfoJson, string?>? onChangeCallback = null;
        var monitor = Substitute.For<IOptionsMonitor<SpriteInfoJson>>();
        monitor.CurrentValue.Returns(new SpriteInfoJson { Data = [] });
        monitor.OnChange(Arg.Do<Action<SpriteInfoJson, string?>>(cb => onChangeCallback = cb));

        var sut = new SpriteInfo(monitor, SERVER_URL);

        var newData = new SpriteInfoJson
        {
            Data = [new SpriteData("player_1.png", type: SpriteType.Player)]
        };
        monitor.CurrentValue.Returns(newData);
        onChangeCallback?.Invoke(newData, null);

        var result = sut.TryGetValue("player_1.png", out _);
        Assert.True(result);
    }
}
