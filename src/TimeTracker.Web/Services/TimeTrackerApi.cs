using System.Net.Http.Json;
using TimeTracker.Contracts.Admin;
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

    public async Task<IReadOnlyList<TimeEntryDto>> GetTimeEntriesAsync(int orgId) =>
        await http.GetFromJsonAsync<List<TimeEntryDto>>($"/api/organizations/{orgId}/time-entries") ?? [];

    public async Task<bool> CreateTimeEntryAsync(int orgId, CreateTimeEntryRequest request)
    {
        var response = await http.PostAsJsonAsync($"/api/organizations/{orgId}/time-entries", request);
        return response.IsSuccessStatusCode;
    }

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
