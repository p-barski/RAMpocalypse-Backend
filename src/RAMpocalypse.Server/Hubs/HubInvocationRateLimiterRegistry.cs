using System.Collections.Concurrent;
using System.Threading.RateLimiting;

namespace RAMpocalypse.Server.Hubs;

public sealed class HubInvocationRateLimiterRegistry(HubMethodRateLimiterFactory rateLimiterFactory) : IDisposable
{
    private readonly HubMethodRateLimiterFactory rateLimiterFactory = rateLimiterFactory;
    private readonly ConcurrentDictionary<string, MethodRateLimiterRegistry> connectionToMethodRegistry = new();

    public RateLimiter GetLimiter(string connectionId, string methodName) =>
        connectionToMethodRegistry.GetOrAdd(connectionId, _ => new MethodRateLimiterRegistry())
            .GetOrAdd(methodName, () => rateLimiterFactory.Create(methodName));

    public void RemoveConnection(string connectionId)
    {
        if (connectionToMethodRegistry.TryRemove(connectionId, out var registry))
            registry.Dispose();
    }

    public void Dispose()
    {
        foreach (var connectionId in connectionToMethodRegistry.Keys)
            RemoveConnection(connectionId);
    }

    private sealed class MethodRateLimiterRegistry : IDisposable
    {
        private readonly ConcurrentDictionary<string, RateLimiter> methodNameToLimiter = new();

        public RateLimiter GetOrAdd(string methodName, Func<RateLimiter> factory) =>
            methodNameToLimiter.GetOrAdd(methodName, _ => factory());

        public void Dispose()
        {
            foreach (var limiter in methodNameToLimiter.Values)
                limiter.Dispose();
        }
    }
}
