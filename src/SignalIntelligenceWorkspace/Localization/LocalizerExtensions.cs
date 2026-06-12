using Microsoft.Extensions.Localization;
using SignalIntelligenceWorkspace.Models;

namespace SignalIntelligenceWorkspace.Localization;

public static class LocalizerExtensions
{
    public static string State(this IStringLocalizer<Ui> localizer, ReviewState state) =>
        localizer[$"State.{state}"];
}
