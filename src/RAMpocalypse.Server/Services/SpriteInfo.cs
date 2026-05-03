using Microsoft.Extensions.Options;
using RAMpocalypse.Server.Game;

namespace RAMpocalypse.Server.Services;

public class SpriteInfoJson
{
    public List<SpriteData> Data { get; set; } = [];
}

public class SpriteInfo : ISpriteInfo
{
    private Dictionary<string, SpriteData> data = [];
    private readonly Dictionary<SpriteType, List<SpriteData>> cache = [];
    private readonly Dictionary<SpriteType, SpriteData> fallbacks =
        Enum.GetValues(typeof(SpriteType)).Cast<SpriteType>()
        .ToDictionary(type => type, type => new SpriteData($"Fallback{type}.png", type: type));
    private readonly Lock dataLock = new();

    public SpriteInfo(IOptionsMonitor<SpriteInfoJson> monitor, string serverUrl)
    {
        FillData(monitor.CurrentValue, serverUrl);
        monitor.OnChange(json =>
        {
            lock (dataLock)
                FillData(json, serverUrl);
        });
    }

    public bool TryGetValue(string key, out SpriteData sprite)
    {
        lock (dataLock) return data.TryGetValue(key, out sprite!);
    }

    public List<SpriteData> GetAllOfType(SpriteType type)
    {
        lock (dataLock) return cache[type];
    }

    private void FillData(SpriteInfoJson json, string serverUrl)
    {
        data = json.Data.ToDictionary(
            sprite => sprite.URL,
            sprite => { sprite.URL = serverUrl + sprite.URL; return sprite; }
        );
        foreach (var type in Enum.GetValues(typeof(SpriteType)).Cast<SpriteType>())
        {
            var filteredSprites = data.Values.Where(s => s.Type == type).ToList();
            cache[type] = filteredSprites;
            if (filteredSprites.Count == 0)
            {
                var fallback = fallbacks[type];
                filteredSprites.Add(fallback);
                data[fallback.URL] = fallback;
            }
        }
    }
}
