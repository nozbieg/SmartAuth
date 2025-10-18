using SmartAuth.Api.Utilities;

namespace SmartAuth.Api.Endpoints.Filters;

public sealed class MediatorEndpointFilter(IServiceProvider sp) : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext ctx, EndpointFilterDelegate next)
    {
        var reqArg = ctx.Arguments.FirstOrDefault(a =>
            a is not null && a.GetType().GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IRequest<>)));

        if (reqArg is not null)
            return await Dispatch(reqArg, ctx.HttpContext.RequestAborted);

        var result = await next(ctx);

        if (result is not null && result.GetType().GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IRequest<>)))
            return await Dispatch(result, ctx.HttpContext.RequestAborted);

        return result;
    }

    private async Task<object?> Dispatch(object request, CancellationToken ct)
    {
        var mediator = sp.GetRequiredService<IMediator>();
        var response = await mediator.Send((dynamic)request, ct);
        if (response is ICommandResult cr)
            return cr.ToIResult();

        return Results.Json(response);
    }
}