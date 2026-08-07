using HolonetAgents.Web.Components;
using HolonetAgents.Web.Models;
using HolonetAgents.Web.Services;
using HolonetAgents.Web.Services.AgentResponses;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddOptions<FoundryOptions>()
    .Bind(builder.Configuration.GetSection(FoundryOptions.SectionName));

builder.Services.AddSingleton<IFoundryAgentService, FoundryAgentService>();
builder.Services.AddSingleton<AgentResponseParser>();
builder.Services.AddSingleton<AgentResponseContractResolver>();
builder.Services.AddSingleton<HolonetWorkflowService>();
builder.Services.AddHostedService<FoundryAgentCacheWarmupService>();

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

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
