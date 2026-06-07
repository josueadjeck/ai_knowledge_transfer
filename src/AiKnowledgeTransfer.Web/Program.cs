using AiKnowledgeTransfer.Application;
using AiKnowledgeTransfer.Infrastructure;
using AiKnowledgeTransfer.Web.Components;

var builder = WebApplication.CreateBuilder(args);
var storageRootPath = Path.Combine(builder.Environment.ContentRootPath, "App_Data", "uploads");

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services
    .AddApplication()
    .AddInfrastructure(storageRootPath);

var app = builder.Build();

await app.Services.InitializeInfrastructureDatabaseAsync();

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
