using System.Net;
using System.Net.Http.Json;
using TimeTracker.Contracts.Me;
using TimeTracker.Contracts.Projects;
using TimeTracker.Contracts.Tasks;
using TimeTracker.Contracts.TimeEntries;

namespace TimeTracker.Mobile.Services;

/// <summary>Raised when an API call fails; carries a user-friendly message and whether it was a 401.</summary>
public class ApiException(string message, bool unauthorized = false) : Exception(message)
{
    public bool Unauthorized { get; } = unauthorized;
}

/// <summary>
/// Typed client over the subset of the TimeTracker API the mobile app needs: profile,
/// organizations, time entries, tasks, and project/field pickers. Mirrors the relevant
/// slice of the web client. The injected HttpClient already carries the bearer token via
/// <see cref="AuthHttpMessageHandler"/>.
/// </summary>
public class ApiClient(HttpClient http)
{
    public Task<MeDto?> GetMeAsync() => GetAsync<MeDto>("/api/me");

    public async Task<IReadOnlyList<OrganizationDto>> GetOrganizationsAsync() =>
        await GetAsync<List<OrganizationDto>>("/api/organizations") ?? [];

    public async Task<IReadOnlyList<ProjectPickerDto>> GetVisibleProjectsAsync(int orgId) =>
        await GetAsync<List<ProjectPickerDto>>($"/api/organizations/{orgId}/projects/visible") ?? [];

    public async Task<IReadOnlyList<EntryFieldDto>> GetEntryFieldsAsync(int orgId) =>
        await GetAsync<List<EntryFieldDto>>($"/api/organizations/{orgId}/entry-fields") ?? [];

    // --- Time entries ---

    public async Task<TimeEntriesPage> GetTimeEntriesAsync(int orgId, int page, int pageSize)
    {
        var url = $"/api/organizations/{orgId}/time-entries?page={page}&pageSize={pageSize}";
        return await GetAsync<TimeEntriesPage>(url) ?? new TimeEntriesPage([], page, pageSize, false, []);
    }

    public Task CreateTimeEntryAsync(int orgId, CreateTimeEntryRequest request) =>
        SendAsync(() => http.PostAsJsonAsync($"/api/organizations/{orgId}/time-entries", request));

    public Task UpdateTimeEntryAsync(int orgId, long id, CreateTimeEntryRequest request) =>
        SendAsync(() => http.PutAsJsonAsync($"/api/organizations/{orgId}/time-entries/{id}", request));

    public Task DeleteTimeEntryAsync(int orgId, long id) =>
        SendAsync(() => http.DeleteAsync($"/api/organizations/{orgId}/time-entries/{id}"));

    // --- Tasks ---

    public async Task<IReadOnlyList<TaskDto>> GetTasksAsync(int orgId, string? scope = null)
    {
        var url = $"/api/organizations/{orgId}/tasks" + (scope is null ? "" : $"?scope={scope}");
        return await GetAsync<List<TaskDto>>(url) ?? [];
    }

    public Task CreateTaskAsync(int orgId, SaveTaskRequest request) =>
        SendAsync(() => http.PostAsJsonAsync($"/api/organizations/{orgId}/tasks", request));

    public Task UpdateTaskAsync(int orgId, int id, SaveTaskRequest request) =>
        SendAsync(() => http.PutAsJsonAsync($"/api/organizations/{orgId}/tasks/{id}", request));

    // --- helpers ---

    private async Task<T?> GetAsync<T>(string url)
    {
        HttpResponseMessage response;
        try
        {
            response = await http.GetAsync(url);
        }
        catch (Exception ex)
        {
            throw new ApiException($"Couldn't reach the server. Check your connection. ({ex.Message})");
        }

        EnsureOk(response);
        try
        {
            return await response.Content.ReadFromJsonAsync<T>();
        }
        catch (Exception ex)
        {
            throw new ApiException($"The server sent an unexpected response. ({ex.Message})");
        }
    }

    private async Task SendAsync(Func<Task<HttpResponseMessage>> send)
    {
        HttpResponseMessage response;
        try
        {
            response = await send();
        }
        catch (Exception ex)
        {
            throw new ApiException($"Couldn't reach the server. Check your connection. ({ex.Message})");
        }
        EnsureOk(response);
    }

    private static void EnsureOk(HttpResponseMessage response)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }
        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            throw new ApiException("Your session expired. Please sign in again.", unauthorized: true);
        }
        if (response.StatusCode == HttpStatusCode.Forbidden)
        {
            throw new ApiException("You don't have permission to do that.");
        }
        throw new ApiException($"The request failed ({(int)response.StatusCode}).");
    }
}
