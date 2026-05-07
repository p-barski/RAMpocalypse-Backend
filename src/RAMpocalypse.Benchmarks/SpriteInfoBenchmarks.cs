using BenchmarkDotNet.Attributes;
using Microsoft.Extensions.Options;
using NSubstitute;
using RAMpocalypse.Server.Services;

namespace RAMpocalypse.Benchmarks;

[ShortRunJob]
[MemoryDiagnoser]
public class SpriteInfoBenchmarks
{
    private const string serverurl = "serverurl";
    private readonly SpriteInfoJson emptyData = new() { Data = [] };
    private SpriteInfo spriteInfo = null!;

    [GlobalSetup]
    public void Setup()
    {
        var monitor = Substitute.For<IOptionsMonitor<SpriteInfoJson>>();
        monitor.CurrentValue.Returns(emptyData);
        spriteInfo = new SpriteInfo(monitor, serverurl);
    }

    [Benchmark]
    public void SpriteInfo_FillData()
    {
        // spriteInfo.FillData(emptyData, serverurl);
    }
}
