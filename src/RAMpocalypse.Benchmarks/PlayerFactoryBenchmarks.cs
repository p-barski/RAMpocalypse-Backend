using BenchmarkDotNet.Attributes;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using RAMpocalypse.Server.Game;
using RAMpocalypse.Server.Services;

namespace RAMpocalypse.Benchmarks;

[ShortRunJob]
[MemoryDiagnoser]
public class PlayerFactoryBenchmarks
{
    private const int numberOfPlayers = 4;
    private const int numberOfSprites = 3;
    private readonly List<Player> players = new(numberOfPlayers);
    private PlayerFactory playerFactory = null!;

    [GlobalSetup]
    public void Setup()
    {
        SpriteInfoJson json = new();
        for (int i = 0; i < numberOfSprites; i++)
        {
            string url = $"{i}.png";
            json.Data.Add(new(url));
            json.Data.Add(new($"weapon_{url}", type: SpriteType.Weapon));
        }
        for (int i = 0; i < numberOfPlayers; i++)
        {
            string name = i.ToString();
            Player player = new(name, new(name));
            player.SubEntities.Add(new(new(0, 0), new(name, type: SpriteType.Weapon), name));
            players.Add(player);
        }
        var monitor = Substitute.For<IOptionsMonitor<SpriteInfoJson>>();
        monitor.CurrentValue.Returns(json);
        SpriteInfo spriteInfo = new(monitor, "serverurl");
        playerFactory = new(Substitute.For<ILogger<PlayerFactory>>(), spriteInfo, TimeProvider.System);
    }

    [Benchmark]
    public void PlayerFactory_RandomizePlayersSprites()
    {
        playerFactory.RandomizePlayersSprites(players);
    }
}
