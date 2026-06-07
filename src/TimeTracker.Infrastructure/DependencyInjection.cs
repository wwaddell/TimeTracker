using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TimeTracker.Infrastructure.Persistence;

namespace TimeTracker.Infrastructure;

public static class DependencyInjection
{
    /// <summary>
    /// Registers the EF Core context against SQL Server. Resolves the connection string
    /// from the "TimeTracker" connection string, falling back to local LocalDB for dev.
    /// </summary>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("TimeTracker")
            ?? TimeTrackerDbContextFactory.DefaultConnectionString;

        services.AddDbContext<TimeTrackerDbContext>(options =>
            options.UseSqlServer(connectionString, sql =>
                sql.MigrationsAssembly(typeof(TimeTrackerDbContext).Assembly.FullName)));

        // Audit attribution: callers (the API) replace this with a real provider that
        // pulls the user from HttpContext. AddScoped here is the default — TryAddScoped
        // would also work; using Add so registrations later in the chain can override.
        services.AddScoped<ICurrentUserProvider, NullCurrentUserProvider>();

        return services;
    }
}
