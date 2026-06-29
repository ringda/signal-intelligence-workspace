using Microsoft.AspNetCore.Http.Extensions;

namespace SignalIntelligenceWorkspace.Services.Security;

public sealed class CanonicalHostRedirectMiddleware(RequestDelegate next)
{
    public const string WwwHost = "www.johannafan.com";
    public const string ApexHost = "johannafan.com";

    public Task InvokeAsync(HttpContext context)
    {
        if (ShouldRedirect(context.Request.Host))
        {
            context.Response.Redirect(BuildApexUrl(context.Request), permanent: true);
            return Task.CompletedTask;
        }

        return next(context);
    }

    public static bool ShouldRedirect(HostString host)
    {
        return host.Host.Equals(WwwHost, StringComparison.OrdinalIgnoreCase);
    }

    public static string BuildApexUrl(HttpRequest request)
    {
        return UriHelper.BuildAbsolute(
            request.Scheme,
            new HostString(ApexHost),
            request.PathBase,
            request.Path,
            request.QueryString);
    }
}

public static class CanonicalHostRedirectMiddlewareExtensions
{
    public static IApplicationBuilder UseCanonicalHostRedirect(this IApplicationBuilder app) =>
        app.UseMiddleware<CanonicalHostRedirectMiddleware>();
}
