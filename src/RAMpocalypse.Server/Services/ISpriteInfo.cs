using RAMpocalypse.Server.Game;

namespace RAMpocalypse.Server.Services;

public interface ISpriteInfo
{
    bool TryGetValue(string key, out SpriteData sprite);
    List<SpriteData> GetAllOfType(SpriteType type);
}
