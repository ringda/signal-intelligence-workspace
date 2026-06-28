using Microsoft.AspNetCore.Mvc;

namespace SignalIntelligenceWorkspace.Services.PublicFeedback;

public static class PublicFeedbackEndpoints
{
    public const string RateLimitPolicyName = "public-feedback";

    public static IEndpointRouteBuilder MapPublicFeedbackEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/api/public-feedback", SubmitAsync)
            .RequireRateLimiting(RateLimitPolicyName);

        return endpoints;
    }

    private static async Task<IResult> SubmitAsync(
        [FromBody] PublicFeedbackSubmitRequest request,
        PublicFeedbackInbox inbox,
        CancellationToken cancellationToken)
    {
        try
        {
            var receipt = await inbox.SubmitAsync(
                new PublicFeedbackSubmission(request.FeedbackType, request.Message, request.PagePath),
                cancellationToken);

            return Results.Ok(new PublicFeedbackSubmitResponse(receipt.Id, receipt.SubmittedAt));
        }
        catch (ArgumentException exception)
        {
            return Results.BadRequest(new PublicFeedbackErrorResponse(exception.Message));
        }
    }

}

public sealed record PublicFeedbackSubmitRequest(
    string FeedbackType,
    string Message,
    string PagePath);

public sealed record PublicFeedbackSubmitResponse(
    string Id,
    DateTimeOffset SubmittedAt);

public sealed record PublicFeedbackErrorResponse(string Message);
