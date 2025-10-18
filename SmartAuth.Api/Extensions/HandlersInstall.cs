namespace SmartAuth.Api.Extensions;

public static class HandlersInstall
{
    public static IServiceCollection AddHandlers(this IServiceCollection services)
    {
        var appAssembly = typeof(Program).Assembly;

        foreach (var type in appAssembly.GetTypes())
        {
            var handlerInterfaces = type.GetInterfaces()
                .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IRequestHandler<,>));

            foreach (var iface in handlerInterfaces)
                services.AddScoped(iface, type);
        }

        return services;
    }
}