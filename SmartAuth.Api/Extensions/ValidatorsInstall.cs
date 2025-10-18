namespace SmartAuth.Api.Extensions;

public static class ValidatorsInstall
{
    public static IServiceCollection AddValidators(this IServiceCollection services)
    {
        var appAssembly = typeof(Program).Assembly;

        foreach (var type in appAssembly.GetTypes())
        {
            var validatorInterfaces = type.GetInterfaces()
                .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IValidator<>));

            foreach (var iface in validatorInterfaces)
                services.AddScoped(iface, type);
        }

        return services;
    }
}