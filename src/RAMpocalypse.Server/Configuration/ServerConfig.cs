using Microsoft.Extensions.Configuration;

namespace RAMpocalypse.Server.Configuration;

public sealed class ServerConfig
{
    public const string SectionName = "Server";

    public string FrontendOrigin { get; set; } = "";
    public string SpriteBaseUrl { get; set; } = "";

    public static ServerConfig BindRequired(IConfiguration configuration) =>
        configuration.GetSection(SectionName).Get<ServerConfig>()
        ?? throw new InvalidOperationException($"Missing '{SectionName}' configuration section.");
}
