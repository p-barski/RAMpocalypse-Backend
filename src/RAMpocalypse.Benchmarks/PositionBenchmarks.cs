using BenchmarkDotNet.Attributes;
using RAMpocalypse.Server.Game;

namespace RAMpocalypse.Benchmarks;

[ShortRunJob]
[MemoryDiagnoser]
public class PositionBenchmarks
{
    private static readonly Random rand = new();
    private readonly Position positionA = new(rand.NextDouble() * 1000, rand.NextDouble() * 1000);
    private readonly Position positionB = new(rand.NextDouble() * 1000, rand.NextDouble() * 1000);
    private readonly Position positionC = new(rand.NextDouble() * 1000, rand.NextDouble() * 1000);
    private readonly double radius = rand.NextDouble() * 10000;
    private readonly double angle = rand.NextDouble() * Math.Tau - Math.PI;

    [Benchmark]
    public void CalculateDistance()
    {
        Position.CalculateDistance(positionA, positionB);
    }

    [Benchmark]
    public void CalculateClosestPointOnLine()
    {
        Position.CalculateClosestPointOnLine(positionA, positionB, positionC);
    }

    [Benchmark]
    public void IsInsideHalfCircle()
    {
        Position.IsInsideHalfCircle(positionA, positionB, radius, angle);
    }
}
