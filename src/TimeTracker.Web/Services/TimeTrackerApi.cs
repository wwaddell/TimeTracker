using System.Net.Http.Json;
using TimeTracker.Contracts.TimeEntries;

namespace TimeTracker.Web.Services;

/// <summary>Typed wrapper over the TimeTracker Web API.</summary>
public class TimeTrackerApi(HttpClient http)
{
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
}
