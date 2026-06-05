using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using TimeTracker.Api;
using TimeTracker.Api.Auth;
using TimeTracker.Api.Calendar;
using TimeTracker.Api.Endpoints;
using TimeTracker.Infrastructure;
using TimeTracker.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

const string WebCorsPolicy = "WebClient";

builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, CurrentUser>();

// Calendar import reads Outlook via Microsoft Graph, server-to-server, with a per-call token.
builder.Services.AddHttpClient<ICalendarSource, GraphCalendarSource>(client =>
    client.BaseAddress = new Uri("https://graph.microsoft.com/v1.0/"));

// "Connect Outlook" OAuth: app registration settings, the token client, and the per-user
// token provider. Data Protection encrypts refresh tokens at rest + protects OAuth state.
builder.Services.Configure<GraphOptions>(builder.Configuration.GetSection(GraphOptions.SectionName));
builder.Services.AddDataProtection();
builder.Services.AddHttpClient<OutlookOAuthClient>();
builder.Services.AddScoped<ICalendarTokenProvider, CalendarTokenProvider>();

builder.Services.AddCors(options =>
{
    options.AddPolicy(WebCorsPolicy, policy =>
    {
        // Blazor WASM dev origins. Tighten/replace with config for non-dev hosting.
        policy.WithOrigins("http://localhost:5008", "https://localhost:7265")
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

// --- Authentication ---
// Real path: Entra External ID JWT bearer (config: Auth:Authority / Auth:Audience).
// Dev path: a backdoor scheme that authenticates as a stand-in user so the app can be
// driven without a real login. Only available in Development, opt-out via Auth:UseDevBypass=false.
var useDevBypass = builder.Environment.IsDevelopment()
    && builder.Configuration.GetValue("Auth:UseDevBypass", true);

var defaultScheme = useDevBypass ? DevAuthHandler.SchemeName : JwtBearerDefaults.AuthenticationScheme;
var authBuilder = builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = defaultScheme;
    options.DefaultChallengeScheme = defaultScheme;
});

if (useDevBypass)
{
    authBuilder.AddScheme<AuthenticationSchemeOptions, DevAuthHandler>(DevAuthHandler.SchemeName, _ => { });
}
else
{
    authBuilder.AddJwtBearer(options =>
    {
        options.Authority = builder.Configuration["Auth:Authority"];
        options.Audience = builder.Configuration["Auth:Audience"];
        options.MapInboundClaims = false; // keep original claim names (sub, oid, roles, ...)
        options.TokenValidationParameters.NameClaimType = "name";
        options.TokenValidationParameters.RoleClaimType = "roles"; // Entra app roles
    });
}

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("Admin", policy => policy.RequireRole("admin"));
});

var app = builder.Build();

// Return clean ProblemDetails JSON for unhandled exceptions (instead of an HTML error page),
// so the client can show a useful message.
app.UseExceptionHandler();
app.UseStatusCodePages();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();

    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<TimeTrackerDbContext>();
    await DevData.EnsureSeededAsync(db);

    if (useDevBypass)
    {
        app.Logger.LogWarning("DEV AUTH BYPASS ENABLED — all requests authenticate as a stand-in user.");
    }
}

// In Development the Blazor WASM client calls the API cross-origin over http
// (http://localhost:5130). HTTPS redirection would answer those with a 307 to the
// https port, and the redirect response carries no CORS headers — so the browser
// blocks it. Only enforce HTTPS redirection outside Development.
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseCors(WebCorsPolicy);
app.UseAuthentication();
app.UseAuthorization();

app.MapTimeTrackerEndpoints();
app.MapTaskEndpoints();
app.MapAdminEndpoints();
app.MapRightsEndpoints();
app.MapOrganizationEndpoints();
app.MapMemberEndpoints();
app.MapRoleEndpoints();
app.MapCalendarEndpoints();
app.MapGlobalAdminEndpoints();

app.Run();
