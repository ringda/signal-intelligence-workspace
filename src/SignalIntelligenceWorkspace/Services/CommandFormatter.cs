using System.Text.Json;
using SignalIntelligenceWorkspace.Models;

namespace SignalIntelligenceWorkspace.Services;

public static class CommandFormatter
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public static string ToJson(SafeCommand command) =>
        JsonSerializer.Serialize(command, command.GetType(), Options);
}
