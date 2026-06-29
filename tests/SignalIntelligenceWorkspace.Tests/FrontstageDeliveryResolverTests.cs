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
    [InlineData("/", "en", "/?lang=en")]
    [InlineData("/home", "zh-Hant", "/home?lang=zh")]
    [InlineData("/unexpected", "fr", "/?lang=en")]
    public void BuildAnonymousPath_StoresOnlyCanonicalPublicPath(
        string pagePath,
        string language,
        string expected)
    {
        Assert.Equal(expected, PostgresFrontstageDeliveryResolver.BuildAnonymousPath(pagePath, language));
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

    [Theory]
    [InlineData("hero", true, "hero")]
    [InlineData("marketing-fit", true, "marketing-fit")]
    [InlineData("Conversation", true, "conversation")]
    [InlineData("raw-token", false, "")]
    [InlineData("", false, "")]
    public void TryNormalizeSectionKey_AllowsOnlyPublicPageSections(
        string input,
        bool expectedResult,
        string expectedSectionKey)
    {
        var result = PostgresFrontstageDeliveryResolver.TryNormalizeSectionKey(input, out var sectionKey);

        Assert.Equal(expectedResult, result);
        Assert.Equal(expectedSectionKey, sectionKey);
    }

    [Theory]
    [InlineData("linkedin", true, "linkedin")]
    [InlineData("feedback-submit", true, "feedback-submit")]
    [InlineData("Email", true, "email")]
    [InlineData("raw token", false, "")]
    [InlineData("../../private", false, "")]
    [InlineData("", false, "")]
    public void TryNormalizeClickEventKey_AllowsOnlyCompactPublicEventKeys(
        string input,
        bool expectedResult,
        string expectedEventKey)
    {
        var result = PostgresFrontstageDeliveryResolver.TryNormalizeClickEventKey(input, out var eventKey);

        Assert.Equal(expectedResult, result);
        Assert.Equal(expectedEventKey, eventKey);
    }
}
