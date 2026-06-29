using System.Net.Http.Headers;
using System.Text.Json;
using SignalIntelligenceWorkspace.Models.HubSpot;

namespace SignalIntelligenceWorkspace.Services.HubSpot;

public sealed class HubSpotCrmService(HttpClient http, IConfiguration configuration)
{
    private const string DefaultPortalId = "246340763";
    private const string DefaultUiDomain = "app-na2.hubspot.com";
    private const string HubSpotApi = "https://api.hubapi.com";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private static readonly IReadOnlyDictionary<string, HubSpotObjectConfig> Objects =
        new Dictionary<string, HubSpotObjectConfig>
        {
            ["contacts"] = new(
                "Contacts",
                "Live CRM people records from HubSpot",
                "0-1",
                [
                    "firstname",
                    "lastname",
                    "email",
                    "company",
                    "jobtitle",
                    "lifecyclestage",
                    "hs_lead_status",
                    "hubspot_owner_id",
                    "createdate",
                    "lastmodifieddate",
                ]),
            ["companies"] = new(
                "Companies",
                "Live company/account records from HubSpot",
                "0-2",
                [
                    "name",
                    "domain",
                    "industry",
                    "numberofemployees",
                    "createdate",
                    "lastmodifieddate",
                ]),
            ["deals"] = new(
                "Deals",
                "Live pipeline records from HubSpot",
                "0-3",
                [
                    "dealname",
                    "dealstage",
                    "amount",
                    "closedate",
                    "pipeline",
                    "createdate",
                    "lastmodifieddate",
                ]),
        };

    private string PortalId => configuration["HubSpot:PortalId"] ?? DefaultPortalId;

    private string UiDomain => configuration["HubSpot:UiDomain"] ?? DefaultUiDomain;

    private string? Token =>
        configuration["HubSpot:PrivateAppAccessToken"]
        ?? configuration["PRIVATE_APP_ACCESS_TOKEN"];

    public async Task<HubSpotCrmSnapshot> GetSnapshotAsync(CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(Token))
        {
            throw new InvalidOperationException("PRIVATE_APP_ACCESS_TOKEN is not set in the server environment.");
        }

        var sections = await Task.WhenAll(
            BuildSectionAsync("contacts", cancellationToken),
            BuildSectionAsync("companies", cancellationToken),
            BuildSectionAsync("deals", cancellationToken));

        var readinessCards = await GetReadinessCardsAsync(cancellationToken);

        return new HubSpotCrmSnapshot
        {
            Sections = sections,
            ReadinessCards = readinessCards,
            PortalUrl = GetPortalUrl(),
            RefreshedAt = DateTimeOffset.Now,
        };
    }

    private async Task<HubSpotCrmSection> BuildSectionAsync(string name, CancellationToken cancellationToken)
    {
        var config = Objects[name];
        try
        {
            var rows = (await FetchHubSpotObjectsAsync(name, cancellationToken))
                .Select(item => MapRow(name, item))
                .ToList();

            return new HubSpotCrmSection
            {
                Title = config.Title,
                Subtitle = config.Subtitle,
                ObjectTypeId = config.ObjectTypeId,
                IndexUrl = IndexUrl(config.ObjectTypeId),
                Rows = rows,
            };
        }
        catch (Exception ex)
        {
            return new HubSpotCrmSection
            {
                Title = config.Title,
                Subtitle = config.Subtitle,
                ObjectTypeId = config.ObjectTypeId,
                IndexUrl = IndexUrl(config.ObjectTypeId),
                Rows = [],
                Error = ex.Message,
            };
        }
    }

    private async Task<IReadOnlyList<HubSpotReadinessCard>> GetReadinessCardsAsync(CancellationToken cancellationToken)
    {
        try
        {
            var contactsTask = FetchHubSpotObjectsAsync("contacts", cancellationToken);
            var ownersTask = FetchOwnersAsync(cancellationToken);
            await Task.WhenAll(contactsTask, ownersTask);

            return contactsTask.Result
                .Select(item => BuildReadinessCard(item, ownersTask.Result))
                .OrderBy(card => card.Score)
                .ThenBy(card => card.Name)
                .ToList();
        }
        catch
        {
            return [];
        }
    }

    private async Task<IReadOnlyList<HubSpotObject>> FetchHubSpotObjectsAsync(
        string name,
        CancellationToken cancellationToken)
    {
        var token = Token;
        if (string.IsNullOrWhiteSpace(token))
        {
            throw new InvalidOperationException("PRIVATE_APP_ACCESS_TOKEN is not set in the server environment.");
        }

        var config = Objects[name];
        var url =
            $"{HubSpotApi}/crm/v3/objects/{name}?limit=10&archived=false&properties={Uri.EscapeDataString(string.Join(",", config.Properties))}";

        var body = await SendAsync<HubSpotListResponse>(url, token, cancellationToken);
        return body.Results ?? [];
    }

    private async Task<Dictionary<string, string>> FetchOwnersAsync(CancellationToken cancellationToken)
    {
        var token = Token;
        if (string.IsNullOrWhiteSpace(token))
        {
            return [];
        }

        try
        {
            var body = await SendAsync<HubSpotOwnersResponse>(
                $"{HubSpotApi}/crm/v3/owners?limit=100&archived=false",
                token,
                cancellationToken);

            return (body.Results ?? [])
                .ToDictionary(
                    owner => owner.Id,
                    owner =>
                    {
                        var fullName = string.Join(" ", new[] { owner.FirstName, owner.LastName }
                            .Where(part => !string.IsNullOrWhiteSpace(part)));
                        return string.IsNullOrWhiteSpace(fullName)
                            ? owner.Email ?? $"Owner {owner.Id}"
                            : fullName;
                    });
        }
        catch
        {
            return [];
        }
    }

    private async Task<T> SendAsync<T>(string url, string token, CancellationToken cancellationToken)
        where T : HubSpotResponse, new()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        using var response = await http.SendAsync(request, cancellationToken);
        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        var body = string.IsNullOrWhiteSpace(json)
            ? new T()
            : JsonSerializer.Deserialize<T>(json, JsonOptions) ?? new T();

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(body.Message ?? $"HubSpot read failed with {(int)response.StatusCode}.");
        }

        return body;
    }

    private HubSpotCrmRow MapRow(string name, HubSpotObject item)
    {
        var properties = item.Properties;
        if (name == "contacts")
        {
            var first = Value(properties, "firstname", string.Empty);
            var last = Value(properties, "lastname", string.Empty);
            var primary = $"{first} {last}".Trim();

            return new HubSpotCrmRow
            {
                Id = item.Id,
                Primary = string.IsNullOrWhiteSpace(primary) ? Value(properties, "email", $"Contact {item.Id}") : primary,
                Secondary = Value(properties, "email"),
                Status = Value(properties, "lifecyclestage", "No lifecycle"),
                Detail = DetailOrFallback(
                    [Value(properties, "company", string.Empty), Value(properties, "jobtitle", string.Empty)],
                    "No company or title"),
                UpdatedAt = DisplayDate(Value(properties, "lastmodifieddate", item.UpdatedAt?.ToString("O") ?? "-")),
                HubSpotUrl = RecordUrl(Objects["contacts"].ObjectTypeId, item.Id),
            };
        }

        if (name == "companies")
        {
            return new HubSpotCrmRow
            {
                Id = item.Id,
                Primary = Value(properties, "name", $"Company {item.Id}"),
                Secondary = Value(properties, "domain"),
                Status = Value(properties, "industry", "No industry"),
                Detail = Value(properties, "numberofemployees", "No employee count"),
                UpdatedAt = DisplayDate(Value(properties, "lastmodifieddate", item.UpdatedAt?.ToString("O") ?? "-")),
                HubSpotUrl = RecordUrl(Objects["companies"].ObjectTypeId, item.Id),
            };
        }

        var amount = Value(properties, "amount");
        var closeDate = Value(properties, "closedate");
        return new HubSpotCrmRow
        {
            Id = item.Id,
            Primary = Value(properties, "dealname", $"Deal {item.Id}"),
            Secondary = amount == "-" ? "No amount" : $"${amount}",
            Status = Value(properties, "dealstage", "No stage"),
            Detail = closeDate == "-" ? "No close date" : $"Close {DisplayDate(closeDate)}",
            UpdatedAt = DisplayDate(Value(properties, "lastmodifieddate", item.UpdatedAt?.ToString("O") ?? "-")),
            HubSpotUrl = RecordUrl(Objects["deals"].ObjectTypeId, item.Id),
        };
    }

    private HubSpotReadinessCard BuildReadinessCard(HubSpotObject item, IReadOnlyDictionary<string, string> owners)
    {
        var properties = item.Properties;
        var first = Value(properties, "firstname", string.Empty);
        var last = Value(properties, "lastname", string.Empty);
        var name = $"{first} {last}".Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            name = Value(properties, "email", $"Contact {item.Id}");
        }

        var email = Value(properties, "email");
        var company = Value(properties, "company");
        var title = Value(properties, "jobtitle");
        var lifecycle = Value(properties, "lifecyclestage", "No lifecycle");
        var ownerId = Value(properties, "hubspot_owner_id", string.Empty);
        var owner = string.IsNullOrWhiteSpace(ownerId)
            ? "No owner"
            : owners.GetValueOrDefault(ownerId, $"Owner {ownerId}");

        var missing = new List<string>();
        if (email == "-") missing.Add("email");
        if (company == "-") missing.Add("company");
        if (title == "-") missing.Add("job title");
        if (lifecycle == "No lifecycle") missing.Add("lifecycle stage");
        if (owner == "No owner") missing.Add("owner");

        var score = Math.Max(20, 100 - missing.Count * 15);
        var status = ReadinessStatus(missing);
        var nextAction = NextActionForContact(missing, lifecycle);
        var missingText = missing.Count > 0 ? string.Join(", ", missing) : "none";
        var why = missing.Count > 0
            ? $"This record is useful, but {string.Join(", ", missing)} should be cleaned before handoff."
            : "This record has enough CRM context for a focused next action.";

        return new HubSpotReadinessCard
        {
            Id = item.Id,
            Name = name,
            Email = email,
            Company = company,
            Title = title,
            Lifecycle = lifecycle,
            Owner = owner,
            Score = score,
            Status = status,
            Missing = missing,
            NextAction = nextAction,
            Why = why,
            SuggestedTaskTitle = status == "Ready"
                ? $"Prepare next-touch memo for {name}"
                : $"Clean CRM handoff fields for {name}",
            SuggestedNoteBody =
                $"CRM handoff readiness review{Environment.NewLine}" +
                $"Contact: {name}{Environment.NewLine}" +
                $"Status: {status} ({score}/100){Environment.NewLine}" +
                $"Missing fields: {missingText}{Environment.NewLine}" +
                $"Recommended next action: {nextAction}{Environment.NewLine}" +
                $"Reason: {why}",
            UpdatedAt = DisplayDate(Value(properties, "lastmodifieddate", item.UpdatedAt?.ToString("O") ?? "-")),
            HubSpotUrl = RecordUrl(Objects["contacts"].ObjectTypeId, item.Id),
        };
    }

    private static string Value(IReadOnlyDictionary<string, string?> properties, string key, string fallback = "-")
    {
        return properties.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value)
            ? value
            : fallback;
    }

    private static string DetailOrFallback(IEnumerable<string> parts, string fallback)
    {
        var detail = string.Join(" / ", parts.Where(part => !string.IsNullOrWhiteSpace(part)));
        return string.IsNullOrWhiteSpace(detail) ? fallback : detail;
    }

    private static string DisplayDate(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw) || raw == "-")
        {
            return "-";
        }

        return DateTimeOffset.TryParse(raw, out var date)
            ? date.ToString("MMM d, yyyy")
            : raw;
    }

    private static string ReadinessStatus(IReadOnlyCollection<string> missing)
    {
        if (missing.Count == 0) return "Ready";
        return missing.Count <= 2 ? "Needs Cleanup" : "Blocked";
    }

    private static string NextActionForContact(IReadOnlyCollection<string> missing, string lifecycle)
    {
        if (missing.Contains("owner"))
        {
            return "Assign an owner before any follow-up or CRM writeback.";
        }

        if (missing.Contains("company") || missing.Contains("job title"))
        {
            return "Complete account context so the next touch is not generic.";
        }

        if (missing.Contains("lifecycle stage"))
        {
            return "Set lifecycle stage from the latest qualification signal.";
        }

        return lifecycle == "opportunity"
            ? "Prepare a next-touch memo and confirm timing, authority, and blocker."
            : "Qualify need and timing, then log a clear next task.";
    }

    private string GetPortalUrl() => $"https://{UiDomain}/contacts/{PortalId}";

    private string IndexUrl(string objectTypeId) => $"https://{UiDomain}/contacts/{PortalId}/objects/{objectTypeId}";

    private string RecordUrl(string objectTypeId, string objectId) =>
        $"https://{UiDomain}/contacts/{PortalId}/record/{objectTypeId}/{objectId}";

    private sealed record HubSpotObjectConfig(
        string Title,
        string Subtitle,
        string ObjectTypeId,
        IReadOnlyList<string> Properties);

    private abstract class HubSpotResponse
    {
        public string? Message { get; init; }
    }

    private sealed class HubSpotListResponse : HubSpotResponse
    {
        public List<HubSpotObject>? Results { get; init; }
    }

    private sealed class HubSpotOwnersResponse : HubSpotResponse
    {
        public List<HubSpotOwner>? Results { get; init; }
    }

    private sealed class HubSpotObject
    {
        public string Id { get; init; } = string.Empty;
        public DateTimeOffset? UpdatedAt { get; init; }
        public Dictionary<string, string?> Properties { get; init; } = [];
    }

    private sealed class HubSpotOwner
    {
        public string Id { get; init; } = string.Empty;
        public string? FirstName { get; init; }
        public string? LastName { get; init; }
        public string? Email { get; init; }
    }
}
