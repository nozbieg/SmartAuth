using SmartAuth.Api.Utilities;

namespace SmartAuth.Api.Endpoints.Filters;

public sealed class MediatorEndpointFilter(IServiceProvider sp) : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext ctx, EndpointFilterDelegate next)
    {
        var req = ctx.Arguments.FirstOrDefault(a =>
            a is not null &&
            a.GetType().GetInterfaces()
                .Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IRequest<>)));

        if (req is null)
            return await next(ctx);

        var mediator = sp.GetRequiredService<IMediator>();
        var response = await mediator.Send((dynamic)req, ctx.HttpContext.RequestAborted);

        if (response is ICommandResult cr)
            return cr.ToIResult();

        return Results.Json(response);
    }
}