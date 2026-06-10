using System.Reflection;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
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
// Bridge the API's HTTP-aware ICurrentUser into the infrastructure-layer provider so the
// DbContext can attribute audit rows (t_task_history) without referencing this project.
builder.Services.AddScoped<TimeTracker.Infrastructure.Persistence.ICurrentUserProvider,
    TimeTracker.Api.Auth.CurrentUserProviderAdapter>();

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
// Default ON in Development, OFF elsewhere — but explicit opt-in via Auth:UseDevBypass=true
// is honored in any environment. Pragmatic for "prove the prod deploy works" demos before
// real Entra OIDC is wired up. The warmup log below makes the security posture obvious in
// the App Service log stream so it doesn't quietly stay on past intent.
var useDevBypass = builder.Configuration.GetValue("Auth:UseDevBypass", builder.Environment.IsDevelopment());

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

// Apply EF migrations on every startup regardless of environment. Cheap when the DB is up
// to date; the alternative (running `dotnet ef database update` from the pipeline) needs the
// runner to reach Azure SQL and brings its own ops headaches. Dev-only seeding (DevData)
// still gated below so prod doesn't get Acme/Personal sample rows.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<TimeTrackerDbContext>();
    await db.Database.MigrateAsync();
}

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
else if (useDevBypass)
{
    // Outside Development: shout, because this is now a security-relevant override.
    app.Logger.LogWarning(
        "DEV AUTH BYPASS ENABLED IN A {Env} ENVIRONMENT — every request is the stand-in user. " +
        "Disable Auth:UseDevBypass before real production use.",
        app.Environment.EnvironmentName);
}

// In Development the Blazor WASM client calls the API cross-origin over http
// (http://localhost:5130). HTTPS redirection would answer those with a 307 to the
// https port, and the redirect response carries no CORS headers — so the browser
// blocks it. Only enforce HTTPS redirection outside Development.
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

// Hosted Blazor: serve the WASM client's static files from the same App Service.
// UseBlazorFrameworkFiles sets Blazor's base path + MIME types for /_framework/*.
// UseStaticFiles serves everything else from wwwroot. We deliberately do NOT use
// MapStaticAssets here — it's the .NET 9+ endpoint-routed alternative, but it forces
// UseRouting/UseEndpoints insertion that doesn't compose cleanly with our minimal-API
// pipeline (we hit "request reached the end of the pipeline without executing the
// endpoint" with that approach). Static-asset fingerprinting is disabled in the Web
// project csproj so plain filenames (blazor.webassembly.js, etc.) exist on disk.
app.UseBlazorFrameworkFiles();
app.UseStaticFiles();

app.UseCors(WebCorsPolicy);
app.UseAuthentication();
app.UseAuthorization();

// API responses are dynamic JSON — never let the browser serve a cached copy. Without this
// the browser may heuristically cache GETs (and SPAs see stale data after an edit on a
// sibling page, e.g. renaming a project doesn't reflect in pickers until a hard reload).
app.Use(async (ctx, next) =>
{
    if (ctx.Request.Path.StartsWithSegments("/api"))
    {
        ctx.Response.OnStarting(() =>
        {
            ctx.Response.Headers["Cache-Control"] = "no-store";
            return Task.CompletedTask;
        });
    }
    await next();
});

// Deployment stamp for the client's update check. The CI pipeline bakes the commit SHA
// into InformationalVersion for both this assembly and the WASM client in the same
// publish; the client polls this endpoint and offers a reload when the values diverge
// (i.e. a new deployment landed while the app was open). Anonymous + trivially cheap.
var apiVersion = typeof(Program).Assembly
    .GetCustomAttribute<System.Reflection.AssemblyInformationalVersionAttribute>()?
    .InformationalVersion ?? "dev";
app.MapGet("/api/version", () => Results.Ok(new { version = apiVersion })).AllowAnonymous();

app.MapTimeTrackerEndpoints();
app.MapTaskEndpoints();
app.MapAdminEndpoints();
app.MapRightsEndpoints();
app.MapOrganizationEndpoints();
app.MapMemberEndpoints();
app.MapRoleEndpoints();
app.MapCalendarEndpoints();
app.MapGlobalAdminEndpoints();
app.MapMeEndpoints();
app.MapProjectEndpoints();
app.MapReportEndpoints();

// Unknown /api/* routes must 404, not fall through to the SPA below — otherwise a
// client calling an endpoint the server doesn't have yet (deploy version skew) receives
// index.html with a 200 and fails with a confusing JSON-parse error instead of a clean
// "not found".
app.Map("/api/{**rest}", () => Results.NotFound());

// SPA fallback: any other non-static path falls through to index.html so client-side
// routing (Blazor) handles it. Real API endpoints match before the catch-all above.
app.MapFallbackToFile("index.html");

app.Run();
