using Microsoft.AspNetCore.Http;
using SignalIntelligenceWorkspace.Services.Security;

namespace SignalIntelligenceWorkspace.Tests;

public sealed class CanonicalHostRedirectMiddlewareTests
{
    [Fact]
    public void ShouldRedirect_ReturnsTrueForWwwHost()
    {
        Assert.True(CanonicalHostRedirectMiddleware.ShouldRedirect(new HostString("www.johannafan.com")));
    }

    [Fact]
    public void ShouldRedirect_ReturnsFalseForApexHost()
    {
        Assert.False(CanonicalHostRedirectMiddleware.ShouldRedirect(new HostString("johannafan.com")));
    }

    [Fact]
    public void BuildApexUrl_PreservesPathAndQuery()
    {
        var context = new DefaultHttpContext();
        context.Request.Scheme = "https";
        context.Request.Host = new HostString("www.johannafan.com");
        context.Request.Path = "/cockpit";
        context.Request.QueryString = new QueryString("?tab=signals");

        var url = CanonicalHostRedirectMiddleware.BuildApexUrl(context.Request);

        Assert.Equal("https://johannafan.com/cockpit?tab=signals", url);
    }

    [Fact]
    public async Task InvokeAsync_RedirectsWwwToApexBeforeNextMiddleware()
    {
        var nextCalled = false;
        var middleware = new CanonicalHostRedirectMiddleware(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });
        var context = new DefaultHttpContext();
        context.Request.Scheme = "https";
        context.Request.Host = new HostString("www.johannafan.com");
        context.Request.Path = "/";

        await middleware.InvokeAsync(context);

        Assert.False(nextCalled);
        Assert.Equal(StatusCodes.Status301MovedPermanently, context.Response.StatusCode);
        Assert.Equal("https://johannafan.com/", context.Response.Headers.Location);
    }

    [Fact]
    public async Task InvokeAsync_AllowsApexHostThrough()
    {
        var nextCalled = false;
        var middleware = new CanonicalHostRedirectMiddleware(context =>
        {
            nextCalled = true;
            context.Response.StatusCode = StatusCodes.Status204NoContent;
            return Task.CompletedTask;
        });
        var context = new DefaultHttpContext();
        context.Request.Scheme = "https";
        context.Request.Host = new HostString("johannafan.com");
        context.Request.Path = "/";

        await middleware.InvokeAsync(context);

        Assert.True(nextCalled);
        Assert.Equal(StatusCodes.Status204NoContent, context.Response.StatusCode);
    }
}
