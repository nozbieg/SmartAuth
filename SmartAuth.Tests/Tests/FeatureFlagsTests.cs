using Microsoft.Extensions.Configuration;
using SmartAuth.Api.Contracts;
using SmartAuth.Tests.Helpers;

namespace SmartAuth.Tests.Tests;

public class FeatureFlagsTests
{
    [Fact]
    public void Feature_flags_bind_from_configuration_true()
    {
        var cfg = TestSetup.BuildConfig(twoFaCodeEnabled: true);
        var flags = cfg.GetSection("FeatureFlags").Get<FeatureFlags>();
        Assert.NotNull(flags);
        Assert.True(flags!.twofa_code);
    }

    [Fact]
    public void Feature_flags_bind_from_configuration_false()
    {
        var cfg = TestSetup.BuildConfig(twoFaCodeEnabled: false);
        var flags = cfg.GetSection("FeatureFlags").Get<FeatureFlags>();
        Assert.NotNull(flags);
        Assert.False(flags!.twofa_code);
    }
}
