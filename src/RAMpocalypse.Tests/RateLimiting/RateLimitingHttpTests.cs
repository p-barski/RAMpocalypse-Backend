using System.Net;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR.Client;
using RAMpocalypse.Server.Hubs;
using Xunit;

namespace RAMpocalypse.Tests.RateLimiting;

public class RateLimitingHttpTests
{
    private readonly RateLimitingWebApplicationFactory factory = new();

    private static HttpRequestMessage CreateNegotiateRequest() =>
        new(HttpMethod.Post, "/gamehub/negotiate?negotiateVersion=1");

    [Fact]
    public async Task StaticAssets_ReturnTooManyRequestsAfterPermitIsExhausted()
    {
        var client = factory.CreateClient();
        using var firstResponse = await client.GetAsync("/assets/sprites/gg.png", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, firstResponse.StatusCode);

        using var secondResponse = await client.GetAsync("/assets/sprites/gg.png", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.TooManyRequests, secondResponse.StatusCode);
    }

    [Fact]
    public async Task HubNegotiate_ReturnsTooManyRequestsAfterPermitIsExhausted()
    {
        var client = factory.CreateClient();
        using var firstRequest = CreateNegotiateRequest();
        using var firstResponse = await client.SendAsync(firstRequest, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, firstResponse.StatusCode);

        using var secondRequest = CreateNegotiateRequest();
        using var secondResponse = await client.SendAsync(secondRequest, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.TooManyRequests, secondResponse.StatusCode);
    }

    [Fact]
    public async Task HubInvoke_GetPlayerId_ReturnsEmptyStringAfterControlPermitIsExhausted()
    {
        factory.HubConnectPermitLimit = 2;
        var hubUrl = new Uri(factory.Server.BaseAddress, "/gamehub");
        await using var connection = new HubConnectionBuilder()
            .WithUrl(hubUrl, options =>
            {
                options.HttpMessageHandlerFactory = _ => factory.Server.CreateHandler();
                options.Transports = HttpTransportType.WebSockets;
                options.WebSocketFactory = async (context, cancellationToken) =>
                    await factory.Server.CreateWebSocketClient().ConnectAsync(context.Uri, cancellationToken);
            })
            .Build();

        await connection.StartAsync(TestContext.Current.CancellationToken);

        var firstPlayerId = await connection.InvokeAsync<string>(
            nameof(GameHub.GetPlayerId),
            TestContext.Current.CancellationToken);

        Assert.False(string.IsNullOrEmpty(firstPlayerId));

        var secondPlayerId = await connection.InvokeAsync<string>(
            nameof(GameHub.GetPlayerId),
            TestContext.Current.CancellationToken);

        Assert.Equal(string.Empty, secondPlayerId);
    }
}
