using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Options;
using RAMpocalypse.Server.Configuration;
using RAMpocalypse.Server.Database;
using RAMpocalypse.Server.Extensions;
using RAMpocalypse.Server.Hubs;
using RAMpocalypse.Server.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration
.AddJsonFile("gameconfig.json", optional: false, reloadOnChange: true)
.AddJsonFile("assetinfo.json", optional: false, reloadOnChange: true);

var serverConfig = ServerConfig.BindRequired(builder.Configuration);

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins(serverConfig.FrontendOrigin)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
}).AddCustomRateLimiting(builder.Configuration)
  .Configure<GameConfig>(builder.Configuration.GetSection("GameConfig"))
  .Configure<SpriteInfoJson>(builder.Configuration.GetSection("SpriteInfo"))
  // as env vars: MongoDB__ConnectionString, MongoDB__DatabaseName
  // as dotnet user secrets: dotnet user-secrets set "MongoDB:ConnectionString" "value" etc
  .Configure<MongoDbConfig>(builder.Configuration.GetSection("MongoDB"))
  // as env vars: Server__FrontendOrigin, Server__SpriteBaseUrl
  .AddSingleton(Options.Create(serverConfig))
  .AddSingleton(TimeProvider.System)
  .AddSingleton<ISpriteInfo>(sp =>
  {
      var jsonMonitor = sp.GetRequiredService<IOptionsMonitor<SpriteInfoJson>>();
      return new SpriteInfo(jsonMonitor, serverConfig.SpriteBaseUrl);
  })
  .AddSingleton<IDatabase>(sp =>
  {
      var logger = sp.GetRequiredService<ILogger<MongoDb>>();
      try
      {
          var db = new MongoDb(sp.GetRequiredService<IOptions<MongoDbConfig>>().Value, logger);
          _ = db.FillGlobalChatHistoryCache();
          return db;
      }
      catch (Exception e)
      {
          logger.LogError("Could not establish connection to monogodb: {Exception}", e);
      }
      return new DummyDb();
  })
  .AddSingleton<IChatCooldowns, ChatCooldowns>()
  .AddSingleton<IGameConfig, ReloadableGameConfig>()
  .AddSingleton<IPlayerConnectionService, PlayerConnectionService>()
  .AddSingleton<ILobbyManager, LobbyManager>()
  .AddSingleton<IMatchmakingService, MatchmakingService>()
  .AddSingleton<IGameService, GameService>()
  .AddSingleton<IPlayerFactory, PlayerFactory>()
  .AddSingleton<ILongLivedAttacksCleaner, LongLivedAttacksCleaner>()
  .AddHostedService<LongLivedAttacksCleanupService>()
  .AddSingleton<HubMethodRateLimiterFactory>()
  .AddSingleton<HubInvocationRateLimiterRegistry>()
  .AddSignalR()
  .AddHubOptions<GameHub>(options => options.AddFilter(typeof(RateLimitingHubFilter)));

builder.Services.AddControllers();
var app = builder.Build();

app.UseCors()
   .UseStaticAssetsResponseHeaders(serverConfig.FrontendOrigin)
   .UseRateLimiter();

app.MapFallbackToFile("/app/index.html");
app.MapHub<GameHub>("/gamehub").RequireRateLimiting("hubConnect");
app.MapStaticAssets().RequireRateLimiting("staticAssets");
app.MapControllers().RequireRateLimiting("api");

app.Run();
