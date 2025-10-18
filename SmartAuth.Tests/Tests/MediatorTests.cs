using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SmartAuth.Infrastructure.Commons;

namespace SmartAuth.Tests.Tests;

public class MediatorTests
{
    private static IMediator BuildMediator()
    {
        var services = new ServiceCollection();
        services.AddLogging(b => b.AddDebug().AddConsole());
        services.AddScoped<IMediator, Mediator>();
        services.AddScoped<IRequestHandler<DummyReq, CommandResult<string>>, DummyHandler>();
        services.AddScoped<IValidator<DummyReq>, DummyValidator>();
        services.AddScoped<IRequestHandler<DummyVoidReq, CommandResult>, DummyVoidHandler>();
        services.AddScoped<IValidator<DummyVoidReq>, DummyVoidValidator>();
        var provider = services.BuildServiceProvider();
        return provider.GetRequiredService<IMediator>();
    }

    [Fact]
    public async Task Mediator_Returns_Fail_CommandResult_On_Validation_Error()
    {
        var mediator = BuildMediator();
        var res = await mediator.Send<CommandResult<string>>(new DummyReq(null!));
        Assert.False(res.IsSuccess);
        Assert.NotNull(res.Error);
        Assert.Equal("validation.error", res.Error!.Code);
    }

    [Fact]
    public async Task Mediator_Returns_Success_On_Valid_Request()
    {
        var mediator = BuildMediator();
        var res = await mediator.Send<CommandResult<string>>(new DummyReq("abc"));
        Assert.True(res.IsSuccess);
        Assert.Null(res.Error);
    }

    [Fact]
    public async Task Mediator_Void_CommandResult_Fail_On_Validation()
    {
        var mediator = BuildMediator();
        var res = await mediator.Send<CommandResult>(new DummyVoidReq(0));
        Assert.False(res.IsSuccess);
        Assert.NotNull(res.Error);
        Assert.Equal("validation.error", res.Error!.Code);
    }

    [Fact]
    public async Task Mediator_Void_CommandResult_Success()
    {
        var mediator = BuildMediator();
        var res = await mediator.Send<CommandResult>(new DummyVoidReq(5));
        Assert.True(res.IsSuccess);
        Assert.Null(res.Error);
    }

    private record DummyReq(string? Value) : IRequest<CommandResult<string>>;
    private record DummyVoidReq(int Number) : IRequest<CommandResult>;

    private class DummyValidator : Validator<DummyReq>
    {
        protected override Task ValidateParams(DummyReq request)
        {
            if (string.IsNullOrWhiteSpace(request.Value)) Metadata.Add(nameof(request.Value), "Value is required");
            return Task.CompletedTask;
        }
    }

    private class DummyVoidValidator : Validator<DummyVoidReq>
    {
        protected override Task ValidateParams(DummyVoidReq request)
        {
            if (request.Number <= 0) Metadata.Add(nameof(request.Number), "Must be positive");
            return Task.CompletedTask;
        }
    }

    private class DummyHandler : IRequestHandler<DummyReq, CommandResult<string>>
    {
        public Task<CommandResult<string>> Handle(DummyReq req, CancellationToken ct)
        {
            return Task.FromResult(CommandResult<string>.Ok(req.Value!.ToUpperInvariant()));
        }
    }

    private class DummyVoidHandler : IRequestHandler<DummyVoidReq, CommandResult>
    {
        public Task<CommandResult> Handle(DummyVoidReq req, CancellationToken ct)
        {
            return Task.FromResult(CommandResult.Ok());
        }
    }
}
