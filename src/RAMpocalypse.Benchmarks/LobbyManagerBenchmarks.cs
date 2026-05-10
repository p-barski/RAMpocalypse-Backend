using BenchmarkDotNet.Attributes;
using RAMpocalypse.Server.Game;
using RAMpocalypse.Server.Services;

namespace RAMpocalypse.Benchmarks;

[ShortRunJob]
[MemoryDiagnoser]
public class LobbyManagerBenchmarks
{
    private static readonly SpriteData BenchSprite = new("bench");
    private LobbyManager lobbyManager = null!;

    [Params(0, 1, 16, 64, 256, 512)]
    public int LobbyCount { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        lobbyManager = new LobbyManager(new GameConfig());

        for (int i = 0; i < LobbyCount; i++)
        {
            lobbyManager.CreateLobby(new($"bench-{LobbyCount}-{i}-1", BenchSprite), new($"bench-{LobbyCount}-{i}-2", BenchSprite));
        }
    }

    [Benchmark]
    public IReadOnlyCollection<GameLobby> GetActiveLobbies()
    {
        return lobbyManager.GetActiveLobbies();
    }
}
