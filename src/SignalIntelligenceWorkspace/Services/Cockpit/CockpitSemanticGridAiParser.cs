using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using SignalIntelligenceWorkspace.Models.Cockpit;

namespace SignalIntelligenceWorkspace.Services.Cockpit;

public sealed class CockpitSemanticGridAiParser(HttpClient httpClient, IConfiguration configuration)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly string? _openAiApiKey =
        configuration["OpenAI:ApiKey"] ??
        Environment.GetEnvironmentVariable("OPENAI_API_KEY");

    private readonly string _openAiModel =
        configuration["OpenAI:Model"] ??
        Environment.GetEnvironmentVariable("OPENAI_MODEL") ??
        "gpt-5.5";

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

    private readonly CockpitSemanticGridAiProvider _provider =
        ResolveProvider(configuration);

    public string ProviderName => _provider switch
    {
        CockpitSemanticGridAiProvider.Gemini => "Gemini",
        CockpitSemanticGridAiProvider.OpenAi => "OpenAI",
        _ => "AI",
    };

    public bool IsConfigured => _provider switch
    {
        CockpitSemanticGridAiProvider.Gemini => !string.IsNullOrWhiteSpace(_geminiApiKey),
        CockpitSemanticGridAiProvider.OpenAi => !string.IsNullOrWhiteSpace(_openAiApiKey),
        _ => false,
    };

    public async Task<CockpitSemanticGridAiParseResult> ParseAsync(
        string prompt,
        CancellationToken cancellationToken = default)
    {
        if (!IsConfigured)
        {
            return CockpitSemanticGridAiParseResult.NotConfigured();
        }

        return _provider switch
        {
            CockpitSemanticGridAiProvider.Gemini => await ParseWithGeminiAsync(prompt, cancellationToken),
            CockpitSemanticGridAiProvider.OpenAi => await ParseWithOpenAiAsync(prompt, cancellationToken),
            _ => CockpitSemanticGridAiParseResult.NotConfigured(),
        };
    }

    public async Task<CockpitAssistantAiResult> AskAssistantAsync(
        string prompt,
        string cockpitContext,
        CancellationToken cancellationToken = default)
    {
        if (!IsConfigured)
        {
            return CockpitAssistantAiResult.NotConfigured();
        }

        return _provider switch
        {
            CockpitSemanticGridAiProvider.Gemini => await AskAssistantWithGeminiAsync(prompt, cockpitContext, cancellationToken),
            CockpitSemanticGridAiProvider.OpenAi => await AskAssistantWithOpenAiAsync(prompt, cockpitContext, cancellationToken),
            _ => CockpitAssistantAiResult.NotConfigured(),
        };
    }

    private async Task<CockpitSemanticGridAiParseResult> ParseWithOpenAiAsync(
        string prompt,
        CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/responses");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _openAiApiKey);
        request.Content = new StringContent(
            JsonSerializer.Serialize(CreateOpenAiRequestBody(prompt), JsonOptions),
            Encoding.UTF8,
            "application/json");

        try
        {
            using var response = await httpClient.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return CockpitSemanticGridAiParseResult.Failed(
                    $"OpenAI request failed with HTTP {(int)response.StatusCode}.");
            }

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
            var outputText = ExtractOutputText(document.RootElement);
            if (string.IsNullOrWhiteSpace(outputText))
            {
                return CockpitSemanticGridAiParseResult.Failed("OpenAI returned no structured text output.");
            }

            var command = JsonSerializer.Deserialize<CockpitSemanticGridAiCommand>(outputText, JsonOptions);
            return command is null
                ? CockpitSemanticGridAiParseResult.Failed("OpenAI returned an empty command.")
                : CockpitSemanticGridAiParseResult.Parsed(command);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return CockpitSemanticGridAiParseResult.Failed(ex.Message);
        }
    }

    private async Task<CockpitSemanticGridAiParseResult> ParseWithGeminiAsync(
        string prompt,
        CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"https://generativelanguage.googleapis.com/v1beta/models/{Uri.EscapeDataString(_geminiModel)}:generateContent");
        request.Headers.Add("x-goog-api-key", _geminiApiKey);
        request.Content = new StringContent(
            JsonSerializer.Serialize(CreateGeminiRequestBody(prompt), JsonOptions),
            Encoding.UTF8,
            "application/json");

        try
        {
            using var response = await httpClient.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return CockpitSemanticGridAiParseResult.Failed(
                    $"Gemini request failed with HTTP {(int)response.StatusCode}.");
            }

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
            var outputText = ExtractGeminiOutputText(document.RootElement);
            if (string.IsNullOrWhiteSpace(outputText))
            {
                return CockpitSemanticGridAiParseResult.Failed("Gemini returned no structured text output.");
            }

            var command = JsonSerializer.Deserialize<CockpitSemanticGridAiCommand>(outputText, JsonOptions);
            return command is null
                ? CockpitSemanticGridAiParseResult.Failed("Gemini returned an empty command.")
                : CockpitSemanticGridAiParseResult.Parsed(command);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return CockpitSemanticGridAiParseResult.Failed(ex.Message);
        }
    }

    private async Task<CockpitAssistantAiResult> AskAssistantWithOpenAiAsync(
        string prompt,
        string cockpitContext,
        CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/responses");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _openAiApiKey);
        request.Content = new StringContent(
            JsonSerializer.Serialize(CreateAssistantOpenAiRequestBody(prompt, cockpitContext), JsonOptions),
            Encoding.UTF8,
            "application/json");

        try
        {
            using var response = await httpClient.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return CockpitAssistantAiResult.Failed(
                    $"OpenAI assistant request failed with HTTP {(int)response.StatusCode}.");
            }

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
            var outputText = ExtractOutputText(document.RootElement);
            return string.IsNullOrWhiteSpace(outputText)
                ? CockpitAssistantAiResult.Failed("OpenAI returned no assistant text output.")
                : CockpitAssistantAiResult.Answered(NormalizeAssistantAnswer(outputText));
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return CockpitAssistantAiResult.Failed(ex.Message);
        }
    }

    private async Task<CockpitAssistantAiResult> AskAssistantWithGeminiAsync(
        string prompt,
        string cockpitContext,
        CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"https://generativelanguage.googleapis.com/v1beta/models/{Uri.EscapeDataString(_geminiModel)}:generateContent");
        request.Headers.Add("x-goog-api-key", _geminiApiKey);
        request.Content = new StringContent(
            JsonSerializer.Serialize(CreateAssistantGeminiRequestBody(prompt, cockpitContext), JsonOptions),
            Encoding.UTF8,
            "application/json");

        try
        {
            using var response = await httpClient.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return CockpitAssistantAiResult.Failed(
                    $"Gemini assistant request failed with HTTP {(int)response.StatusCode}.");
            }

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
            var outputText = ExtractGeminiOutputText(document.RootElement);
            return string.IsNullOrWhiteSpace(outputText)
                ? CockpitAssistantAiResult.Failed("Gemini returned no assistant text output.")
                : CockpitAssistantAiResult.Answered(NormalizeAssistantAnswer(outputText));
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return CockpitAssistantAiResult.Failed(ex.Message);
        }
    }

    private object CreateOpenAiRequestBody(string prompt) =>
        new
        {
            model = _openAiModel,
            store = false,
            reasoning = new
            {
                effort = "low",
            },
            input = new object[]
            {
                new
                {
                    role = "system",
                    content = new object[]
                    {
                        new
                        {
                            type = "input_text",
                            text = SystemInstruction,
                        },
                    },
                },
                new
                {
                    role = "user",
                    content = new object[]
                    {
                        new
                        {
                            type = "input_text",
                            text = prompt,
                        },
                    },
                },
            },
            text = new
            {
                verbosity = "low",
                format = new
                {
                    type = "json_schema",
                    name = "cockpit_grid_command",
                    strict = true,
                    schema = CreateResponseSchema(includeGeminiPropertyOrdering: false),
                },
            },
        };

    private object CreateAssistantOpenAiRequestBody(string prompt, string cockpitContext) =>
        new
        {
            model = _openAiModel,
            store = false,
            reasoning = new
            {
                effort = "low",
            },
            input = new object[]
            {
                new
                {
                    role = "system",
                    content = new object[]
                    {
                        new
                        {
                            type = "input_text",
                            text = AssistantSystemInstruction,
                        },
                    },
                },
                new
                {
                    role = "user",
                    content = new object[]
                    {
                        new
                        {
                            type = "input_text",
                            text = CreateAssistantPrompt(prompt, cockpitContext),
                        },
                    },
                },
            },
            text = new
            {
                verbosity = "low",
            },
        };

    private object CreateGeminiRequestBody(string prompt) =>
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
                            text = prompt,
                        },
                    },
                },
            },
            generationConfig = new
            {
                temperature = 0,
                maxOutputTokens = 512,
                responseMimeType = "application/json",
                responseSchema = CreateGeminiResponseSchema(),
            },
        };

    private object CreateAssistantGeminiRequestBody(string prompt, string cockpitContext) =>
        new
        {
            systemInstruction = new
            {
                parts = new object[]
                {
                    new
                    {
                        text = AssistantSystemInstruction,
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
                            text = CreateAssistantPrompt(prompt, cockpitContext),
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
        Convert a user's natural-language request into a read-only Grid command for a job application cockpit.

        Return only the structured output schema.

        Allowed filters:
        - strongFit: user asks for high fit, best fit, highly aligned, priority, recommended, worth applying, 值得投, 比較值得投, 優先投, or 最值得投 roles.
        - possibleFit: user asks for possible, medium, adjacent, edge, maybe roles.
        - needsReview: user asks for pending, unclear, hold, visa/work-authorization check, needs confirmation/review.
        - applied: user asks for already sent/submitted/applied jobs. In Traditional Chinese, use applied only for 已申請, 已送出, 已投, or 投遞. Do not treat generic 申請 or JD alone as applied.

        Allowed date windows:
        - lastThreeDays: user asks for the last/past/recent three days, recent few days, 3 days, 近三天, 最近三天, 這幾天, or 三天內.

        Allowed sorts:
        - newest: newest/recent/date/time first, 最新的放前面, 最新在前, or 時間新到舊.
        - company: company order.
        - fitScore: fit/score/best order.

        Allowed semantic content search:
        - Use contentQuery and contentTerms when the user asks for a job category, function, industry, skill, keyword, language, tool, audience, or work theme.
        - Examples: Sales 相關工作, sales operations, CRM/order management, GTM, marketing, data analytics, Mandarin, transportation, consulting, internships, customer-facing, ERP, reporting.
        - contentQuery should be a short human-readable label for the content filter.
        - contentTerms should contain 1 to 10 concrete search terms likely to appear in job company, title, match level, status, or judgement notes. Prefer concise English terms plus obvious acronyms; include important Traditional Chinese terms only when useful.
        - This is read-only local text search over the current Grid rows. Do not invent database fields or request remote data access.

        If dateWindow is lastThreeDays and no sort is specified, set sort to newest.
        If the user asks to clear/reset/show all, set clear true.
        If the user asks to mutate source data, send externally, reveal secrets, run SQL/code, or perform unsupported operations, set matched false.
        Company filters are handled locally by the app; do not infer company values.
        """;

    private const string AssistantSystemInstruction = """
        You are an AI assistant embedded in a job-search signal cockpit.

        Use only the cockpit context supplied by the app. Do not claim to have searched the web, queried databases, opened files, sent applications, edited source data, or viewed secrets.
        Help the user reason about visible jobs, selected application notes, fit, next steps, follow-up risks, and prioritization.
        Keep answers concise and decision-oriented. Use bullet points only when they improve scanning.
        Keep the final answer under about 900 Chinese characters unless the user explicitly asks for more detail.
        Answer in Traditional Chinese by default, unless the user asks in English or requests English wording.
        When evidence is missing, say what is missing and suggest the next review step.
        """;

    private static string CreateAssistantPrompt(string prompt, string cockpitContext) =>
        $"""
        Current cockpit context:
        {cockpitContext}

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

    private static Dictionary<string, object?> CreateResponseSchema(bool includeGeminiPropertyOrdering)
    {
        var schema = new Dictionary<string, object?>
        {
            ["type"] = "object",
            ["additionalProperties"] = false,
            ["properties"] = new Dictionary<string, object?>
            {
                ["matched"] = new Dictionary<string, object?>
                {
                    ["type"] = "boolean",
                },
                ["clear"] = new Dictionary<string, object?>
                {
                    ["type"] = "boolean",
                },
                ["filter"] = new Dictionary<string, object?>
                {
                    ["type"] = new object[] { "string", "null" },
                    ["enum"] = new object?[] { "strongFit", "possibleFit", "needsReview", "applied", null },
                },
                ["dateWindow"] = new Dictionary<string, object?>
                {
                    ["type"] = new object[] { "string", "null" },
                    ["enum"] = new object?[] { "lastThreeDays", null },
                },
                ["sort"] = new Dictionary<string, object?>
                {
                    ["type"] = new object[] { "string", "null" },
                    ["enum"] = new object?[] { "newest", "company", "fitScore", null },
                },
                ["contentQuery"] = new Dictionary<string, object?>
                {
                    ["type"] = new object[] { "string", "null" },
                    ["maxLength"] = 80,
                },
                ["contentTerms"] = new Dictionary<string, object?>
                {
                    ["type"] = "array",
                    ["items"] = new Dictionary<string, object?>
                    {
                        ["type"] = "string",
                        ["maxLength"] = 40,
                    },
                    ["maxItems"] = 10,
                },
                ["reason"] = new Dictionary<string, object?>
                {
                    ["type"] = "string",
                },
            },
            ["required"] = new[] { "matched", "clear", "filter", "dateWindow", "sort", "contentQuery", "contentTerms", "reason" },
        };

        if (includeGeminiPropertyOrdering)
        {
            schema["propertyOrdering"] = new[] { "matched", "clear", "filter", "dateWindow", "sort", "reason" };
        }

        return schema;
    }

    private static Dictionary<string, object?> CreateGeminiResponseSchema() =>
        new()
        {
            ["type"] = "object",
            ["properties"] = new Dictionary<string, object?>
            {
                ["matched"] = new Dictionary<string, object?>
                {
                    ["type"] = "boolean",
                    ["description"] = "Whether the request maps to one of the allowed read-only grid commands.",
                },
                ["clear"] = new Dictionary<string, object?>
                {
                    ["type"] = "boolean",
                    ["description"] = "True only when the user asks to clear, reset, or show all jobs.",
                },
                ["filter"] = new Dictionary<string, object?>
                {
                    ["type"] = "string",
                    ["nullable"] = true,
                    ["enum"] = new[] { "strongFit", "possibleFit", "needsReview", "applied" },
                    ["description"] = "Allowed fit/status filter, or null if no fit/status filter is requested.",
                },
                ["dateWindow"] = new Dictionary<string, object?>
                {
                    ["type"] = "string",
                    ["nullable"] = true,
                    ["enum"] = new[] { "lastThreeDays" },
                    ["description"] = "Allowed date window, or null if no date window is requested.",
                },
                ["sort"] = new Dictionary<string, object?>
                {
                    ["type"] = "string",
                    ["nullable"] = true,
                    ["enum"] = new[] { "newest", "company", "fitScore" },
                    ["description"] = "Allowed sort order, or null if no sort is requested.",
                },
                ["contentQuery"] = new Dictionary<string, object?>
                {
                    ["type"] = "string",
                    ["nullable"] = true,
                    ["description"] = "Short human-readable content filter label, or null if no content search is requested.",
                },
                ["contentTerms"] = new Dictionary<string, object?>
                {
                    ["type"] = "array",
                    ["items"] = new Dictionary<string, object?>
                    {
                        ["type"] = "string",
                    },
                    ["description"] = "Concrete local text-search terms for job title, company, match level, status, and judgement notes. Empty array if no content search is requested.",
                },
                ["reason"] = new Dictionary<string, object?>
                {
                    ["type"] = "string",
                    ["description"] = "Short reason for the command mapping or rejection.",
                },
            },
            ["required"] = new[] { "matched", "clear", "filter", "dateWindow", "sort", "contentQuery", "contentTerms", "reason" },
            ["propertyOrdering"] = new[] { "matched", "clear", "filter", "dateWindow", "sort", "contentQuery", "contentTerms", "reason" },
        };

    private static string? ExtractOutputText(JsonElement root)
    {
        if (root.TryGetProperty("output_text", out var outputText) &&
            outputText.ValueKind is JsonValueKind.String)
        {
            return outputText.GetString();
        }

        if (!root.TryGetProperty("output", out var output) ||
            output.ValueKind is not JsonValueKind.Array)
        {
            return null;
        }

        var builder = new StringBuilder();
        foreach (var item in output.EnumerateArray())
        {
            if (!item.TryGetProperty("content", out var content) ||
                content.ValueKind is not JsonValueKind.Array)
            {
                continue;
            }

            foreach (var contentItem in content.EnumerateArray())
            {
                if (contentItem.TryGetProperty("text", out var text) &&
                    text.ValueKind is JsonValueKind.String)
                {
                    builder.Append(text.GetString());
                }
            }
        }

        return builder.Length == 0 ? null : builder.ToString();
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

    private static CockpitSemanticGridAiProvider ResolveProvider(IConfiguration configuration)
    {
        var configuredProvider =
            configuration["Ai:Provider"] ??
            configuration["Cockpit:SemanticAiProvider"] ??
            Environment.GetEnvironmentVariable("AI_PROVIDER");

        if (string.Equals(configuredProvider, "Gemini", StringComparison.OrdinalIgnoreCase))
        {
            return CockpitSemanticGridAiProvider.Gemini;
        }

        if (string.Equals(configuredProvider, "OpenAI", StringComparison.OrdinalIgnoreCase))
        {
            return CockpitSemanticGridAiProvider.OpenAi;
        }

        var hasGeminiKey =
            !string.IsNullOrWhiteSpace(configuration["Gemini:ApiKey"]) ||
            !string.IsNullOrWhiteSpace(configuration["GoogleAI:ApiKey"]) ||
            !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("GEMINI_API_KEY")) ||
            !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("GOOGLE_API_KEY")) ||
            !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("GOOGLE_AI_API_KEY")) ||
            !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("GOOGLE_GENERATIVE_AI_API_KEY"));
        if (hasGeminiKey)
        {
            return CockpitSemanticGridAiProvider.Gemini;
        }

        var hasOpenAiKey =
            !string.IsNullOrWhiteSpace(configuration["OpenAI:ApiKey"]) ||
            !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("OPENAI_API_KEY"));
        return hasOpenAiKey
            ? CockpitSemanticGridAiProvider.OpenAi
            : CockpitSemanticGridAiProvider.None;
    }

    private enum CockpitSemanticGridAiProvider
    {
        None,
        OpenAi,
        Gemini,
    }
}
