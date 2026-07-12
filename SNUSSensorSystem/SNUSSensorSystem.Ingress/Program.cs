using SNUSSensorSystem.Ingress.Configuration;
using SNUSSensorSystem.Ingress.Middleware;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(options =>
{
    options.AddPolicy(
        "IngressCors",
        policy =>
        {
            policy
                .AllowAnyHeader()
                .AllowAnyMethod()
                .SetIsOriginAllowed(_ => true)
                .AllowCredentials();
        });
});

builder.Services.AddHealthChecks();

builder.Services.AddSnusReverseProxy(
    builder.Configuration);

var app = builder.Build();

app.UseMiddleware<RequestLoggingMiddleware>();

app.UseMiddleware<SecurityHeadersMiddleware>();

app.UseCors("IngressCors");

app.MapGet(
    "/",
    () => Results.Ok(new
    {
        service = "SNUS Ingress",

        routes = new[]
        {
            "/api/ingest",
            "/api/reports",
            "/api/notify",
            "/hub"
        }
    }));

app.MapHealthChecks("/health");

app.MapReverseProxy();

app.Run();