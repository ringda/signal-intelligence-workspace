namespace SignalIntelligenceWorkspace.Services.Frontstage;

public sealed record FrontstageDeliveryContext(
    string TokenHash,
    string AudienceKey,
    string RoleLensKey,
    string PageVersion,
    string ContentVariantId,
    IReadOnlyDictionary<string, FrontstageDeliveryCopy> PublicCopy)
{
    public FrontstageDeliveryCopy? GetCopy(string language)
    {
        if (PublicCopy.TryGetValue(language, out var copy))
        {
            return copy;
        }

        return PublicCopy.TryGetValue("en", out var englishCopy)
            ? englishCopy
            : PublicCopy.Values.FirstOrDefault();
    }
}

public sealed record FrontstageDeliveryCopy(
    string ContextLabel,
    string HeroLead,
    string MarketingText,
    string ConversationText,
    string Message1,
    string Message3);

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

public interface IFrontstageDeliveryResolver
{
    Task<FrontstageDeliveryContext?> ResolveAsync(
        FrontstageDeliveryRequest request,
        CancellationToken cancellationToken = default);

    Task<bool> LogSectionViewAsync(
        FrontstageSectionViewRequest request,
        CancellationToken cancellationToken = default);
}
