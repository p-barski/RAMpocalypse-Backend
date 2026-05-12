namespace RAMpocalypse.Server.Extensions;

public static class StaticAssetsResponseHeadersExtensions
{
    public static IApplicationBuilder UseStaticAssetsResponseHeaders(
        this IApplicationBuilder app,
        string allowedOrigin)
    {
        return app.Use((context, next) =>
        {
            if (context.Request.Path.StartsWithSegments("/assets", StringComparison.OrdinalIgnoreCase))
            {
                context.Response.OnStarting(() =>
                {
                    context.Response.Headers.CacheControl = "no-cache, no-store, must-revalidate";
                    context.Response.Headers.Pragma = "no-cache";
                    context.Response.Headers.Expires = "0";
                    context.Response.Headers.AccessControlAllowOrigin = allowedOrigin;
                    return Task.CompletedTask;
                });
            }
            return next(context);
        });
    }
}
