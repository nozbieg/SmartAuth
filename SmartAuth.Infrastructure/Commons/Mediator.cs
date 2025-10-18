using Microsoft.Extensions.Logging;
using SmartAuth.Infrastructure.Tracing;
using System.Collections.Concurrent;
using System.Reflection;

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
    private static readonly ConcurrentDictionary<Type, MethodInfo> ValidatorInvokerCache = new();
    private static readonly ConcurrentDictionary<(Type, Type), MethodInfo> HandlerInvokerCache = new();

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
        var requestType = request.GetType();
        var handlerType = typeof(IRequestHandler<,>)
            .MakeGenericType(requestType, typeof(TResponse));

        var handler = provider.GetService(handlerType)
                      ?? throw new InvalidOperationException($"No handler for {requestType}");

        var key = (requestType, typeof(TResponse));
        var invoker = HandlerInvokerCache.GetOrAdd(key, static k =>
            typeof(Mediator).GetMethod(nameof(InvokeHandlerTyped), BindingFlags.NonPublic | BindingFlags.Static)!
                .MakeGenericMethod(k.Item1, k.Item2));

        var task = (Task<TResponse>)invoker.Invoke(null, new[] { handler, request!, ct })!;
        return await task.ConfigureAwait(false);
    }

    private async Task<Error?> ValidateRequest<TResponse>(IRequest<TResponse> request, CancellationToken ct)
    {
        var requestType = request.GetType();
        var vInterface = typeof(IValidator<>).MakeGenericType(requestType);
        var enumerableType = typeof(IEnumerable<>).MakeGenericType(vInterface);
        var validatorsObj = provider.GetService(enumerableType) as IEnumerable<object>;
        if (validatorsObj is null) return null;

        var invoker = ValidatorInvokerCache.GetOrAdd(requestType, static t =>
            typeof(Mediator).GetMethod(nameof(InvokeValidatorTyped), BindingFlags.NonPublic | BindingFlags.Static)!
                .MakeGenericMethod(t));

        foreach (var v in validatorsObj)
        {
            var task = (Task<Error?>)invoker.Invoke(null, new[] { v, request!, ct })!;
            var err = await task.ConfigureAwait(false);
            if (err is not null) return err;
        }

        return null;
    }

    private static Task<Error?> InvokeValidatorTyped<TReq>(object validator, object request, CancellationToken ct)
        => ((IValidator<TReq>)validator).Validate((TReq)request, ct);

    private static Task<TResp> InvokeHandlerTyped<TReq, TResp>(object handlerObj, object requestObj, CancellationToken ct)
        where TReq : IRequest<TResp>
        => ((IRequestHandler<TReq, TResp>)handlerObj).Handle((TReq)requestObj, ct);
}

public interface IMediator
{
    Task<TResponse> Send<TResponse>(IRequest<TResponse> req, CancellationToken ct = default);
}