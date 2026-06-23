using System.Globalization;
using Microsoft.AspNetCore.Localization;
using SignalIntelligenceWorkspace.Components;
using SignalIntelligenceWorkspace.Services;
using SignalIntelligenceWorkspace.Services.ApplicationIntelligence;
using SignalIntelligenceWorkspace.Services.Cockpit;
using SignalIntelligenceWorkspace.Services.HubSpot;
using SignalIntelligenceWorkspace.Services.Scenarios;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.AddControllers();
builder.Services.AddLocalization();

builder.Services.AddTelerikBlazor();

// Scenario-neutral engine: swapping to another scenario pack is this one line.
builder.Services.AddSingleton(DksProposalScenario.Create());
builder.Services.AddSingleton(TimeProvider.System);
// Singleton so demo state (drafts, filter, audit) survives full-page navigation
// between routes. This is a single-user demo; multi-user/per-session state is roadmap.
builder.Services.AddSingleton<WorkspaceState>();
builder.Services.AddScoped<ApplicationIntelligenceDataService>();
builder.Services.AddScoped<CockpitDataService>();
builder.Services.AddHttpClient<CockpitSemanticGridAiParser>();
builder.Services.AddHttpClient<HubSpotCrmService>();
builder.Services.AddHttpClient<HubSpotAssistantAiService>();

// English default; Traditional Chinese available via the in-app toggle.
var supportedCultures = new[] { "en", "zh-Hant" };
builder.Services.Configure<RequestLocalizationOptions>(options =>
{
    options.SetDefaultCulture("en")
        .AddSupportedCultures(supportedCultures)
        .AddSupportedUICultures(supportedCultures);
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);

app.UseHttpsRedirection();

app.UseRequestLocalization();

app.MapStaticAssets();
app.UseAntiforgery();

app.MapControllers();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
