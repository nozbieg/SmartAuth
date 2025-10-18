namespace SmartAuth.Infrastructure.Commons;

public interface ICommandResult
{
    bool IsSuccess { get; }
    Error? Error { get; }
}

public interface ICommandResult<out T> : ICommandResult
{
    T? Value { get; }
}

public readonly struct CommandResult<T> : ICommandResult<T>
{
    public bool IsSuccess { get; }
    public T? Value { get; }
    public Error? Error { get; }

    private CommandResult(T value)
    {
        IsSuccess = true;
        Value = value;
        Error = null;
    }

    private CommandResult(Error error)
    {
        IsSuccess = false;
        Value = default;
        Error = error;
    }

    public static CommandResult<T> Ok(T value) => new(value);
    public static CommandResult<T> Fail(Error error) => new(error);
}

public readonly struct CommandResult : ICommandResult
{
    public bool IsSuccess { get; }
    public Error? Error { get; }

    private CommandResult(bool ok, Error? error) => (IsSuccess, Error) = (ok, error);

    public static CommandResult Ok() => new(true, null);
    public static CommandResult Fail(Error error) => new(false, error);
}