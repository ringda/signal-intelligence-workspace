using System.Globalization;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.RateLimiting;
using SignalIntelligenceWorkspace.Components;
using SignalIntelligenceWorkspace.Services;
using SignalIntelligenceWorkspace.Services.ApplicationIntelligence;
using SignalIntelligenceWorkspace.Services.Cockpit;
using SignalIntelligenceWorkspace.Services.Frontstage;
using SignalIntelligenceWorkspace.Services.HubSpot;
using SignalIntelligenceWorkspace.Services.HubSpotWorkflow;
using SignalIntelligenceWorkspace.Services.PublicCadence;
using SignalIntelligenceWorkspace.Services.PublicFeedback;
using SignalIntelligenceWorkspace.Services.Scenarios;
using SignalIntelligenceWorkspace.Services.Security;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.AddControllers();
builder.Services.AddLocalization();
builder.Services.AddHttpContextAccessor();

builder.Services.AddTelerikBlazor();
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.OnRejected = async (context, cancellationToken) =>
    {
        context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        context.HttpContext.Response.ContentType = "application/json";
        await context.HttpContext.Response.WriteAsJsonAsync(
            new PublicFeedbackErrorResponse("Too many quick reads. Please try again in a minute."),
            cancellationToken);
    };
    options.AddPolicy(PublicFeedbackEndpoints.RateLimitPolicyName, context =>
        RateLimitPartition.GetSlidingWindowLimiter(
            GetPublicFeedbackRateLimitPartition(context),
            _ => new SlidingWindowRateLimiterOptions
            {
                PermitLimit = 5,
                Window = TimeSpan.FromMinutes(1),
                SegmentsPerWindow = 6,
                QueueLimit = 0,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
            }));
});

// Scenario-neutral engine: swapping to another scenario pack is this one line.
builder.Services.AddSingleton(DksProposalScenario.Create());
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.Configure<PublicFeedbackOptions>(builder.Configuration.GetSection("PublicFeedback"));
builder.Services.Configure<FrontstageDeliveryOptions>(builder.Configuration.GetSection("FrontstageDelivery"));
builder.Services.Configure<PublicOperatingCadenceOptions>(builder.Configuration.GetSection("PublicOperatingCadence"));
builder.Services.AddSingleton<PublicFeedbackInbox>();
builder.Services.AddSingleton<IPublicFeedbackWriter, PostgresPublicFeedbackWriter>();
builder.Services.AddSingleton<PublicFeedbackSchemaInitializer>();
builder.Services.AddScoped<IFrontstageDeliveryResolver, PostgresFrontstageDeliveryResolver>();
builder.Services.AddScoped<IPublicOperatingCadenceReader, PostgresPublicOperatingCadenceReader>();
// Singleton so demo state (drafts, filter, audit) survives full-page navigation
// between routes. This is a single-user demo; multi-user/per-session state is roadmap.
builder.Services.AddSingleton<WorkspaceState>();
builder.Services.AddScoped<ApplicationIntelligenceDataService>();
builder.Services.AddScoped<CockpitDataService>();
builder.Services.AddHttpClient<CockpitSemanticGridAiParser>();
builder.Services.AddHttpClient<HubSpotCrmService>();
builder.Services.AddHttpClient<HubSpotAssistantAiService>();
builder.Services.AddScoped<HubSpotReadinessRules>();
builder.Services.AddScoped<HubSpotWritebackPolicy>();
builder.Services.AddScoped<HubSpotProposalBuilder>();
builder.Services.AddScoped<HubSpotWorkflowAuditLog>();
builder.Services.AddScoped<HubSpotWorkflowEngine>();

// English default; Traditional Chinese available via the in-app toggle.
var supportedCultures = new[] { "en", "zh-Hant" };
builder.Services.Configure<RequestLocalizationOptions>(options =>
{
    options.SetDefaultCulture("en")
        .AddSupportedCultures(supportedCultures)
        .AddSupportedUICultures(supportedCultures);
});

var app = builder.Build();

await using (var scope = app.Services.CreateAsyncScope())
{
    var initializer = scope.ServiceProvider.GetRequiredService<PublicFeedbackSchemaInitializer>();
    await initializer.EnsureCreatedAsync();
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);

app.UseHttpsRedirection();

app.UseCanonicalHostRedirect();
app.UseBasicAuthentication();

app.UseRequestLocalization();
app.UseRateLimiter();
app.UseStaticFiles();

app.MapStaticAssets();
app.UseAntiforgery();

app.MapPublicFeedbackEndpoints();
app.MapFrontstageTrackingEndpoints();
app.MapControllers();
app.MapMethods("/", new[] { HttpMethods.Head }, () => Results.Ok());
app.MapMethods("/home", new[] { HttpMethods.Head }, () => Results.Ok());
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();

static string GetPublicFeedbackRateLimitPartition(HttpContext context)
{
    var forwardedFor = context.Request.Headers["X-Forwarded-For"].ToString();
    if (!string.IsNullOrWhiteSpace(forwardedFor))
    {
        return forwardedFor.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)[0];
    }

    return context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
}
