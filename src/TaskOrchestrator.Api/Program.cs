using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using TaskOrchestrator.Api.Features.Attachments;
using TaskOrchestrator.Api.Features.Boards;
using TaskOrchestrator.Api.Features.Cards;
using TaskOrchestrator.Api.Features.Columns;
using TaskOrchestrator.Api.Features.Me;
using TaskOrchestrator.Api.Features.Members;
using TaskOrchestrator.Api.Features.Teams;
using TaskOrchestrator.Api.Hubs;
using TaskOrchestrator.Api.Persistence;
using TaskOrchestrator.Api.Persistence.Repositories;
using TaskOrchestrator.Api.Services;
using TaskOrchestrator.Api.Services.FileStorage;
using TaskOrchestrator.Api.Services.Invites;

var builder = WebApplication.CreateBuilder(args);

// --- Auth ---
var auth0Domain   = builder.Configuration["Auth0:Domain"]
    ?? throw new InvalidOperationException("Auth0:Domain is not configured.");
var auth0Audience = builder.Configuration["Auth0:Audience"]
    ?? throw new InvalidOperationException("Auth0:Audience is not configured.");

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = $"https://{auth0Domain}/";
        options.Audience  = auth0Audience;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            NameClaimType  = "sub",
            RoleClaimType  = "https://taskorch/roles",
        };
        // Allow the JWT to be passed via the SignalR query-string
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = ctx =>
            {
                var token = ctx.Request.Query["access_token"];
                if (!string.IsNullOrEmpty(token) &&
                    ctx.HttpContext.Request.Path.StartsWithSegments("/hubs"))
                    ctx.Token = token;
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddHttpContextAccessor();

// --- Persistence ---
var connStr = builder.Configuration.GetConnectionString("MariaDb")
    ?? throw new InvalidOperationException("Connection string 'MariaDb' is not configured.");

builder.Services.AddSingleton<IDbConnectionFactory>(new MariaDbConnectionFactory(connStr));
builder.Services.AddScoped<IBoardRepository,       BoardRepository>();
builder.Services.AddScoped<ICardRepository,        CardRepository>();
builder.Services.AddScoped<IAttachmentRepository,  AttachmentRepository>();
builder.Services.AddScoped<IActivityRepository,    ActivityRepository>();
builder.Services.AddScoped<IUserRepository,        UserRepository>();
builder.Services.AddScoped<IBoardMemberRepository, BoardMemberRepository>();
builder.Services.AddScoped<IInviteRepository,      InviteRepository>();
builder.Services.AddScoped<IColumnRepository,      ColumnRepository>();
builder.Services.AddScoped<ITeamRepository,        TeamRepository>();
builder.Services.AddScoped<ITeamInviteRepository,  TeamInviteRepository>();

// --- User context (reads JWT claims) ---
builder.Services.AddScoped<IUserContext, UserContext>();
builder.Services.AddHttpClient<IInviteEmailService, ResendInviteEmailService>();

// --- Real-time ---
builder.Services.AddSignalR();
builder.Services.AddScoped<IBoardNotifier, BoardNotifier>();

// --- File storage ---
builder.Services.AddSingleton<IFileStorageService, LocalFileStorageService>();

// --- Email ---
builder.Services.AddHttpClient();
builder.Services.AddScoped<IEmailService, ResendEmailService>();

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

if (app.Environment.IsDevelopment())
    app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/health", () => Results.Ok(new { status = "ok" }))
   .AllowAnonymous();

app.MapHub<TaskHub>("/hubs/board");

app.MapBoardEndpoints();
app.MapCardEndpoints();
app.MapAttachmentEndpoints();
app.MapMeEndpoints();
app.MapMemberEndpoints();
app.MapJoinEndpoint();
app.MapColumnEndpoints();
app.MapTeamEndpoints();

app.Run();
