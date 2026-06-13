using TimeTracker.Contracts.Projects;
using TimeTracker.Contracts.Tasks;
using TimeTracker.Contracts.TimeEntries;
using TimeTracker.Mobile.Services;

namespace TimeTracker.Mobile.Pages;

public partial class AddTimeEntryPage : ContentPage
{
    private readonly ApiClient _api;
    private readonly AppState _state;
    private readonly TimeEntryDto? _entry; // null = new

    // Picker backing lists; index 0 is the "none" sentinel so the user can clear a pick.
    private List<ProjectPickerDto> _projects = [];
    private List<TaskDto> _tasks = [];
    private bool _saving;

    public AddTimeEntryPage(ApiClient api, AppState state, TimeEntryDto? entry)
    {
        InitializeComponent();
        _api = api;
        _state = state;
        _entry = entry;

        Title = entry is null ? "Add time entry" : "Edit time entry";
        DeleteButton.IsVisible = entry is not null;
        DatePickerCtl.Date = (entry?.EntryDate ?? DateOnly.FromDateTime(DateTime.Today)).ToDateTime(TimeOnly.MinValue);
        NoteEditor.Text = entry?.Note ?? "";
        if (entry?.DurationMinutes is { } mins)
        {
            DurationEntry.Text = mins.ToString();
        }
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadPickersAsync();
    }

    private async Task LoadPickersAsync()
    {
        try
        {
            _projects = (await _api.GetVisibleProjectsAsync(_state.SelectedOrgId)).ToList();
            _tasks = (await _api.GetTasksAsync(_state.SelectedOrgId)).ToList();
        }
        catch (ApiException ex)
        {
            ShowError(ex.Message);
            return;
        }

        // "(none)" first so the picker can represent no-selection.
        ProjectPicker.ItemsSource = new[] { "(none)" }.Concat(_projects.Select(p => p.Name)).ToList();
        TaskPicker.ItemsSource = new[] { "(none)" }.Concat(_tasks.Select(t => t.Title)).ToList();

        ProjectPicker.SelectedIndex = _entry?.ProjectId is { } pid
            ? _projects.FindIndex(p => p.Id == pid) + 1 // +1 for the sentinel
            : 0;
        TaskPicker.SelectedIndex = _entry?.TaskId is { } tid
            ? _tasks.FindIndex(t => t.Id == tid) + 1
            : 0;
    }

    private async void OnSaveClicked(object? sender, EventArgs e)
    {
        if (_saving)
        {
            return;
        }
        if (string.IsNullOrWhiteSpace(NoteEditor.Text))
        {
            ShowError("A note is required.");
            return;
        }

        int? duration = int.TryParse(DurationEntry.Text, out var d) ? d : null;
        int? projectId = ProjectPicker.SelectedIndex > 0 ? _projects[ProjectPicker.SelectedIndex - 1].Id : null;
        int? taskId = TaskPicker.SelectedIndex > 0 ? _tasks[TaskPicker.SelectedIndex - 1].Id : null;

        var request = new CreateTimeEntryRequest
        {
            Note = NoteEditor.Text.Trim(),
            EntryDate = DateOnly.FromDateTime(DatePickerCtl.Date),
            DurationMinutes = duration,
            ProjectId = projectId,
            TaskId = taskId,
            // Always send the device timezone (IANA on Android/iOS); matches web behavior.
            Timezone = TimeZoneInfo.Local.Id,
        };

        _saving = true;
        SaveButton.Text = "Saving…";
        try
        {
            if (_entry is null)
            {
                await _api.CreateTimeEntryAsync(_state.SelectedOrgId, request);
            }
            else
            {
                await _api.UpdateTimeEntryAsync(_state.SelectedOrgId, _entry.Id, request);
            }
            await Navigation.PopAsync();
        }
        catch (ApiException ex)
        {
            ShowError(ex.Message);
            _saving = false;
            SaveButton.Text = "Save";
        }
    }

    private async void OnDeleteClicked(object? sender, EventArgs e)
    {
        if (_entry is null || !await DisplayAlert("Delete entry", "Delete this time entry?", "Delete", "Cancel"))
        {
            return;
        }
        try
        {
            await _api.DeleteTimeEntryAsync(_state.SelectedOrgId, _entry.Id);
            await Navigation.PopAsync();
        }
        catch (ApiException ex)
        {
            ShowError(ex.Message);
        }
    }

    private void ShowError(string message)
    {
        ErrorLabel.Text = message;
        ErrorLabel.IsVisible = true;
    }
}
