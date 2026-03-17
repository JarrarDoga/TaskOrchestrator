using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using TaskOrchestrator.Client;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

var apiBase = builder.Configuration["ApiBaseUrl"] ?? "http://localhost:5150";
builder.Services.AddScoped(_ => new HttpClient { BaseAddress = new Uri(apiBase) });

await builder.Build().RunAsync();
