using Blazored.LocalStorage;
using CookingApp.Web.Components;
using CookingApp.Web.Services;
using Microsoft.AspNetCore.Components.Web;

// NOTE: This is Blazor Server — Program.cs uses the WebApplication pattern.
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddBlazoredLocalStorage();

// Typed HTTP client pointed at the API project
builder.Services.AddHttpClient<ApiClient>(client =>
{
    client.BaseAddress = new Uri(
        builder.Configuration["ApiBaseUrl"] ?? "https://localhost:7001");
});

builder.Services.AddScoped<AuthStateService>();
builder.Services.AddScoped<UserLocationService>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
