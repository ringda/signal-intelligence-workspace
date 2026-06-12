namespace SignalIntelligenceWorkspace.Models;

/// <summary>
/// Append-only. Entries are never mutated after creation — that is the point of an audit trail.
/// </summary>
public sealed record AuditEntry(
    string Id,
    string Timestamp,
    string Role,
    string Prompt,
    string GeneratedCommand,
    string Decision,
    string Reason);
