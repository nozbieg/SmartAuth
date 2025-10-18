using System.Reflection;

namespace SmartAuth.Infrastructure.Commons;

internal static class CommandResultFactory
{
    public static TResponse Fail<TResponse>(Error error)
    {
        var t = typeof(TResponse);

        if (t == typeof(CommandResult))
            return (TResponse)(object)CommandResult.Fail(error);

        if (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(CommandResult<>))
        {
            var genericArg = t.GetGenericArguments()[0];
            var method = typeof(CommandResult)
                .GetMethod(nameof(FailGeneric), BindingFlags.Static | BindingFlags.NonPublic)!
                .MakeGenericMethod(genericArg);

            return (TResponse)method.Invoke(null, new object[] { error })!;
        }

        throw new InvalidOperationException($"Unsupported response type {t.FullName}");
    }

    private static CommandResult<T> FailGeneric<T>(Error error) => CommandResult<T>.Fail(error);
}