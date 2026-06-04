using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace TimeTracker.Infrastructure.Persistence;

/// <summary>
/// Enables <c>dotnet ef</c> tooling to construct the context at design time without
/// running the host. Uses the <c>TIMETRACKER_CONNECTION</c> environment variable when
/// set, otherwise the local SQL Server default instance.
/// </summary>
public class TimeTrackerDbContextFactory : IDesignTimeDbContextFactory<TimeTrackerDbContext>
{
    public const string DefaultConnectionString =
        @"Server=localhost;Database=TimeTracker;Trusted_Connection=True;TrustServerCertificate=True;";

    public TimeTrackerDbContext CreateDbContext(string[] args)
    {
        var connectionString =
            Environment.GetEnvironmentVariable("TIMETRACKER_CONNECTION") ?? DefaultConnectionString;

        var options = new DbContextOptionsBuilder<TimeTrackerDbContext>()
            .UseSqlServer(connectionString, sql => sql.MigrationsAssembly(typeof(TimeTrackerDbContext).Assembly.FullName))
            .Options;

        return new TimeTrackerDbContext(options);
    }
}
