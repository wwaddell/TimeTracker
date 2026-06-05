using System.Net.Http.Json;
using TimeTracker.Contracts;
using TimeTracker.Contracts.Admin;
using TimeTracker.Contracts.Calendar;
using TimeTracker.Contracts.Me;
using TimeTracker.Contracts.Members;
using TimeTracker.Contracts.Organizations;
using TimeTracker.Contracts.Projects;
using TimeTracker.Contracts.Rights;
using TimeTracker.Contracts.Roles;
using TimeTracker.Contracts.Tasks;
using TimeTracker.Contracts.TimeEntries;

namespace TimeTracker.Web.Services;

/// <summary>Typed wrapper over the TimeTracker Web API with friendly error handling.</summary>
public class TimeTrackerApi(HttpClient http)
{
    // --- Me / profile ---

    public async Task<MeDto?> GetMeAsync() => await GetAsync<MeDto>("/api/me");

    public async Task<ApiResult> SaveMePreferencesAsync(SaveMePreferencesRequest request) =>
        await SendAsync(() => http.PutAsJsonAsync("/api/me/preferences", request));

    // --- Time logging ---

    public async Task<IReadOnlyList<OrganizationDto>> GetOrganizationsAsync() =>
        await GetAsync<List<OrganizationDto>>("/api/organizations") ?? [];

    public async Task<IReadOnlyList<EntryFieldDto>> GetEntryFieldsAsync(int orgId) =>
        await GetAsync<List<EntryFieldDto>>($"/api/organizations/{orgId}/entry-fields") ?? [];

    public async Task<PagedResult<TimeEntryDto>> GetTimeEntriesAsync(int orgId, int page, int pageSize) =>
        await GetAsync<PagedResult<TimeEntryDto>>(
            $"/api/organizations/{orgId}/time-entries?page={page}&pageSize={pageSize}")
        ?? new PagedResult<TimeEntryDto>([], page, pageSize, 0);

    public async Task<ApiResult> CreateTimeEntryAsync(int orgId, CreateTimeEntryRequest request) =>
        await SendAsync(() => http.PostAsJsonAsync($"/api/organizations/{orgId}/time-entries", request));

    public async Task<ApiResult> UpdateTimeEntryAsync(int orgId, long id, CreateTimeEntryRequest request) =>
        await SendAsync(() => http.PutAsJsonAsync($"/api/organizations/{orgId}/time-entries/{id}", request));

    public async Task<ApiResult> DeleteTimeEntryAsync(int orgId, long id) =>
        await SendAsync(() => http.DeleteAsync($"/api/organizations/{orgId}/time-entries/{id}"));

    // --- Projects ---

    public async Task<IReadOnlyList<ProjectPickerDto>> GetVisibleProjectsAsync(int orgId) =>
        await GetAsync<List<ProjectPickerDto>>($"/api/organizations/{orgId}/projects/visible") ?? [];

    public async Task<IReadOnlyList<ProjectDto>> GetProjectsAsync(int orgId) =>
        await GetAsync<List<ProjectDto>>($"/api/organizations/{orgId}/projects") ?? [];

    public async Task<ApiResult> CreateProjectAsync(int orgId, SaveProjectRequest request) =>
        await SendAsync(() => http.PostAsJsonAsync($"/api/organizations/{orgId}/projects", request));

    public async Task<ApiResult> UpdateProjectAsync(int orgId, int id, SaveProjectRequest request) =>
        await SendAsync(() => http.PutAsJsonAsync($"/api/organizations/{orgId}/projects/{id}", request));

    public async Task<ApiResult> DeleteProjectAsync(int orgId, int id) =>
        await SendAsync(() => http.DeleteAsync($"/api/organizations/{orgId}/projects/{id}"));

    public async Task<IReadOnlyList<ProjectMemberDto>> GetProjectMembersAsync(int orgId, int id) =>
        await GetAsync<List<ProjectMemberDto>>($"/api/organizations/{orgId}/projects/{id}/members") ?? [];

    public async Task<ApiResult> AddProjectMemberAsync(int orgId, int id, AddProjectMemberRequest request) =>
        await SendAsync(() => http.PostAsJsonAsync($"/api/organizations/{orgId}/projects/{id}/members", request));

    public async Task<ApiResult> RemoveProjectMemberAsync(int orgId, int id, int userId) =>
        await SendAsync(() => http.DeleteAsync($"/api/organizations/{orgId}/projects/{id}/members/{userId}"));

    // --- Tasks ---

    public async Task<IReadOnlyList<TaskDto>> GetTasksAsync(int orgId, string? scope = null)
    {
        var url = $"/api/organizations/{orgId}/tasks"
            + (string.IsNullOrEmpty(scope) ? "" : $"?scope={Uri.EscapeDataString(scope)}");
        return await GetAsync<List<TaskDto>>(url) ?? [];
    }

    public async Task<ApiResult> CreateTaskAsync(int orgId, SaveTaskRequest request) =>
        await SendAsync(() => http.PostAsJsonAsync($"/api/organizations/{orgId}/tasks", request));

    public async Task<ApiResult> UpdateTaskAsync(int orgId, int id, SaveTaskRequest request) =>
        await SendAsync(() => http.PutAsJsonAsync($"/api/organizations/{orgId}/tasks/{id}", request));

    public async Task<ApiResult> DeleteTaskAsync(int orgId, int id) =>
        await SendAsync(() => http.DeleteAsync($"/api/organizations/{orgId}/tasks/{id}"));

    // --- Admin: configurable fields ---

    public async Task<IReadOnlyList<OrganizationRoleDto>> GetRolesAsync(int orgId) =>
        await GetAsync<List<OrganizationRoleDto>>($"/api/organizations/{orgId}/roles") ?? [];

    public async Task<IReadOnlyList<EntryFieldAdminDto>> GetAdminFieldsAsync(int orgId) =>
        await GetAsync<List<EntryFieldAdminDto>>($"/api/organizations/{orgId}/admin/entry-fields") ?? [];

    public async Task<ApiResult> CreateFieldAsync(int orgId, SaveEntryFieldRequest request) =>
        await SendAsync(() => http.PostAsJsonAsync($"/api/organizations/{orgId}/admin/entry-fields", request));

    public async Task<ApiResult> UpdateFieldAsync(int orgId, int fieldId, SaveEntryFieldRequest request) =>
        await SendAsync(() => http.PutAsJsonAsync($"/api/organizations/{orgId}/admin/entry-fields/{fieldId}", request));

    public async Task<DeleteFieldResult> DeleteFieldAsync(int orgId, int fieldId)
    {
        HttpResponseMessage response;
        try
        {
            response = await http.DeleteAsync($"/api/organizations/{orgId}/admin/entry-fields/{fieldId}");
        }
        catch (Exception ex)
        {
            return new DeleteFieldResult(false, false, ConnectionError(ex));
        }

        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<DeleteFieldResult>()
                ?? new DeleteFieldResult(true, false, "Field deleted.");
        }

        var problem = await TryReadProblemAsync(response);
        return new DeleteFieldResult(false, false, problem ?? $"Delete failed ({(int)response.StatusCode}).");
    }

    // --- Admin: rights, organization, members, roles ---

    public async Task<IReadOnlyList<RightDto>> GetRightsAsync() =>
        await GetAsync<List<RightDto>>("/api/rights") ?? [];

    public async Task<IReadOnlyList<OrganizationDetailsDto>> GetManageableOrganizationsAsync() =>
        await GetAsync<List<OrganizationDetailsDto>>("/api/organizations/manageable") ?? [];

    public async Task<OrganizationDetailsDto?> GetOrgDetailsAsync(int orgId) =>
        await GetAsync<OrganizationDetailsDto>($"/api/organizations/{orgId}/details");

    public async Task<ApiResult> SaveOrgDetailsAsync(int orgId, SaveOrganizationRequest request) =>
        await SendAsync(() => http.PutAsJsonAsync($"/api/organizations/{orgId}/details", request));

    public async Task<ApiResult> SaveEntrySettingsAsync(int orgId, EntrySettingsRequest request) =>
        await SendAsync(() => http.PutAsJsonAsync($"/api/organizations/{orgId}/entry-settings", request));

    public async Task<IReadOnlyList<MemberDto>> GetMembersAsync(int orgId) =>
        await GetAsync<List<MemberDto>>($"/api/organizations/{orgId}/members") ?? [];

    public async Task<ApiResult> InviteMemberAsync(int orgId, InviteMemberRequest request) =>
        await SendAsync(() => http.PostAsJsonAsync($"/api/organizations/{orgId}/members", request));

    public async Task<ApiResult> SetMemberRolesAsync(int orgId, int userId, SetMemberRolesRequest request) =>
        await SendAsync(() => http.PutAsJsonAsync($"/api/organizations/{orgId}/members/{userId}/roles", request));

    public async Task<ApiResult> RemoveMemberAsync(int orgId, int userId) =>
        await SendAsync(() => http.DeleteAsync($"/api/organizations/{orgId}/members/{userId}"));

    public async Task<IReadOnlyList<RoleAdminDto>> GetAdminRolesAsync(int orgId) =>
        await GetAsync<List<RoleAdminDto>>($"/api/organizations/{orgId}/roles/admin") ?? [];

    public async Task<IReadOnlyList<RoleMemberDto>> GetRoleMembersAsync(int orgId, int roleId) =>
        await GetAsync<List<RoleMemberDto>>($"/api/organizations/{orgId}/roles/{roleId}/members") ?? [];

    public async Task<ApiResult> CreateRoleAsync(int orgId, SaveRoleRequest request) =>
        await SendAsync(() => http.PostAsJsonAsync($"/api/organizations/{orgId}/roles", request));

    public async Task<ApiResult> UpdateRoleAsync(int orgId, int roleId, SaveRoleRequest request) =>
        await SendAsync(() => http.PutAsJsonAsync($"/api/organizations/{orgId}/roles/{roleId}", request));

    public async Task<ApiResult> DeleteRoleAsync(int orgId, int roleId) =>
        await SendAsync(() => http.DeleteAsync($"/api/organizations/{orgId}/roles/{roleId}"));

    // --- Global admin: organization management ---

    public async Task<IReadOnlyList<AdminOrgDto>> GetAllOrganizationsAsync() =>
        await GetAsync<List<AdminOrgDto>>("/api/admin/organizations") ?? [];

    public async Task<ApiResult> CreateOrganizationAsync(CreateOrganizationRequest request) =>
        await SendAsync(() => http.PostAsJsonAsync("/api/admin/organizations", request));

    public async Task<ApiResult> UpdateOrganizationAsync(int orgId, UpdateOrganizationRequest request) =>
        await SendAsync(() => http.PutAsJsonAsync($"/api/admin/organizations/{orgId}", request));

    public async Task<IReadOnlyList<OrgAdminDto>> GetOrgAdminsAsync(int orgId) =>
        await GetAsync<List<OrgAdminDto>>($"/api/admin/organizations/{orgId}/admins") ?? [];

    public async Task<ApiResult> AssignOrgAdminAsync(int orgId, AssignOrgAdminRequest request) =>
        await SendAsync(() => http.PostAsJsonAsync($"/api/admin/organizations/{orgId}/admins", request));

    public async Task<ApiResult> RemoveOrgAdminAsync(int orgId, int userId) =>
        await SendAsync(() => http.DeleteAsync($"/api/admin/organizations/{orgId}/admins/{userId}"));

    // --- Calendar import (Outlook via Graph) ---

    public async Task<CalendarPreviewResult> PreviewCalendarAsync(int orgId, CalendarPreviewRequest request) =>
        await PostAsync<CalendarPreviewResult>($"/api/organizations/{orgId}/calendar/preview", request)
        ?? new CalendarPreviewResult();

    public async Task<CalendarImportResult> ImportCalendarAsync(int orgId, CalendarImportRequest request) =>
        await PostAsync<CalendarImportResult>($"/api/organizations/{orgId}/calendar/import", request)
        ?? new CalendarImportResult(0, 0);

    public async Task<CalendarConnectionStatusDto> GetCalendarConnectionAsync(int orgId) =>
        await GetAsync<CalendarConnectionStatusDto>($"/api/organizations/{orgId}/calendar/connection")
        ?? new CalendarConnectionStatusDto(false, null, false, null);

    public async Task<ConnectUrlResponse?> GetCalendarConnectUrlAsync(int orgId) =>
        await PostAsync<ConnectUrlResponse>($"/api/organizations/{orgId}/calendar/connect-url", new { });

    public async Task<ConnectUrlResponse?> GetCalendarAdminConsentUrlAsync(int orgId) =>
        await GetAsync<ConnectUrlResponse>($"/api/organizations/{orgId}/calendar/admin-consent-url");

    public async Task<ApiResult> DisconnectCalendarAsync(int orgId) =>
        await SendAsync(() => http.DeleteAsync($"/api/organizations/{orgId}/calendar/connection"));

    // --- Plumbing ---

    private async Task<T?> GetAsync<T>(string url)
    {
        HttpResponseMessage response;
        try
        {
            response = await http.GetAsync(url);
        }
        catch (Exception ex)
        {
            throw new ApiException(ConnectionError(ex), ex);
        }

        if (!response.IsSuccessStatusCode)
        {
            var problem = await TryReadProblemAsync(response);
            throw new ApiException(problem ?? ServerError(response));
        }

        try
        {
            return await response.Content.ReadFromJsonAsync<T>();
        }
        catch (Exception ex)
        {
            throw new ApiException("The server sent an unexpected response.", ex);
        }
    }

    // POST that returns a typed body, surfacing failures as ApiException (like GetAsync).
    private async Task<T?> PostAsync<T>(string url, object body)
    {
        HttpResponseMessage response;
        try
        {
            response = await http.PostAsJsonAsync(url, body);
        }
        catch (Exception ex)
        {
            throw new ApiException(ConnectionError(ex), ex);
        }

        if (!response.IsSuccessStatusCode)
        {
            var problem = await TryReadProblemAsync(response);
            throw new ApiException(problem ?? ServerError(response));
        }

        try
        {
            return await response.Content.ReadFromJsonAsync<T>();
        }
        catch (Exception ex)
        {
            throw new ApiException("The server sent an unexpected response.", ex);
        }
    }

    private static async Task<ApiResult> SendAsync(Func<Task<HttpResponseMessage>> send)
    {
        HttpResponseMessage response;
        try
        {
            response = await send();
        }
        catch (Exception ex)
        {
            return new ApiResult(false, ConnectionError(ex));
        }

        if (response.IsSuccessStatusCode)
        {
            return new ApiResult(true, null);
        }

        var problem = await TryReadProblemAsync(response);
        return new ApiResult(false, problem ?? ServerError(response));
    }

    private static string ConnectionError(Exception ex) =>
        ex is TaskCanceledException
            ? "The request to the TimeTracker API timed out. It may be starting up or unreachable."
            : "Couldn't reach the TimeTracker API. Make sure the API is running, then retry.";

    private static string ServerError(HttpResponseMessage response) => (int)response.StatusCode >= 500
        ? "The server hit an error handling the request (this is often the database being unavailable)."
        : $"The request failed ({(int)response.StatusCode} {response.ReasonPhrase}).";

    private static async Task<string?> TryReadProblemAsync(HttpResponseMessage response)
    {
        try
        {
            var problem = await response.Content.ReadFromJsonAsync<ValidationProblemResponse>();
            if (problem?.Errors is { Count: > 0 })
            {
                return string.Join(" ", problem.Errors.SelectMany(e => e.Value));
            }
            return problem?.Detail ?? problem?.Title;
        }
        catch
        {
            return null;
        }
    }

    private record ValidationProblemResponse(string? Title, string? Detail, Dictionary<string, string[]>? Errors);
}

/// <summary>Outcome of a mutating API call, with a user-facing error when it fails.</summary>
public record ApiResult(bool Success, string? Error);

/// <summary>Thrown by read calls when the API is unreachable or returns an error.</summary>
public sealed class ApiException(string message, Exception? inner = null) : Exception(message, inner);
