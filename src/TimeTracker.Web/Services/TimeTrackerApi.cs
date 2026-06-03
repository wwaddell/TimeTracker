using System.Net.Http.Json;
using TimeTracker.Contracts;
using TimeTracker.Contracts.Admin;
using TimeTracker.Contracts.Tasks;
using TimeTracker.Contracts.TimeEntries;

namespace TimeTracker.Web.Services;

/// <summary>Typed wrapper over the TimeTracker Web API.</summary>
public class TimeTrackerApi(HttpClient http)
{
    // --- Time logging ---

    public async Task<IReadOnlyList<OrganizationDto>> GetOrganizationsAsync() =>
        await http.GetFromJsonAsync<List<OrganizationDto>>("/api/organizations") ?? [];

    public async Task<IReadOnlyList<EntryFieldDto>> GetEntryFieldsAsync(int orgId) =>
        await http.GetFromJsonAsync<List<EntryFieldDto>>($"/api/organizations/{orgId}/entry-fields") ?? [];

    public async Task<PagedResult<TimeEntryDto>> GetTimeEntriesAsync(int orgId, int page, int pageSize) =>
        await http.GetFromJsonAsync<PagedResult<TimeEntryDto>>(
            $"/api/organizations/{orgId}/time-entries?page={page}&pageSize={pageSize}")
        ?? new PagedResult<TimeEntryDto>([], page, pageSize, 0);

    public async Task<ApiResult> CreateTimeEntryAsync(int orgId, CreateTimeEntryRequest request) =>
        await SendAsync(() => http.PostAsJsonAsync($"/api/organizations/{orgId}/time-entries", request));

    public async Task<ApiResult> UpdateTimeEntryAsync(int orgId, long id, CreateTimeEntryRequest request) =>
        await SendAsync(() => http.PutAsJsonAsync($"/api/organizations/{orgId}/time-entries/{id}", request));

    public async Task<ApiResult> DeleteTimeEntryAsync(int orgId, long id) =>
        await SendAsync(() => http.DeleteAsync($"/api/organizations/{orgId}/time-entries/{id}"));

    // --- Tasks ---

    public async Task<IReadOnlyList<TaskDto>> GetTasksAsync(int orgId) =>
        await http.GetFromJsonAsync<List<TaskDto>>($"/api/organizations/{orgId}/tasks") ?? [];

    public async Task<ApiResult> CreateTaskAsync(int orgId, SaveTaskRequest request) =>
        await SendAsync(() => http.PostAsJsonAsync($"/api/organizations/{orgId}/tasks", request));

    public async Task<ApiResult> UpdateTaskAsync(int orgId, int id, SaveTaskRequest request) =>
        await SendAsync(() => http.PutAsJsonAsync($"/api/organizations/{orgId}/tasks/{id}", request));

    public async Task<ApiResult> DeleteTaskAsync(int orgId, int id) =>
        await SendAsync(() => http.DeleteAsync($"/api/organizations/{orgId}/tasks/{id}"));

    // --- Admin: configurable fields ---

    public async Task<IReadOnlyList<OrganizationRoleDto>> GetRolesAsync(int orgId) =>
        await http.GetFromJsonAsync<List<OrganizationRoleDto>>($"/api/organizations/{orgId}/roles") ?? [];

    public async Task<IReadOnlyList<EntryFieldAdminDto>> GetAdminFieldsAsync(int orgId) =>
        await http.GetFromJsonAsync<List<EntryFieldAdminDto>>($"/api/organizations/{orgId}/admin/entry-fields") ?? [];

    public async Task<ApiResult> CreateFieldAsync(int orgId, SaveEntryFieldRequest request) =>
        await SendAsync(() => http.PostAsJsonAsync($"/api/organizations/{orgId}/admin/entry-fields", request));

    public async Task<ApiResult> UpdateFieldAsync(int orgId, int fieldId, SaveEntryFieldRequest request) =>
        await SendAsync(() => http.PutAsJsonAsync($"/api/organizations/{orgId}/admin/entry-fields/{fieldId}", request));

    public async Task<DeleteFieldResult> DeleteFieldAsync(int orgId, int fieldId)
    {
        var response = await http.DeleteAsync($"/api/organizations/{orgId}/admin/entry-fields/{fieldId}");
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<DeleteFieldResult>()
                ?? new DeleteFieldResult(true, false, "Field deleted.");
        }

        var problem = await TryReadProblemAsync(response);
        return new DeleteFieldResult(false, false, problem ?? $"Delete failed ({(int)response.StatusCode}).");
    }

    private static async Task<ApiResult> SendAsync(Func<Task<HttpResponseMessage>> send)
    {
        var response = await send();
        if (response.IsSuccessStatusCode)
        {
            return new ApiResult(true, null);
        }

        var problem = await TryReadProblemAsync(response);
        return new ApiResult(false, problem ?? $"Request failed ({(int)response.StatusCode}).");
    }

    private static async Task<string?> TryReadProblemAsync(HttpResponseMessage response)
    {
        try
        {
            var problem = await response.Content.ReadFromJsonAsync<ValidationProblemResponse>();
            if (problem?.Errors is { Count: > 0 })
            {
                return string.Join(" ", problem.Errors.SelectMany(e => e.Value));
            }
            return problem?.Title;
        }
        catch
        {
            return null;
        }
    }

    private record ValidationProblemResponse(string? Title, Dictionary<string, string[]>? Errors);
}

/// <summary>Outcome of a mutating API call, with a user-facing error when it fails.</summary>
public record ApiResult(bool Success, string? Error);
