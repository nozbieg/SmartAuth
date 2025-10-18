namespace SmartAuth.Api.Utilities;

public static class CommandResultHttpMapping
{
    public static IResult ToIResult(this ICommandResult result)
    {
        // Non-generic struct CommandResult
        if (result is CommandResult nongeneric)
            return nongeneric.ToIResult();

        var type = result.GetType();
        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(CommandResult<>))
        {
            return ToGenericResult((dynamic)result); // deleguje do generycznej wersji bez refleksji
        }

        // Fallback (nie powinno wystąpić w normalnym scenariuszu)
        if (result.IsSuccess)
            return Results.Ok();

        var e = result.Error!;
        var problem = new { e.Code, e.Message, e.Detail, e.Status, e.Metadata, e.TraceId };
        return Results.Json(problem, statusCode: (int)e.Status);
    }

    private static IResult ToGenericResult<T>(CommandResult<T> result) => result.ToIResult();

    public static IResult ToIResult<T>(this CommandResult<T> result)
    {
        if (result.IsSuccess)
            return Results.Ok(result.Value);

        var e = result.Error!;
        var problem = new
        {
            e.Code,
            e.Message,
            e.Detail,
            e.Status,
            e.Metadata,
            e.TraceId
        };

        return Results.Json(problem, statusCode: (int)e.Status);
    }
    
    public static IResult ToIResult(this CommandResult result)
    {
        if (result.IsSuccess)
            return Results.Ok(result);

        var e = result.Error!;
        var problem = new
        {
            e.Code,
            e.Message,
            e.Detail,
            e.Status,
            e.Metadata,
            e.TraceId
        };

        return Results.Json(problem, statusCode: (int)e.Status);
    }
}