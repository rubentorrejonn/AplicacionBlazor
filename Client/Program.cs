using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using UltimateProyect.Client;
using Microsoft.AspNetCore.Components.Authorization;
using UltimateProyect.Client.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient
{
    BaseAddress = new Uri(builder.HostEnvironment.BaseAddress)
});

builder.Services.AddScoped<RecepcionService>();
builder.Services.AddScoped<SalidasService>();
builder.Services.AddScoped<UserService>();
builder.Services.AddScoped<AuthenticationStateProvider, CustomAuthStateProvider>();
builder.Services.AddAuthorizationCore(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
    options.AddPolicy("Operario", policy => policy.RequireRole("Operario"));
    options.AddPolicy("ICP", policy => policy.RequireRole("ICP"));
});
builder.Services.AddBlazorBootstrap();

await builder.Build().RunAsync();