using RAMpocalypse.Server.Hubs;
using RAMpocalypse.Server.Services;

var builder = WebApplication.CreateBuilder(args);
var origin = "http://localhost:5173";

builder.Configuration.AddJsonFile("gameconfig.json", optional: false, reloadOnChange: true);
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins(origin)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
}).Configure<GameConfig>(builder.Configuration.GetSection("GameConfig"))
  .AddSingleton<IGameConfig, ReloadableGameConfig>()
  .AddSingleton<IPlayerConnectionService, PlayerConnectionService>()
  .AddSingleton<ILobbyManager, LobbyManager>()
  .AddSingleton<IMatchmakingService, MatchmakingService>()
  .AddSingleton<IGameService, GameService>()
  .AddSingleton<IPlayerFactory, PlayerFactory>()
  .AddSignalR();

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
}).UseCors();

app.MapGet("/", () => "RAMpocalypse Server is running!");
app.MapHub<GameHub>("/gamehub");

app.Run();
