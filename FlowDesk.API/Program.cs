using System.Security.Claims;
using System.Text;
using System.Threading.RateLimiting;
using FlowDesk.API.Hubs;
using FlowDesk.API.Middleware;
using FlowDesk.Core.Interfaces;
using FlowDesk.Core.Services;
using FlowDesk.Infrastructure.Data;
using FlowDesk.Infrastructure.Repositories;
using FlowDesk.Infrastructure.Services;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;

// TraversePath() walks up from CWD until it finds .env (handles running from
// FlowDesk.API/ or the solution root interchangeably)
DotNetEnv.Env.TraversePath().Load();

// Npgsql 6+ requires DateTimes to be explicitly UTC for timestamptz columns.
// This switch makes it treat Unspecified kind the same as UTC (legacy behaviour).
AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

var builder = WebApplication.CreateBuilder(args);

// ── Database ───────────────────────────────────────────────────────────────────
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration["DATABASE_URL"]));

// ── HTTP context + current user ────────────────────────────────────────────────
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();

// ── Repositories and services ──────────────────────────────────────────────────
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddSingleton<IFileStorageService, FileStorageService>();
builder.Services.AddScoped<IOrganisationRepository, OrganisationRepository>();
builder.Services.AddScoped<IOrganisationService, OrganisationService>();
builder.Services.AddScoped<IProjectRepository, ProjectRepository>();
builder.Services.AddScoped<IProjectService, ProjectService>();
builder.Services.AddScoped<IMilestoneRepository, MilestoneRepository>();
builder.Services.AddScoped<IMilestoneService, MilestoneService>();
builder.Services.AddScoped<IDeliverableRepository, DeliverableRepository>();
builder.Services.AddScoped<IDeliverableService, DeliverableService>();
builder.Services.AddScoped<IMessageRepository, MessageRepository>();
builder.Services.AddScoped<IMessageService, MessageService>();
builder.Services.AddScoped<IInvoiceRepository, InvoiceRepository>();
builder.Services.AddScoped<IInvoiceService, InvoiceService>();
builder.Services.AddHttpClient();
builder.Services.AddScoped<IAIReportService, AIReportService>();
builder.Services.AddSingleton<IStripeService, StripeService>();
builder.Services.AddSingleton<IEmailService, SendGridEmailService>();

// ── SignalR ────────────────────────────────────────────────────────────────────
builder.Services.AddSignalR();

// ── JWT authentication ─────────────────────────────────────────────────────────
var jwtSecret = builder.Configuration["JWT_SECRET"]
    ?? throw new InvalidOperationException("JWT_SECRET is not set.");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
            ValidateIssuer = true,
            ValidIssuer = "flowdesk-api",
            ValidateAudience = true,
            ValidAudience = "flowdesk-web",
            ClockSkew = TimeSpan.Zero
        };

        // Read JWT from the httpOnly cookie instead of the Authorization header
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = ctx =>
            {
                ctx.Token = ctx.Request.Cookies["access_token"];
                // Fallback for SignalR WebSocket upgrade (used when cookie is unavailable cross-origin)
                if (string.IsNullOrEmpty(ctx.Token))
                {
                    var qs = ctx.Request.Query["access_token"].ToString();
                    if (!string.IsNullOrEmpty(qs) && ctx.HttpContext.Request.Path.StartsWithSegments("/hubs"))
                        ctx.Token = qs;
                }
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AgencyOnly", p => p.RequireRole("AgencyOwner", "AgencyMember"));
    options.AddPolicy("AgencyOwnerOnly", p => p.RequireRole("AgencyOwner"));
    options.AddPolicy("ClientOnly", p => p.RequireRole("Client"));
});

// ── CORS ───────────────────────────────────────────────────────────────────────
var frontendUrl = builder.Configuration["FRONTEND_URL"] ?? "http://localhost:3000";
builder.Services.AddCors(options =>
    options.AddPolicy("AllowFrontend", policy =>
        policy.WithOrigins(frontendUrl)
              .AllowCredentials()
              .AllowAnyHeader()
              .AllowAnyMethod()));

// ── Rate limiting ──────────────────────────────────────────────────────────────
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    static string IpKey(HttpContext ctx) =>
        ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown";

    static string UserKey(HttpContext ctx) =>
        ctx.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
        ?? ctx.Connection.RemoteIpAddress?.ToString()
        ?? "unknown";

    // Auth endpoints — keyed by IP
    options.AddPolicy("login", ctx => RateLimitPartition.GetFixedWindowLimiter(IpKey(ctx),
        _ => new FixedWindowRateLimiterOptions { PermitLimit = 10, Window = TimeSpan.FromMinutes(1) }));

    options.AddPolicy("register", ctx => RateLimitPartition.GetFixedWindowLimiter(IpKey(ctx),
        _ => new FixedWindowRateLimiterOptions { PermitLimit = 5, Window = TimeSpan.FromMinutes(1) }));

    options.AddPolicy("refresh", ctx => RateLimitPartition.GetFixedWindowLimiter(IpKey(ctx),
        _ => new FixedWindowRateLimiterOptions { PermitLimit = 20, Window = TimeSpan.FromMinutes(1) }));

    options.AddPolicy("accept-invite", ctx => RateLimitPartition.GetFixedWindowLimiter(IpKey(ctx),
        _ => new FixedWindowRateLimiterOptions { PermitLimit = 10, Window = TimeSpan.FromMinutes(1) }));

    // SignalR token — keyed by authenticated user; SignalR reconnects call this frequently
    options.AddPolicy("signalr-token", ctx => RateLimitPartition.GetFixedWindowLimiter(UserKey(ctx),
        _ => new FixedWindowRateLimiterOptions { PermitLimit = 20, Window = TimeSpan.FromMinutes(1) }));

    // AI report — keyed by user so IP rotation can't bypass it; Gemini free tier is precious
    options.AddPolicy("ai-report", ctx => RateLimitPartition.GetFixedWindowLimiter(UserKey(ctx),
        _ => new FixedWindowRateLimiterOptions { PermitLimit = 3, Window = TimeSpan.FromMinutes(1) }));
});

// ── Validation ─────────────────────────────────────────────────────────────────
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddValidatorsFromAssemblyContaining<Program>();

// ── Controllers + OpenAPI ──────────────────────────────────────────────────────
builder.Services.AddControllers()
    .AddJsonOptions(o =>
        o.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter()));
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "FlowDesk API", Version = "v1" });
});

// Only override the URL on Railway where PORT is injected; locally launchSettings.json handles it
var railwayPort = Environment.GetEnvironmentVariable("PORT");
if (railwayPort is not null)
    builder.WebHost.UseUrls($"http://0.0.0.0:{railwayPort}");

var app = builder.Build();

// Trust the X-Forwarded-For header from Railway's proxy so rate limiting uses the real client IP
app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
});

app.UseMiddleware<ExceptionMiddleware>();
app.UseRateLimiter();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.MapScalarApiReference(options =>
        options.OpenApiRoutePattern = "/swagger/v1/swagger.json");
}

app.UseCors("AllowFrontend");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHub<ChatHub>("/hubs/chat");
app.MapHub<ProjectHub>("/hubs/project");

app.Run();
