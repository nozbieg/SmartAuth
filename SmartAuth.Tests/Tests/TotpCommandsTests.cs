using System.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using SmartAuth.Api.Features.Auth;
using SmartAuth.Api.Services;
using SmartAuth.Api.Utilities;
using SmartAuth.Domain.Entities;
using SmartAuth.Infrastructure;
using SmartAuth.Infrastructure.Security;
using SmartAuth.Tests.Helpers;

namespace SmartAuth.Tests.Tests;

public sealed class TotpCommandsTests(PostgresContainerFixture fx) : IClassFixture<PostgresContainerFixture>
{
    private readonly string _cs = fx.ConnectionString;

    private async Task<(AuthDbContext db, IHttpContextAccessor accessor, IConfiguration cfg)> BuildContextAsync(string email)
    {
        var (accessor, sp, cfg) = TestSetup.BuildHttpContextWithUser(_cs, email, twoFaCodeEnabled: true);
        var db = await TestSetup.EnsureMigratedAsync(sp);
        await TestSetup.AddUserAsync(db, email);
        return (db, accessor, cfg);
    }

    [Fact]
    public async Task Totp_setup_enable_disable_flow_success()
    {
        var (db, accessor, _) = await BuildContextAsync("totpuser@example.com");
        var msClient = new MicrosoftAuthenticatorClient(Options.Create(new TotpOptions()));
        var setupHandler = new TwoFaTotpSetupCommandHandler(db, accessor, msClient,
            Options.Create(new TotpOptions()));
        var setupResult = await setupHandler.Handle(new TwoFaTotpSetupCommand(), CancellationToken.None);
        Assert.True(setupResult.IsSuccess);
        var setup = setupResult.Value!;
        Assert.NotEqual(Guid.Empty, setup.SetupId);
        Assert.False(string.IsNullOrWhiteSpace(setup.Secret));
        Assert.Contains("otpauth://totp", setup.OtpAuthUri);
        Assert.False(string.IsNullOrWhiteSpace(setup.QrImageBase64));

        var pending = await db.UserAuthenticators.FirstAsync(a => a.Id == setup.SetupId);
        Assert.False(pending.IsActive);

        var code = Totp.GenerateCode(pending.Secret);
        var enableHandler = new TwoFaTotpEnableCommandHandler(db, accessor);
        var enableResult =
            await enableHandler.Handle(new TwoFaTotpEnableCommand(pending.Id, code), CancellationToken.None);
        Assert.True(enableResult.IsSuccess);

        var active = await db.UserAuthenticators.FirstAsync(a => a.Id == pending.Id);
        Assert.True(active.IsActive);

        var statusHandler = new TwoFaTotpStatusQueryHandler(db, accessor);
        var status = await statusHandler.Handle(new TwoFaTotpStatusQuery(), CancellationToken.None);
        Assert.True(status.IsSuccess);
        Assert.NotNull(status.Value);
        Assert.True(status.Value!.Active);

        var disableHandler = new TwoFaTotpDisableCommandHandler(db, accessor);
        var disable = await disableHandler.Handle(new TwoFaTotpDisableCommand(), CancellationToken.None);
        Assert.True(disable.IsSuccess);
        Assert.False(await db.UserAuthenticators.AnyAsync(a => a.User!.Email == "totpuser@example.com"));

        var status2 = await statusHandler.Handle(new TwoFaTotpStatusQuery(), CancellationToken.None);
        Assert.True(status2.IsSuccess);
        Assert.NotNull(status2.Value);
        Assert.False(status2.Value!.Active);
    }

    [Fact]
    public async Task Totp_enable_fails_with_invalid_code()
    {
        var (db, accessor, _) = await BuildContextAsync("badcode@example.com");
        var msClient = new MicrosoftAuthenticatorClient(Options.Create(new TotpOptions()));
        var setupHandler = new TwoFaTotpSetupCommandHandler(db, accessor, msClient,
            Options.Create(new TotpOptions()));
        var setupResult = await setupHandler.Handle(new TwoFaTotpSetupCommand(), CancellationToken.None);
        Assert.True(setupResult.IsSuccess);
        var setup = setupResult.Value!;

        var enableHandler = new TwoFaTotpEnableCommandHandler(db, accessor);
        var badEnable =
            await enableHandler.Handle(new TwoFaTotpEnableCommand(setup.SetupId, "000000"), CancellationToken.None);
        Assert.False(badEnable.IsSuccess);
        Assert.Equal(HttpStatusCode.Unauthorized, badEnable.Error!.Status);
    }

    [Fact]
    public async Task Totp_setup_restart_flow_replaces_pending()
    {
        var (db, accessor, _) = await BuildContextAsync("restart@example.com");
        var msClient = new MicrosoftAuthenticatorClient(Options.Create(new TotpOptions()));
        var setupHandler = new TwoFaTotpSetupCommandHandler(db, accessor, msClient,
            Options.Create(new TotpOptions()));
        var first = await setupHandler.Handle(new TwoFaTotpSetupCommand(), CancellationToken.None);
        Assert.True(first.IsSuccess);
        var firstId = first.Value!.SetupId;
        var second = await setupHandler.Handle(new TwoFaTotpSetupCommand(true), CancellationToken.None);
        Assert.True(second.IsSuccess);
        Assert.NotEqual(firstId, second.Value!.SetupId);
        var countPending = await db.UserAuthenticators.CountAsync(a => a.Type == AuthenticatorType.Totp && !a.IsActive);
        Assert.Equal(1, countPending);
        Assert.False(string.IsNullOrWhiteSpace(second.Value!.QrImageBase64));
    }

    [Fact]
    public async Task Totp_setup_conflict_when_already_active_and_no_force_restart()
    {
        var (db, accessor, _) = await BuildContextAsync("conflict@example.com");
        var msClient = new MicrosoftAuthenticatorClient(Options.Create(new TotpOptions()));
        var setupHandler = new TwoFaTotpSetupCommandHandler(db, accessor, msClient,
            Options.Create(new TotpOptions()));
        var first = await setupHandler.Handle(new TwoFaTotpSetupCommand(), CancellationToken.None);
        Assert.True(first.IsSuccess);
        var pending = await db.UserAuthenticators.FirstAsync(a => a.Id == first.Value!.SetupId);
        var code = Totp.GenerateCode(pending.Secret);
        var enableHandler = new TwoFaTotpEnableCommandHandler(db, accessor);
        var enableRes = await enableHandler.Handle(new TwoFaTotpEnableCommand(pending.Id, code), CancellationToken.None);
        Assert.True(enableRes.IsSuccess);

        var second = await setupHandler.Handle(new TwoFaTotpSetupCommand(), CancellationToken.None);
        Assert.False(second.IsSuccess);
        Assert.Equal("common.conflict", second.Error!.Code);
    }
}