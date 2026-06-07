using Microsoft.Extensions.DependencyInjection;
using TimeTracker.Infrastructure.Persistence;

namespace TimeTracker.Api.Auth;

/// <summary>
/// Adapts the API's <see cref="ICurrentUser"/> (HTTP-aware) to the infrastructure-layer
/// <see cref="ICurrentUserProvider"/> the DbContext uses for audit attribution.
/// </summary>
/// <remarks>
/// Resolves <see cref="ICurrentUser"/> from the request scope lazily (via IServiceProvider)
/// instead of taking it as a constructor dependency. This is necessary because CurrentUser
/// depends on the DbContext for user lookup — taking it directly here would create a
/// DbContext → ICurrentUserProvider → ICurrentUser → DbContext DI cycle that fails the
/// container's validation at startup. By the time SaveChanges fires, the user id is already
/// cached by CurrentUser from the earlier auth checks in the request, so the resolve is cheap.
/// </remarks>
public class CurrentUserProviderAdapter(IServiceProvider services) : ICurrentUserProvider
{
    public async Task<int?> GetCurrentUserIdAsync()
    {
        try
        {
            var currentUser = services.GetService<ICurrentUser>();
            return currentUser is null ? null : await currentUser.GetUserIdAsync();
        }
        catch
        {
            // No HTTP user context (e.g. background job, dev-time seeding) — leave the audit
            // row's ChangedByUserId null instead of failing the save.
            return null;
        }
    }
}
