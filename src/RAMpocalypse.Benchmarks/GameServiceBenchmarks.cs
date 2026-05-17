using BenchmarkDotNet.Attributes;
using RAMpocalypse.Server.Game;
using RAMpocalypse.Server.Services;

namespace RAMpocalypse.Benchmarks;

[ShortRunJob]
[MemoryDiagnoser]
public class GameServiceBenchmarks
{
    private readonly GameService gameService = new(new GameConfig(), TimeProvider.System);
    private readonly Player player = new("bench-player", new(""));
    private readonly GameLobby lobby = new("bench-lobby", MaxNumberOfPlayers.Four, DateTime.UtcNow)!;
    private readonly Position originalPosition = new(100.0, 100.0);
    private readonly Position newPosition = new(105.0, 102.0);

    [GlobalSetup]
    public void Setup()
    {
        lobby.AddPlayer(player);
    }

    [Benchmark]
    public void ValidateAndUpdatePosition()
    {
        player.Position = originalPosition;
        player.LastPositionUpdateTime = DateTime.UtcNow.AddSeconds(-0.02);
        gameService.ValidateAndUpdatePosition(player, newPosition, lobby);
    }
}
