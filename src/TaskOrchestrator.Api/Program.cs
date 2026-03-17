using TaskOrchestrator.Api.Features.Attachments;
using TaskOrchestrator.Api.Features.Boards;
using TaskOrchestrator.Api.Features.Cards;
using TaskOrchestrator.Api.Hubs;
using TaskOrchestrator.Api.Persistence;
using TaskOrchestrator.Api.Persistence.Repositories;
using TaskOrchestrator.Api.Services.FileStorage;

var builder = WebApplication.CreateBuilder(args);

// --- Persistence ---
var connStr = builder.Configuration.GetConnectionString("MariaDb")
    ?? throw new InvalidOperationException("Connection string 'MariaDb' is not configured.");

builder.Services.AddSingleton<IDbConnectionFactory>(new MariaDbConnectionFactory(connStr));
builder.Services.AddScoped<IBoardRepository,      BoardRepository>();
builder.Services.AddScoped<ICardRepository,       CardRepository>();
builder.Services.AddScoped<IAttachmentRepository, AttachmentRepository>();
builder.Services.AddScoped<IActivityRepository,   ActivityRepository>();

// --- Real-time ---
builder.Services.AddSignalR();
builder.Services.AddScoped<IBoardNotifier, BoardNotifier>();

// --- File storage ---
builder.Services.AddSingleton<IFileStorageService, LocalFileStorageService>();

// --- CORS ---
var allowedOrigins = builder.Configuration.GetSection("AllowedOrigins").Get<string[]>()
    ?? ["http://localhost:5173"];

builder.Services.AddCors(o => o.AddDefaultPolicy(p =>
    p.WithOrigins(allowedOrigins)
     .AllowAnyHeader()
     .AllowAnyMethod()
     .AllowCredentials()));

// --- App ---
var app = builder.Build();

app.UseCors();

if (app.Environment.IsDevelopment())
    app.UseDeveloperExceptionPage();

app.UseHttpsRedirection();

app.MapHub<TaskHub>("/hubs/board");

app.MapBoardEndpoints();
app.MapCardEndpoints();
app.MapAttachmentEndpoints();

app.Run();
