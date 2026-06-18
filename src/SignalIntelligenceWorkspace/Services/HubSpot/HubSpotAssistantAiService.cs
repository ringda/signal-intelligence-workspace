using System.Text;
using System.Text.Json;
using SignalIntelligenceWorkspace.Models.HubSpot;

namespace SignalIntelligenceWorkspace.Services.HubSpot;

public sealed class HubSpotAssistantAiService(HttpClient httpClient, IConfiguration configuration)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly string? _geminiApiKey =
        configuration["Gemini:ApiKey"] ??
        configuration["GoogleAI:ApiKey"] ??
        Environment.GetEnvironmentVariable("GEMINI_API_KEY") ??
        Environment.GetEnvironmentVariable("GOOGLE_API_KEY") ??
        Environment.GetEnvironmentVariable("GOOGLE_AI_API_KEY") ??
        Environment.GetEnvironmentVariable("GOOGLE_GENERATIVE_AI_API_KEY");

    private readonly string _geminiModel =
        configuration["Gemini:Model"] ??
        Environment.GetEnvironmentVariable("GEMINI_MODEL") ??
        "gemini-2.5-flash";

    public string ProviderName => "Gemini";

    public bool IsConfigured => !string.IsNullOrWhiteSpace(_geminiApiKey);

    public async Task<HubSpotAssistantAiResult> AskAsync(
        string prompt,
        string hubSpotContext,
        CancellationToken cancellationToken = default)
    {
        if (!IsConfigured)
        {
            return HubSpotAssistantAiResult.NotConfigured();
        }

        var requestUri =
            $"https://generativelanguage.googleapis.com/v1beta/models/{Uri.EscapeDataString(_geminiModel)}:generateContent";
        var requestBody = JsonSerializer.Serialize(CreateGeminiRequestBody(prompt, hubSpotContext), JsonOptions);
        const int maxAttempts = 3;

        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, requestUri);
            request.Headers.Add("x-goog-api-key", _geminiApiKey!);
            request.Content = new StringContent(requestBody, Encoding.UTF8, "application/json");

            try
            {
                using var response = await httpClient.SendAsync(request, cancellationToken);
                if (!response.IsSuccessStatusCode)
                {
                    if (IsTransientStatusCode((int)response.StatusCode) && attempt < maxAttempts)
                    {
                        await Task.Delay(TimeSpan.FromMilliseconds(500 * attempt), cancellationToken);
                        continue;
                    }

                    return HubSpotAssistantAiResult.Failed(
                        $"Gemini assistant request failed with HTTP {(int)response.StatusCode}.");
                }

                await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
                using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
                var outputText = ExtractGeminiOutputText(document.RootElement);
                return string.IsNullOrWhiteSpace(outputText)
                    ? HubSpotAssistantAiResult.Failed("Gemini returned no assistant text output.")
                    : HubSpotAssistantAiResult.Answered(NormalizeAssistantAnswer(outputText));
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (HttpRequestException) when (attempt < maxAttempts)
            {
                await Task.Delay(TimeSpan.FromMilliseconds(500 * attempt), cancellationToken);
                continue;
            }
            catch (Exception ex)
            {
                return HubSpotAssistantAiResult.Failed(ex.Message);
            }
        }

        return HubSpotAssistantAiResult.Failed("Gemini assistant request failed after retry.");
    }

    private object CreateGeminiRequestBody(string prompt, string hubSpotContext) =>
        new
        {
            systemInstruction = new
            {
                parts = new object[]
                {
                    new
                    {
                        text = SystemInstruction,
                    },
                },
            },
            contents = new object[]
            {
                new
                {
                    role = "user",
                    parts = new object[]
                    {
                        new
                        {
                            text = CreateAssistantPrompt(prompt, hubSpotContext),
                        },
                    },
                },
            },
            generationConfig = new
            {
                temperature = 0.2,
                maxOutputTokens = 2048,
            },
        };

    private const string SystemInstruction = """
        You are an AI assistant embedded in a HubSpot CRM signal cockpit.

        Use only the HubSpot CRM context supplied by the app. Do not claim to have searched the web, queried HubSpot directly, opened files, sent emails, edited CRM data, created tasks, or viewed secrets.
        The page is read-only. If the user asks to create, update, delete, log, or write back to HubSpot, draft the proposed note/task/field changes and explicitly say that no CRM write was executed.
        Help the user reason about CRM hygiene, account/contact/deal readiness, follow-up priority, next-touch memos, and missing handoff fields.
        Keep answers concise and decision-oriented. Use bullet points only when they improve scanning.
        Keep the final answer under about 900 Chinese characters unless the user explicitly asks for more detail.
        Answer in Traditional Chinese by default, unless the user asks in English or requests English wording.
        When evidence is missing, say what is missing and suggest the next review step.
        """;

    private static string CreateAssistantPrompt(string prompt, string hubSpotContext) =>
        $"""
        Current HubSpot CRM context:
        {hubSpotContext}

        User request:
        {prompt}
        """;

    private static string NormalizeAssistantAnswer(string answer)
    {
        var normalized = answer.Replace("\r\n", "\n").Trim();
        while (normalized.Contains("\n\n\n", StringComparison.Ordinal))
        {
            normalized = normalized.Replace("\n\n\n", "\n\n", StringComparison.Ordinal);
        }

        return normalized.Length <= 1800
            ? normalized
            : normalized[..1800];
    }

    private static string? ExtractGeminiOutputText(JsonElement root)
    {
        if (!root.TryGetProperty("candidates", out var candidates) ||
            candidates.ValueKind is not JsonValueKind.Array)
        {
            return null;
        }

        var builder = new StringBuilder();
        foreach (var candidate in candidates.EnumerateArray())
        {
            if (!candidate.TryGetProperty("content", out var content) ||
                !content.TryGetProperty("parts", out var parts) ||
                parts.ValueKind is not JsonValueKind.Array)
            {
                continue;
            }

            foreach (var part in parts.EnumerateArray())
            {
                if (part.TryGetProperty("text", out var text) &&
                    text.ValueKind is JsonValueKind.String)
                {
                    builder.Append(text.GetString());
                }
            }

            if (builder.Length > 0)
            {
                return builder.ToString();
            }
        }

        return null;
    }

    private static bool IsTransientStatusCode(int statusCode) =>
        statusCode is 429 or 500 or 502 or 503 or 504;
}
