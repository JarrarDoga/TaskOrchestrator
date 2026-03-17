using TaskOrchestrator.Api.Features.Boards;
using TaskOrchestrator.Api.Features.Tasks;
using TaskOrchestrator.Api.Hubs;
using TaskOrchestrator.Api.Persistence;

var builder = WebApplication.CreateBuilder(args);

// --- Services ---

var connectionString = builder.Configuration.GetConnectionString("MariaDb")
    ?? throw new InvalidOperationException("Connection string 'MariaDb' is not configured.");

builder.Services.AddSingleton<IDbConnectionFactory>(
    new MariaDbConnectionFactory(connectionString));

builder.Services.AddSignalR();

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.WithOrigins(
                builder.Configuration.GetSection("AllowedOrigins").Get<string[]>()
                ?? ["http://localhost:5173"])
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials());
});

// --- App ---

var app = builder.Build();

app.UseCors();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

app.UseHttpsRedirection();

app.MapHub<TaskHub>("/hubs/tasks");

app.MapBoardEndpoints();
app.MapTaskEndpoints();

app.Run();
