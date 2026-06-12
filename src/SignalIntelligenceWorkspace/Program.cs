using SignalIntelligenceWorkspace.Components;
using SignalIntelligenceWorkspace.Services;
using SignalIntelligenceWorkspace.Services.Scenarios;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddTelerikBlazor();

// Scenario-neutral engine: swapping to another scenario pack is this one line.
builder.Services.AddSingleton(DksProposalScenario.Create());
builder.Services.AddSingleton(TimeProvider.System);
// Singleton so demo state (drafts, filter, audit) survives full-page navigation
// between routes. This is a single-user demo; multi-user/per-session state is roadmap.
builder.Services.AddSingleton<WorkspaceState>();

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

app.MapStaticAssets();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();