using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using TaskOrchestrator.Client;
using TaskOrchestrator.Client.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

var apiBase = builder.Configuration["ApiBaseUrl"] ?? "http://localhost:5041";

// Authenticated HttpClient — attaches the Auth0 access token to every API request
builder.Services.AddHttpClient("API", c => c.BaseAddress = new Uri(apiBase))
    .AddHttpMessageHandler(sp =>
    {
        var handler = sp.GetRequiredService<AuthorizationMessageHandler>()
            .ConfigureHandler(
                authorizedUrls: [apiBase],
                scopes: [builder.Configuration["Auth0:Scope"] ?? "openid profile email"]);
        return handler;
    });

// Default HttpClient (no auth) kept for public endpoints / health checks
builder.Services.AddScoped(sp =>
    sp.GetRequiredService<IHttpClientFactory>().CreateClient("API"));

// Per-board services (scoped so each board page gets its own instance)
builder.Services.AddScoped<SignalRService>();
builder.Services.AddScoped<BoardStateService>();

// Auth0 OIDC
builder.Services.AddOidcAuthentication(options =>
{
    builder.Configuration.Bind("Auth0", options.ProviderOptions);
    options.ProviderOptions.ResponseType = "code";
    // Request a JWT (not opaque) by passing the audience
    options.ProviderOptions.AdditionalProviderParameters.Add(
        "audience", builder.Configuration["Auth0:Audience"] ?? string.Empty);
});

await builder.Build().RunAsync();
