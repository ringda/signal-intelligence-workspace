namespace SignalIntelligenceWorkspace.Models;

/// <summary>
/// A self-contained scenario data pack. The engine never renders <see cref="Id"/>;
/// swapping scenarios is one new data file plus one DI registration change.
/// </summary>
public sealed record ScenarioPack(
    string Id,
    string DisplayTitle,
    IReadOnlyList<string> Segments,
    IReadOnlyList<string> ResourceTypes,
    IReadOnlyList<ResourceRow> Resources,
    IReadOnlyList<ThemeRow> Themes,
    IReadOnlyList<string> DemoPrompts);
