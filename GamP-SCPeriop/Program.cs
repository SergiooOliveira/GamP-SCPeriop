using Blazored.LocalStorage;
using Blazored.SessionStorage;
using GamP_SCPeriop;
using GamP_SCPeriop.Client.Auth;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.DependencyInjection;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddBlazoredLocalStorage();
builder.Services.AddBlazoredSessionStorage();

builder.Services.AddAuthorizationCore();
builder.Services.AddScoped<AuthenticationStateProvider, CustomAuthStateProvider>();

// NEW: register the handler
builder.Services.AddScoped<AuthTokenHandler>();

// CHANGED: was AddScoped(sp => new HttpClient {...}), now a named client with the handler attached
builder.Services.AddHttpClient("API", client =>
        client.BaseAddress = new Uri(builder.HostEnvironment.BaseAddress))
    .AddHttpMessageHandler<AuthTokenHandler>();

// NEW: makes plain @inject HttpClient in your components resolve to the "API" client above
builder.Services.AddScoped(sp => sp.GetRequiredService<IHttpClientFactory>().CreateClient("API"));

await builder.Build().RunAsync();