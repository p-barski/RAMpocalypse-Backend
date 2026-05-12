using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace RAMpocalypse.Tests.RateLimiting;

public sealed class RateLimitingWebApplicationFactory : WebApplicationFactory<Program>
{
    public int HubConnectPermitLimit { get; set; } = 1;
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment(Environments.Production);
        builder.ConfigureAppConfiguration((_, configurationBuilder) =>
        {
            configurationBuilder.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["RateLimiting:HubConnect:PermitLimit"] = HubConnectPermitLimit.ToString(),
                ["RateLimiting:HubConnect:WindowSeconds"] = "60",
                ["RateLimiting:HubConnect:QueueLimit"] = "0",
                ["RateLimiting:StaticAssets:PermitLimit"] = "1",
                ["RateLimiting:StaticAssets:WindowSeconds"] = "60",
                ["RateLimiting:StaticAssets:QueueLimit"] = "0",
                ["RateLimiting:HubInvoke:Control:PermitLimit"] = "1",
                ["RateLimiting:HubInvoke:Control:WindowSeconds"] = "60",
                ["RateLimiting:HubInvoke:Control:QueueLimit"] = "0",
            });
        });
    }
}
