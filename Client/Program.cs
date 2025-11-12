using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using UltimateProyect.Client;
using Microsoft.AspNetCore.Components.Authorization;
using UltimateProyect.Client.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
builder.Services.AddScoped<RecepcionService>();
builder.Services.AddScoped<SalidasService>();
builder.Services.AddScoped<AuthenticationStateProvider, CustomAuthStateProvider>();
builder.Services.AddAuthorizationCore(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
    options.AddPolicy("OperarioOnly", policy => policy.RequireRole("Operario"));
    options.AddPolicy("ICPOnly", policy => policy.RequireRole("ICP"));
});

await builder.Build().RunAsync();