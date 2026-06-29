using Microsoft.AspNetCore.Mvc;

namespace SignalIntelligenceWorkspace.Services.Frontstage;

public static class FrontstageTrackingEndpoints
{
    public const string SectionViewPath = "/api/frontstage/section-view";

    public static IEndpointRouteBuilder MapFrontstageTrackingEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost(SectionViewPath, SubmitSectionViewAsync);
        return endpoints;
    }

    private static async Task<IResult> SubmitSectionViewAsync(
        [FromBody] FrontstageSectionViewSubmitRequest request,
        IFrontstageDeliveryResolver resolver,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var logged = await resolver.LogSectionViewAsync(
            new FrontstageSectionViewRequest(
                request.Token,
                request.SectionKey,
                request.Language,
                httpContext.Request.Headers[Microsoft.Net.Http.Headers.HeaderNames.Referer].ToString(),
                httpContext.Request.Headers[Microsoft.Net.Http.Headers.HeaderNames.UserAgent].ToString()),
            cancellationToken);

        return logged
            ? Results.Ok(new FrontstageSectionViewSubmitResponse(true))
            : Results.BadRequest(new FrontstageSectionViewSubmitResponse(false));
    }
}

public sealed record FrontstageSectionViewSubmitRequest(
    string Token,
    string SectionKey,
    string Language);

public sealed record FrontstageSectionViewSubmitResponse(bool Logged);
