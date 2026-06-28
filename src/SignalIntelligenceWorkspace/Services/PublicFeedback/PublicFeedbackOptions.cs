namespace SignalIntelligenceWorkspace.Services.PublicFeedback;

public sealed class PublicFeedbackOptions
{
    public string ConnectionStringKey { get; set; } = "Cockpit:ConnectionString";

    public string SchemaName { get; set; } = "public_feedback";

    public string TableName { get; set; } = "submissions";
}
