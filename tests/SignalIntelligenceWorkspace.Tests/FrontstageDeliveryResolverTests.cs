using SignalIntelligenceWorkspace.Services.Frontstage;

namespace SignalIntelligenceWorkspace.Tests;

public sealed class FrontstageDeliveryResolverTests
{
    [Theory]
    [InlineData("zh", "zh")]
    [InlineData("zh-Hant", "zh")]
    [InlineData("en", "en")]
    [InlineData("fr", "en")]
    public void NormalizeLanguage_AllowsEnglishAndTraditionalChinese(string input, string expected)
    {
        Assert.Equal(expected, PostgresFrontstageDeliveryResolver.NormalizeLanguage(input));
    }

    [Fact]
    public void BuildRedactedPath_NeverIncludesRawToken()
    {
        Assert.Equal("/r/[redacted-token]?lang=en", PostgresFrontstageDeliveryResolver.BuildRedactedPath("en"));
        Assert.Equal("/r/[redacted-token]?lang=zh", PostgresFrontstageDeliveryResolver.BuildRedactedPath("zh"));
    }

    [Theory]
    [InlineData("https://www.linkedin.com/messaging/thread/abc", "www.linkedin.com")]
    [InlineData("not-a-url", null)]
    [InlineData("", null)]
    public void GetReferrerDomain_ReturnsOnlyPublicDomain(string referrer, string? expected)
    {
        Assert.Equal(expected, PostgresFrontstageDeliveryResolver.GetReferrerDomain(referrer));
    }

    [Theory]
    [InlineData("Mozilla/5.0 Edg/126.0", "edge")]
    [InlineData("Mozilla/5.0 Chrome/126.0 Safari/537.36", "chrome")]
    [InlineData("Mozilla/5.0 Firefox/127.0", "firefox")]
    [InlineData("Googlebot/2.1", "bot")]
    [InlineData("", "unknown")]
    public void GetUserAgentFamily_GroupsWithoutStoringRawAgent(string userAgent, string expected)
    {
        Assert.Equal(expected, PostgresFrontstageDeliveryResolver.GetUserAgentFamily(userAgent));
    }
}
