namespace SignalIntelligenceWorkspace.Services.Frontstage;

public sealed class FrontstageDeliveryOptions
{
    public string ConnectionStringKey { get; set; } = "Cockpit:ConnectionString";

    public string SchemaName { get; set; } = "frontstage";

    public string TokenPepper { get; set; } = "frontstage-dev-pepper-20260626";
}
