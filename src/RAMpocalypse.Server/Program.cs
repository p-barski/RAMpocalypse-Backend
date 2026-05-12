using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Options;
using RAMpocalypse.Server.Database;
using RAMpocalypse.Server.Extensions;
using RAMpocalypse.Server.Hubs;
using RAMpocalypse.Server.Services;

var builder = WebApplication.CreateBuilder(args);
var origin = "http://localhost:5173";

builder.Configuration
.AddJsonFile("gameconfig.json", optional: false, reloadOnChange: true)
.AddJsonFile("assetinfo.json", optional: false, reloadOnChange: true);
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins(origin)
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
  .AddSingleton(TimeProvider.System)
  .AddSingleton<ISpriteInfo>(sp =>
  {
      var jsonMonitor = sp.GetRequiredService<IOptionsMonitor<SpriteInfoJson>>();
      return new SpriteInfo(jsonMonitor, "http://localhost:5027/assets/sprites/");
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
   .UseStaticAssetsResponseHeaders(origin)
   .UseRateLimiter();

app.MapGet("/", () => "RAMpocalypse Server is running!");
app.MapHub<GameHub>("/gamehub").RequireRateLimiting("hubConnect");
app.MapStaticAssets().RequireRateLimiting("staticAssets");
app.MapControllers().RequireRateLimiting("api");

app.Run();
