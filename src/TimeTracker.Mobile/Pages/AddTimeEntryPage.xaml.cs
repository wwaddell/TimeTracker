using TimeTracker.Mobile.Data;
using TimeTracker.Mobile.Services;

namespace TimeTracker.Mobile.Pages;

public partial class AddTimeEntryPage : ContentPage
{
    private readonly DataService _data;
    private readonly AppState _state;
    private readonly LocalTimeEntry? _entry; // null = new

    private List<LocalProject> _projects = [];
    private List<LocalTask> _tasks = [];
    private bool _saving;

    public AddTimeEntryPage(DataService data, AppState state, LocalTimeEntry? entry)
    {
        InitializeComponent();
        _data = data;
        _state = state;
        _entry = entry;

        Title = entry is null ? "Add time entry" : "Edit time entry";
        DeleteButton.IsVisible = entry is not null;
        DatePickerCtl.Date = ParseDate(entry?.EntryDate) ?? DateTime.Today;
        NoteEditor.Text = entry?.Note ?? "";
        if (entry?.DurationMinutes is { } mins)
        {
            DurationEntry.Text = mins.ToString();
        }
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        _projects = await _data.GetProjectsAsync(_state.SelectedOrgId);
        _tasks = await _data.GetTasksAsync(_state.SelectedOrgId);

        ProjectPicker.ItemsSource = new[] { "(none)" }.Concat(_projects.Select(p => p.Name)).ToList();
        TaskPicker.ItemsSource = new[] { "(none)" }.Concat(_tasks.Select(t => t.Title)).ToList();

        ProjectPicker.SelectedIndex = _entry?.ProjectId is { } pid
            ? _projects.FindIndex(p => p.Id == pid) + 1
            : 0;
        TaskPicker.SelectedIndex = _entry?.TaskId is { } tid
            ? _tasks.FindIndex(t => t.ServerId == tid) + 1
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

        var project = ProjectPicker.SelectedIndex > 0 ? _projects[ProjectPicker.SelectedIndex - 1] : null;
        var task = TaskPicker.SelectedIndex > 0 ? _tasks[TaskPicker.SelectedIndex - 1] : null;

        var entry = _entry ?? new LocalTimeEntry { OrgId = _state.SelectedOrgId };
        entry.Note = NoteEditor.Text.Trim();
        entry.EntryDate = DateOnly.FromDateTime((DateTime)DatePickerCtl.Date).ToString("yyyy-MM-dd");
        entry.DurationMinutes = int.TryParse(DurationEntry.Text, out var d) ? d : null;
        entry.ProjectId = project?.Id;
        entry.ProjectName = project?.Name;
        // Tasks only link to entries server-side by their server id, so an unsynced task
        // (no ServerId yet) can't be linked until it syncs.
        entry.TaskId = task?.ServerId;
        entry.TaskTitle = task?.Title;
        entry.Timezone = TimeZoneInfo.Local.Id;

        _saving = true;
        SaveButton.Text = "Saving…";
        try
        {
            await _data.SaveTimeEntryAsync(entry);
            await Navigation.PopAsync();
        }
        catch (Exception ex)
        {
            ShowError(ex.Message);
            _saving = false;
            SaveButton.Text = "Save";
        }
    }

    private async void OnDeleteClicked(object? sender, EventArgs e)
    {
        if (_entry is null || !await DisplayAlertAsync("Delete entry", "Delete this time entry?", "Delete", "Cancel"))
        {
            return;
        }
        await _data.DeleteTimeEntryAsync(_entry);
        await Navigation.PopAsync();
    }

    private static DateTime? ParseDate(string? iso) =>
        DateTime.TryParse(iso, out var dt) ? dt : null;

    private void ShowError(string message)
    {
        ErrorLabel.Text = message;
        ErrorLabel.IsVisible = true;
    }
}
