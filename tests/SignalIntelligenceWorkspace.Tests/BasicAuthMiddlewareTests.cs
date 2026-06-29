using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using SignalIntelligenceWorkspace.Services.Security;

namespace SignalIntelligenceWorkspace.Tests;

public sealed class BasicAuthMiddlewareTests
{
    [Theory]
    [InlineData("/")]
    [InlineData("/home")]
    [InlineData("/r/ft_20260629_credit-one_test")]
    [InlineData("/api/frontstage/section-view")]
    [InlineData("/api/frontstage/click")]
    [InlineData("/_framework/blazor.web.js")]
    [InlineData("/_content/Telerik.UI.for.Blazor/js/telerik-blazor.js")]
    [InlineData("/_blazor")]
    [InlineData("/favicon.png")]
    [InlineData("/app.css")]
    [InlineData("/app.vwzsywn7n4.css")]
    [InlineData("/SignalIntelligenceWorkspace.styles.css")]
    [InlineData("/SignalIntelligenceWorkspace.13lg14ogy8.styles.css")]
    [InlineData("/SignalIntelligenceWorkspace.styles.abc123.js")]
    public async Task InvokeAsync_AllowsPublicAndAssetPathsWithoutCredentials(string path)
    {
        var context = CreateContext(path);
        var middleware = CreateMiddleware(configured: false);

        await middleware.InvokeAsync(context);

        Assert.Equal(StatusCodes.Status204NoContent, context.Response.StatusCode);
    }

    [Theory]
    [InlineData("/")]
    [InlineData("/home")]
    public async Task InvokeAsync_AllowsPublicHeadRequestsWithoutCredentials(string path)
    {
        var context = CreateContext(path, HttpMethods.Head);
        var middleware = CreateMiddleware(configured: false);

        await middleware.InvokeAsync(context);

        Assert.Equal(StatusCodes.Status204NoContent, context.Response.StatusCode);
    }

    [Theory]
    [InlineData("/cockpit")]
    [InlineData("/hubspot")]
    [InlineData("/r")]
    [InlineData("/governance")]
    [InlineData("/application-intelligence")]
    public async Task InvokeAsync_RejectsPrivateRoutesWithoutCredentials(string path)
    {
        var context = CreateContext(path);
        var middleware = CreateMiddleware();

        await middleware.InvokeAsync(context);

        Assert.Equal(StatusCodes.Status401Unauthorized, context.Response.StatusCode);
        Assert.Equal("Basic realm=\"Signal Intelligence Workspace\", charset=\"UTF-8\"", context.Response.Headers.WWWAuthenticate);
    }

    [Theory]
    [InlineData("/cockpit")]
    [InlineData("/hubspot")]
    public async Task InvokeAsync_RejectsPrivateHeadRequestsWithoutCredentials(string path)
    {
        var context = CreateContext(path, HttpMethods.Head);
        var middleware = CreateMiddleware();

        await middleware.InvokeAsync(context);

        Assert.Equal(StatusCodes.Status401Unauthorized, context.Response.StatusCode);
        Assert.Equal("Basic realm=\"Signal Intelligence Workspace\", charset=\"UTF-8\"", context.Response.Headers.WWWAuthenticate);
    }

    [Theory]
    [InlineData("/cockpit")]
    [InlineData("/hubspot")]
    [InlineData("/governance")]
    [InlineData("/application-intelligence")]
    public async Task InvokeAsync_AllowsPrivateRoutesWithValidCredentials(string path)
    {
        var context = CreateContext(path);
        context.Request.Headers.Authorization = BuildHeader("dashboard", "secret-password");
        var middleware = CreateMiddleware();

        await middleware.InvokeAsync(context);

        Assert.Equal(StatusCodes.Status204NoContent, context.Response.StatusCode);
    }

    [Fact]
    public async Task InvokeAsync_PrivateRouteReturnsServiceUnavailableWhenBasicAuthIsMissing()
    {
        var context = CreateContext("/cockpit");
        var middleware = CreateMiddleware(configured: false);

        await middleware.InvokeAsync(context);

        Assert.Equal(StatusCodes.Status503ServiceUnavailable, context.Response.StatusCode);
    }

    private static BasicAuthMiddleware CreateMiddleware(bool configured = true)
    {
        var values = configured
            ? new Dictionary<string, string?>
            {
                ["BasicAuth:Username"] = "dashboard",
                ["BasicAuth:Password"] = "secret-password",
            }
            : [];

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(values)
            .Build();

        return new BasicAuthMiddleware(
            next: context =>
            {
                context.Response.StatusCode = StatusCodes.Status204NoContent;
                return Task.CompletedTask;
            },
            configuration,
            NullLogger<BasicAuthMiddleware>.Instance);
    }

    private static DefaultHttpContext CreateContext(string path, string method = "GET")
    {
        var context = new DefaultHttpContext();
        context.Request.Method = method;
        context.Request.Path = path;
        return context;
    }

    private static string BuildHeader(string username, string password)
    {
        var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{username}:{password}"));
        return $"Basic {credentials}";
    }
}
