namespace SignalIntelligenceWorkspace.Services.Security;

public sealed class BasicAuthMiddleware(
    RequestDelegate next,
    IConfiguration configuration,
    ILogger<BasicAuthMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
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
}

public static class BasicAuthMiddlewareExtensions
{
    public static IApplicationBuilder UseBasicAuthentication(this IApplicationBuilder app) =>
        app.UseMiddleware<BasicAuthMiddleware>();
}
