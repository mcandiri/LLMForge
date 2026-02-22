using LLMForge.Demo.Services;

var builder = WebApplication.CreateBuilder(args);

// Add Blazor services
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Register scoped services for session state (keys never persisted)
builder.Services.AddScoped<ApiKeyService>();
builder.Services.AddScoped<OrchestrationHistoryService>();
builder.Services.AddScoped<OrchestratorFactory>();
builder.Services.AddHttpClient();

var app = builder.Build();

// Configure the HTTP request pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<LLMForge.Demo.Components.App>()
    .AddInteractiveServerRenderMode();

app.Run();
