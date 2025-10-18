namespace SmartAuth.Infrastructure.Commons;

public interface IValidator<in TRequest>
{
    Task<Error?> Validate(TRequest request, CancellationToken ct);
}

public abstract class Validator<TRequest> : IValidator<TRequest>
{
    protected readonly Dictionary<string, object> Metadata = new();

    public virtual async Task<Error?> Validate(TRequest request, CancellationToken ct)
    {
        await ValidateParams(request);
        return Metadata.Count == 0
            ? null
            : Errors.Validation(GetMessageBasedOnMetadata(Metadata), Metadata);
    }

    protected abstract Task ValidateParams(TRequest request);

    private static string GetMessageBasedOnMetadata(Dictionary<string, object> metadata)
    {
        if (metadata.Count == 0) return string.Empty;
        if (metadata.Count == 1)
        {
            var entry = metadata.First();
            return $"Field '{entry.Key}' is invalid: {entry.Value}";
        }

        var keys = string.Join(" ", metadata.Keys);
        var values = string.Join("\n", metadata.Values.Select(v => v?.ToString()));
        return $"Fields '{keys}' are invalid.\n{values}";
    }
}