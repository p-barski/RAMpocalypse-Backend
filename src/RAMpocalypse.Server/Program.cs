using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;
using RAMpocalypse.Server.Database;
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
}).AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("api", opt =>
    {
        opt.PermitLimit = 10;
        opt.Window = TimeSpan.FromSeconds(30);
        opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        opt.QueueLimit = 2;
    });
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
}).Configure<GameConfig>(builder.Configuration.GetSection("GameConfig"))
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
  .AddSignalR();

builder.Services.AddControllers();
var app = builder.Build();

app.UseStaticFiles(new StaticFileOptions
{
    OnPrepareResponse = ctx =>
    {
        ctx.Context.Response.Headers.Append("Cache-Control", "no-cache, no-store, must-revalidate");
        ctx.Context.Response.Headers.Append("Pragma", "no-cache");
        ctx.Context.Response.Headers.Append("Expires", "0");
        ctx.Context.Response.Headers.Append("Access-Control-Allow-Origin", origin);
    }
}).UseCors().UseRateLimiter();

app.MapGet("/", () => "RAMpocalypse Server is running!");
app.MapHub<GameHub>("/gamehub");
app.MapControllers().RequireRateLimiting("api");

app.Run();
