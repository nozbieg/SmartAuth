using System.Reflection;
using SmartAuth.Api.Utilities;

namespace SmartAuth.Api.Features;

public sealed class MediatorEndpointFilter : IEndpointFilter
{
    private readonly IServiceProvider _sp;

    public MediatorEndpointFilter(IServiceProvider sp) => _sp = sp;

    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext ctx, EndpointFilterDelegate next)
    {
        var req = ctx.Arguments.FirstOrDefault(a =>
            a is not null &&
            a.GetType().GetInterfaces()
                .Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IRequest<>)));

        if (req is null)
            return await next(ctx);

        var mediator = _sp.GetRequiredService<IMediator>();
        var response = await mediator.Send((dynamic)req, ctx.HttpContext.RequestAborted);

        if (response is CommandResult rNoGeneric)
            return rNoGeneric.ToIResult();

        var t = response?.GetType();
        if (t?.IsGenericType != true || t?.GetGenericTypeDefinition() != typeof(CommandResult<>))
            return Results.Json(response);

        var toIResult = typeof(CommandResultHttpMapping).GetMethods(BindingFlags.Public | BindingFlags.Static)
            .First(m => m.Name == nameof(CommandResultHttpMapping.ToIResult) && m.GetParameters() is [not null])
            .MakeGenericMethod(t?.GetGenericArguments()[0]);

        return toIResult.Invoke(null, new object?[] { response, ctx.HttpContext });
    }
}