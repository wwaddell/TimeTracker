namespace TimeTracker.Infrastructure.Persistence;

/// <summary>
/// Lets the DbContext attribute audit rows (e.g. <c>t_task_history</c>) to a user
/// without taking a dependency on the API's authentication stack. The API project
/// registers an adapter that bridges to its <c>ICurrentUser</c>; non-HTTP callers
/// (DevData seeding, design-time tooling) use the null implementation that returns
/// no user.
/// </summary>
public interface ICurrentUserProvider
{
    /// <summary>The current user's id, or null when there's no user context (seeding/CLI).</summary>
    Task<int?> GetCurrentUserIdAsync();
}

/// <summary>No-user fallback used when no real provider is registered.</summary>
internal sealed class NullCurrentUserProvider : ICurrentUserProvider
{
    public Task<int?> GetCurrentUserIdAsync() => Task.FromResult<int?>(null);
}
