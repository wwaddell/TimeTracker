using TimeTracker.Domain.Enums;
using TimeTracker.Mobile.Data;
using TimeTracker.Mobile.Services;

namespace TimeTracker.Mobile.Pages;

public partial class EditTaskPage : ContentPage
{
    private static readonly TaskPriority[] Priorities =
        [TaskPriority.Low, TaskPriority.Medium, TaskPriority.High, TaskPriority.Urgent];

    private readonly DataService _data;
    private readonly AppState _state;
    private readonly LocalTask? _task; // null = new

    private List<LocalProject> _projects = [];
    private int _percent;
    private bool _saving;
    private bool _dueDateCleared;

    public EditTaskPage(DataService data, AppState state, LocalTask? task)
    {
        InitializeComponent();
        _data = data;
        _state = state;
        _task = task;

        Title = task is null ? "New task" : "Edit task";
        LogTimeButton.IsVisible = task is not null;

        TitleEntry.Text = task?.Title ?? "";
        DescriptionEditor.Text = task?.Description ?? "";
        _percent = task?.PercentComplete ?? 0;
        PercentSlider.Value = _percent;
        PercentLabel.Text = $"{_percent}%";
        CompleteSwitch.IsToggled = task?.IsComplete ?? false;
        DueDatePicker.Date = ParseDate(task?.DueDate) ?? DateTime.Today;

        PriorityPicker.ItemsSource = new[] { "(none)" }.Concat(Priorities.Select(p => p.ToString())).ToList();
        PriorityPicker.SelectedIndex = task?.Priority is { } pr ? Array.IndexOf(Priorities, (TaskPriority)pr) + 1 : 0;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        _projects = await _data.GetProjectsAsync(_state.SelectedOrgId);
        ProjectPicker.ItemsSource = new[] { "(none)" }.Concat(_projects.Select(p => p.Name)).ToList();
        ProjectPicker.SelectedIndex = _task?.ProjectId is { } pid ? _projects.FindIndex(p => p.Id == pid) + 1 : 0;
    }

    private void OnPercentChanged(object? sender, ValueChangedEventArgs e)
    {
        _percent = (int)Math.Round(e.NewValue);
        PercentLabel.Text = $"{_percent}%";
        if (_percent >= 100 && !CompleteSwitch.IsToggled)
        {
            CompleteSwitch.IsToggled = true;
        }
        else if (_percent < 100 && CompleteSwitch.IsToggled)
        {
            CompleteSwitch.IsToggled = false;
        }
    }

    private void OnCompleteToggled(object? sender, ToggledEventArgs e)
    {
        if (e.Value)
        {
            PercentSlider.Value = 100;
        }
    }

    private void OnClearDueDate(object? sender, EventArgs e) => _dueDateCleared = true;

    private async void OnSaveClicked(object? sender, EventArgs e)
    {
        if (_saving)
        {
            return;
        }
        if (string.IsNullOrWhiteSpace(TitleEntry.Text))
        {
            ShowError("A title is required.");
            return;
        }

        var complete = CompleteSwitch.IsToggled;
        var project = ProjectPicker.SelectedIndex > 0 ? _projects[ProjectPicker.SelectedIndex - 1] : null;
        int? priority = PriorityPicker.SelectedIndex > 0 ? (int)Priorities[PriorityPicker.SelectedIndex - 1] : null;
        string? dueDate = _dueDateCleared ? null : DateOnly.FromDateTime((DateTime)DueDatePicker.Date).ToString("yyyy-MM-dd");

        var task = _task ?? new LocalTask { OrgId = _state.SelectedOrgId };
        task.Title = TitleEntry.Text.Trim();
        task.Description = string.IsNullOrWhiteSpace(DescriptionEditor.Text) ? null : DescriptionEditor.Text.Trim();
        task.IsComplete = complete;
        task.PercentComplete = complete ? 100 : _percent;
        task.PercentBeforeComplete = complete ? (_task?.PercentComplete ?? _percent) : null;
        task.Priority = priority;
        task.DueDate = dueDate;
        task.ProjectId = project?.Id;
        task.ProjectName = project?.Name;
        // AssignedToUserId 0 = server defaults to current user (new) / leaves unchanged (edit).

        _saving = true;
        SaveButton.Text = "Saving…";
        try
        {
            await _data.SaveTaskAsync(task);
            await Navigation.PopAsync();
        }
        catch (Exception ex)
        {
            ShowError(ex.Message);
            _saving = false;
            SaveButton.Text = "Save";
        }
    }

    private async void OnLogTimeClicked(object? sender, EventArgs e)
    {
        await Navigation.PushAsync(new AddTimeEntryPage(_data, _state, entry: null));
    }

    private static DateTime? ParseDate(string? iso) => DateTime.TryParse(iso, out var dt) ? dt : null;

    private void ShowError(string message)
    {
        ErrorLabel.Text = message;
        ErrorLabel.IsVisible = true;
    }
}
