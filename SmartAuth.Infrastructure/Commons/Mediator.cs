using Microsoft.Extensions.Logging;
using SmartAuth.Infrastructure.Tracing;

namespace SmartAuth.Infrastructure.Commons;

public interface IRequest<TResponse>
{
}

public interface IRequestHandler<in TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    Task<TResponse> Handle(TRequest req, CancellationToken ct);
}

public class Mediator(IServiceProvider provider, ILogger<Mediator> logger) : IMediator
{
    public async Task<TResponse> Send<TResponse>(IRequest<TResponse> req, CancellationToken ct = default)
    {
        try
        {
            var err = await ValidateRequest(req, ct);
            if (err is not null) return CommandResultFactory.Fail<TResponse>(err);

            return await HandleRequestAsync(req, ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unhandled exception in mediator for {RequestType}", req.GetType().Name);

            var traceId = OtelTrace.CurrentTraceId();
            var error = Errors.Internal(detail: ex.Message);
            error.Metadata!["traceId"] = traceId;
            error.Metadata!["exception"] = ex.GetType().FullName ?? "UnknownException";
            error.Metadata!["stackTrace"] = ex.StackTrace ?? "";

            return CommandResultFactory.Fail<TResponse>(error);
        }
    }

    private async Task<TResponse> HandleRequestAsync<TResponse>(IRequest<TResponse> request, CancellationToken ct)
    {
        var handlerType = typeof(IRequestHandler<,>)
            .MakeGenericType(request.GetType(), typeof(TResponse));

        dynamic handler = provider.GetService(handlerType)
                          ?? throw new InvalidOperationException($"No handler for {request.GetType()}");

        return await handler.Handle((dynamic)request, ct);
    }

    private async Task<Error?> ValidateRequest<TResponse>(IRequest<TResponse> request, CancellationToken ct)
    {
        var vInterface = typeof(IValidator<>).MakeGenericType(request.GetType());
        var enumerableType = typeof(IEnumerable<>).MakeGenericType(vInterface);
        var validators = (IEnumerable<object>?)provider.GetService(enumerableType);
        if (validators is null) return null;
        foreach (dynamic validator in validators)
        {
            Error? err = await validator.Validate((dynamic)request, ct);
            if (err is not null)
            {
                return err;
            }
        }

        return null;
    }
}

public interface IMediator
{
    Task<TResponse> Send<TResponse>(IRequest<TResponse> req, CancellationToken ct = default);
}