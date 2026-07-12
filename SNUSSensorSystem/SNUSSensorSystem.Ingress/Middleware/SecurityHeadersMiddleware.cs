namespace SNUSSensorSystem.Ingress.Middleware;

public sealed class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;

    public SecurityHeadersMiddleware(
        RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(
        HttpContext context)
    {
        context.Response.OnStarting(() =>
        {
            context.Response.Headers.TryAdd(
                "X-Content-Type-Options",
                "nosniff");

            context.Response.Headers.TryAdd(
                "X-Frame-Options",
                "DENY");

            context.Response.Headers.TryAdd(
                "Referrer-Policy",
                "no-referrer");

            context.Response.Headers.TryAdd(
                "Permissions-Policy",
                "camera=(), microphone=(), geolocation=()");

            context.Response.Headers.TryAdd(
                "Content-Security-Policy",
                "default-src 'none'; frame-ancestors 'none'");

            return Task.CompletedTask;
        });

        await _next(context);
    }
}