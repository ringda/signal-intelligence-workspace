namespace SignalIntelligenceWorkspace.Services.Frontstage;

public sealed record FrontstageTokenContext(
    string TokenHash,
    string AudienceKey,
    string RoleLensKey,
    string PageVersion,
    string? ContentVariantId,
    string? HubSpotTaskId,
    string? OutreachAttemptKey,
    string? RepoJdFolder);

public sealed record FrontstageDeliveryRequest(
    string Token,
    string Language,
    string? Referrer,
    string? UserAgent,
    bool LogVisit);

public sealed record FrontstageSectionViewRequest(
    string Token,
    string SectionKey,
    string Language,
    string? Referrer,
    string? UserAgent);

public sealed record FrontstageClickRequest(
    string Token,
    string EventKey,
    string? Target,
    string Language,
    string? Referrer,
    string? UserAgent);

public interface IFrontstageDeliveryResolver
{
    Task<FrontstageTokenContext?> ResolveAsync(
        FrontstageDeliveryRequest request,
        CancellationToken cancellationToken = default);

    Task<bool> LogSectionViewAsync(
        FrontstageSectionViewRequest request,
        CancellationToken cancellationToken = default);

    Task<bool> LogClickAsync(
        FrontstageClickRequest request,
        CancellationToken cancellationToken = default);
}
