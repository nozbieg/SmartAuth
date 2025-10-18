namespace SmartAuth.Api.Utilities;

public static class CommandResultHttpMapping
{
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