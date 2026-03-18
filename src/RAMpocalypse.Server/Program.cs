using RAMpocalypse.Server.Hubs;
using RAMpocalypse.Server.Services;

var builder = WebApplication.CreateBuilder(args);
var origin = "http://localhost:5173";

builder.Services.AddSignalR();
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins(origin)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

builder.Services.AddSingleton<IGameConfig, GameConfig>();
builder.Services.AddSingleton<IPlayerConnectionService, PlayerConnectionService>();
builder.Services.AddSingleton<ILobbyManager, LobbyManager>();
builder.Services.AddSingleton<IMatchmakingService, MatchmakingService>();
builder.Services.AddSingleton<IGameService, GameService>();
builder.Services.AddSingleton<IPlayerFactory, PlayerFactory>();

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
});
app.UseCors();
app.MapGet("/", () => "RAMpocalypse Server is running!");
app.MapHub<GameHub>("/gamehub");

app.Run();
