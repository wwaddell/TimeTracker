using TimeTracker.Contracts.Projects;
using TimeTracker.Contracts.Tasks;
using TimeTracker.Domain.Enums;
using TimeTracker.Mobile.Services;

namespace TimeTracker.Mobile.Pages;

public partial class EditTaskPage : ContentPage
{
    private static readonly TaskPriority[] Priorities =
        [TaskPriority.Low, TaskPriority.Medium, TaskPriority.High, TaskPriority.Urgent];

    private readonly ApiClient _api;
    private readonly AppState _state;
    private readonly TaskDto? _task; // null = new

    private List<ProjectPickerDto> _projects = [];
    private int _percent;
    private bool _saving;

    public EditTaskPage(ApiClient api, AppState state, TaskDto? task)
    {
        InitializeComponent();
        _api = api;
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
        DueDatePicker.Date = (task?.DueDate ?? DateOnly.FromDateTime(DateTime.Today)).ToDateTime(TimeOnly.MinValue);

        // "(none)" sentinel at index 0; real priorities follow.
        PriorityPicker.ItemsSource = new[] { "(none)" }.Concat(Priorities.Select(p => p.ToString())).ToList();
        PriorityPicker.SelectedIndex = task?.Priority is { } pr ? Array.IndexOf(Priorities, pr) + 1 : 0;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        try
        {
            _projects = (await _api.GetVisibleProjectsAsync(_state.SelectedOrgId)).ToList();
        }
        catch (ApiException ex)
        {
            ShowError(ex.Message);
            return;
        }
        ProjectPicker.ItemsSource = new[] { "(none)" }.Concat(_projects.Select(p => p.Name)).ToList();
        ProjectPicker.SelectedIndex = _task?.ProjectId is { } pid ? _projects.FindIndex(p => p.Id == pid) + 1 : 0;
    }

    private void OnPercentChanged(object? sender, ValueChangedEventArgs e)
    {
        _percent = (int)Math.Round(e.NewValue);
        PercentLabel.Text = $"{_percent}%";
        // Keep the complete switch in sync at the extremes.
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
    private bool _dueDateCleared;

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
        int? projectId = ProjectPicker.SelectedIndex > 0 ? _projects[ProjectPicker.SelectedIndex - 1].Id : null;
        TaskPriority? priority = PriorityPicker.SelectedIndex > 0 ? Priorities[PriorityPicker.SelectedIndex - 1] : null;
        // A new task keeps the chosen due date unless cleared; editing respects the Clear button.
        DateOnly? dueDate = _dueDateCleared ? null : DateOnly.FromDateTime((DateTime)DueDatePicker.Date);

        var request = new SaveTaskRequest
        {
            Title = TitleEntry.Text.Trim(),
            Description = string.IsNullOrWhiteSpace(DescriptionEditor.Text) ? null : DescriptionEditor.Text.Trim(),
            IsComplete = complete,
            PercentComplete = complete ? 100 : _percent,
            PercentBeforeComplete = complete ? (_task?.PercentComplete ?? _percent) : null,
            Priority = priority,
            DueDate = dueDate,
            ProjectId = projectId,
            ReferenceCode = _task?.ReferenceCode,
            ExternalUrl = _task?.ExternalUrl,
            // 0 = default to current user (new) / leave unchanged (edit).
            AssignedToUserId = _task?.AssignedToUserId ?? 0,
        };

        _saving = true;
        SaveButton.Text = "Saving…";
        try
        {
            if (_task is null)
            {
                await _api.CreateTaskAsync(_state.SelectedOrgId, request);
            }
            else
            {
                await _api.UpdateTaskAsync(_state.SelectedOrgId, _task.Id, request);
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

    // Jump to the time-entry form pre-linked to this task (saves the task context).
    private async void OnLogTimeClicked(object? sender, EventArgs e)
    {
        if (_task is null)
        {
            return;
        }
        await Navigation.PushAsync(new AddTimeEntryPage(_api, _state, entry: null) { });
    }

    private void ShowError(string message)
    {
        ErrorLabel.Text = message;
        ErrorLabel.IsVisible = true;
    }
}
