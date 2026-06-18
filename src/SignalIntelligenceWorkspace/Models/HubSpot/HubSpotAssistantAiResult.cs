namespace SignalIntelligenceWorkspace.Models.HubSpot;

public sealed record HubSpotAssistantAiResult(
    bool Attempted,
    string? Message,
    string? Error)
{
    public static HubSpotAssistantAiResult NotConfigured() =>
        new(false, null, null);

    public static HubSpotAssistantAiResult Answered(string message) =>
        new(true, message, null);

    public static HubSpotAssistantAiResult Failed(string error) =>
        new(true, null, error);
}
