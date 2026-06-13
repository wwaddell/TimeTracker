using System.Collections.ObjectModel;
using TimeTracker.Contracts.Tasks;
using TimeTracker.Mobile.Services;

namespace TimeTracker.Mobile.Pages;

public partial class TasksPage : ContentPage
{
    private readonly ApiClient _api;
    private readonly AppState _state;
    private readonly ObservableCollection<TaskDto> _tasks = [];
    private bool _initialized;
    private bool _suppressOrgEvent;

    public TasksPage(ApiClient api, AppState state)
    {
        InitializeComponent();
        _api = api;
        _state = state;
        TasksView.ItemsSource = _tasks;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (!_initialized)
        {
            _initialized = true;
            PopulateOrgPicker();
        }
        await LoadTasksAsync();
    }

    private void PopulateOrgPicker()
    {
        _suppressOrgEvent = true;
        OrgPicker.ItemsSource = _state.Organizations.Select(o => o.Name).ToList();
        OrgPicker.IsVisible = _state.Organizations.Count > 1;
        var idx = _state.Organizations.ToList().FindIndex(o => o.Id == _state.SelectedOrgId);
        OrgPicker.SelectedIndex = idx < 0 ? 0 : idx;
        _suppressOrgEvent = false;
    }

    private async void OnOrgChanged(object? sender, EventArgs e)
    {
        if (_suppressOrgEvent || OrgPicker.SelectedIndex < 0)
        {
            return;
        }
        _state.SelectedOrgId = _state.Organizations[OrgPicker.SelectedIndex].Id;
        await LoadTasksAsync();
    }

    private async void OnRefresh(object? sender, EventArgs e)
    {
        await LoadTasksAsync();
        Refresh.IsRefreshing = false;
    }

    private async Task LoadTasksAsync()
    {
        if (_state.SelectedOrgId == 0)
        {
            return;
        }
        try
        {
            var tasks = await _api.GetTasksAsync(_state.SelectedOrgId);
            _tasks.Clear();
            foreach (var task in tasks)
            {
                _tasks.Add(task);
            }
        }
        catch (ApiException ex)
        {
            await DisplayAlertAsync("Couldn't load tasks", ex.Message, "OK");
        }
    }

    private async void OnAddClicked(object? sender, EventArgs e)
    {
        await Navigation.PushAsync(new EditTaskPage(_api, _state, task: null));
    }

    private async void OnTaskSelected(object? sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is not TaskDto task)
        {
            return;
        }
        TasksView.SelectedItem = null;
        await Navigation.PushAsync(new EditTaskPage(_api, _state, task));
    }
}
