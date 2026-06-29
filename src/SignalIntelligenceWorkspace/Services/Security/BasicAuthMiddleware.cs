namespace SignalIntelligenceWorkspace.Services.Security;

public sealed class BasicAuthMiddleware(
    RequestDelegate next,
    IConfiguration configuration,
    ILogger<BasicAuthMiddleware> logger)
{
    private static readonly string[] PublicRootAssetExtensions =
    [
        ".css",
        ".js",
        ".png",
        ".jpg",
        ".jpeg",
        ".svg",
        ".ico",
        ".webmanifest",
        ".woff",
        ".woff2"
    ];

    public async Task InvokeAsync(HttpContext context)
    {
        if (AllowsAnonymous(context.Request.Path))
        {
            await next(context);
            return;
        }

        var options = BasicAuthOptions.FromConfiguration(configuration);

        if (!options.IsConfigured)
        {
            logger.LogError(
                "Basic Auth is not configured. Set BasicAuth:Username and BasicAuth:Password through user-secrets, environment variables, or Azure App Settings.");

            context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
            context.Response.ContentType = "text/plain";
            await context.Response.WriteAsync("Basic Auth is not configured.");
            return;
        }

        var authorizationHeader = context.Request.Headers.Authorization.ToString();
        if (!BasicAuthCredentialValidator.IsAuthorized(authorizationHeader, options))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            context.Response.Headers.WWWAuthenticate = BuildAuthenticateHeader(options.Realm);
            context.Response.ContentType = "text/plain";
            await context.Response.WriteAsync("Authentication required.");
            return;
        }

        await next(context);
    }

    private static string BuildAuthenticateHeader(string realm)
    {
        var escapedRealm = realm
            .Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("\"", "\\\"", StringComparison.Ordinal);

        return $"Basic realm=\"{escapedRealm}\", charset=\"UTF-8\"";
    }

    private static bool AllowsAnonymous(PathString path)
    {
        var value = path.Value ?? string.Empty;
        if (string.IsNullOrEmpty(value) || value is "/" or "/home")
        {
            return true;
        }

        return value.StartsWith("/_framework/", StringComparison.OrdinalIgnoreCase) ||
            value.StartsWith("/_content/", StringComparison.OrdinalIgnoreCase) ||
            value.StartsWith("/_blazor", StringComparison.OrdinalIgnoreCase) ||
            value.StartsWith("/r/", StringComparison.OrdinalIgnoreCase) ||
            value.Equals("/api/public-feedback", StringComparison.OrdinalIgnoreCase) ||
            value.Equals("/api/frontstage/section-view", StringComparison.OrdinalIgnoreCase) ||
            value.Equals("/api/frontstage/click", StringComparison.OrdinalIgnoreCase) ||
            value.StartsWith("/Components/", StringComparison.OrdinalIgnoreCase) ||
            value.Equals("/favicon.png", StringComparison.OrdinalIgnoreCase) ||
            value.Equals("/app.css", StringComparison.OrdinalIgnoreCase) ||
            value.Equals("/SignalIntelligenceWorkspace.styles.css", StringComparison.OrdinalIgnoreCase) ||
            IsRootStaticAsset(value);
    }

    private static bool IsRootStaticAsset(string path)
    {
        if (path.Length == 0 || path[0] != '/' || path.IndexOf('/', 1) >= 0)
        {
            return false;
        }

        return PublicRootAssetExtensions.Any(extension =>
            path.EndsWith(extension, StringComparison.OrdinalIgnoreCase));
    }
}

public static class BasicAuthMiddlewareExtensions
{
    public static IApplicationBuilder UseBasicAuthentication(this IApplicationBuilder app) =>
        app.UseMiddleware<BasicAuthMiddleware>();
}
