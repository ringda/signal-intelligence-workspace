namespace SignalIntelligenceWorkspace.Services.PublicCadence;

public sealed class PublicOperatingCadenceOptions
{
    public string ConnectionStringKey { get; set; } = "Cockpit:ConnectionString";

    public string SchemaName { get; set; } = "frontstage";

    public string ViewName { get; set; } = "public_operating_cadence_v1";
}
