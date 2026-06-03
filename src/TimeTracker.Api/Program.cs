using TimeTracker.Api;
using TimeTracker.Api.Endpoints;
using TimeTracker.Infrastructure;
using TimeTracker.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

const string WebCorsPolicy = "WebClient";

builder.Services.AddOpenApi();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddCors(options =>
{
    options.AddPolicy(WebCorsPolicy, policy =>
    {
        // Blazor WASM dev origins. Tighten/replace with config for non-dev hosting.
        policy.WithOrigins(
                "http://localhost:5008", "https://localhost:7265")
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();

    // Apply migrations and seed the local dev user/org/fields.
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<TimeTrackerDbContext>();
    await DevData.EnsureSeededAsync(db);
}

app.UseHttpsRedirection();
app.UseCors(WebCorsPolicy);

app.MapTimeTrackerEndpoints();

app.Run();
