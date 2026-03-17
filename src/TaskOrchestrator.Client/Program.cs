using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.AspNetCore.SignalR.Client;
using TaskOrchestrator.Client;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

var apiBaseUrl = builder.Configuration["ApiBaseUrl"]
    ?? "http://localhost:5150";

builder.Services.AddScoped(_ => new HttpClient
{
    BaseAddress = new Uri(apiBaseUrl)
});

// SignalR hub connection — registered as a factory so each page
// that needs it can build and manage its own connection lifecycle.
builder.Services.AddTransient(_ =>
    new HubConnectionBuilder()
        .WithUrl($"{apiBaseUrl}/hubs/tasks")
        .WithAutomaticReconnect()
        .Build());

await builder.Build().RunAsync();
