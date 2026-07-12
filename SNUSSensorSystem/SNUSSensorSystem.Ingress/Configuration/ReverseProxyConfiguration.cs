namespace SNUSSensorSystem.Ingress.Configuration;

public static class ReverseProxyConfiguration
{
    public static IServiceCollection AddSnusReverseProxy(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services
            .AddReverseProxy()
            .LoadFromConfig(
                configuration.GetSection("ReverseProxy"));

        return services;
    }
}