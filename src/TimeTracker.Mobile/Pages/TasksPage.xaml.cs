using System.Collections.ObjectModel;
using TimeTracker.Mobile.Data;
using TimeTracker.Mobile.Services;

namespace TimeTracker.Mobile.Pages;

public partial class TasksPage : ContentPage
{
    private readonly DataService _data;
    private readonly AppState _state;
    private readonly ObservableCollection<LocalTask> _tasks = [];
    private bool _initialized;
    private bool _suppressOrgEvent;

    public TasksPage(DataService data, AppState state)
    {
        InitializeComponent();
        _data = data;
        _state = state;
        TasksView.ItemsSource = _tasks;
        _data.Changed += OnDataChanged;
    }

    private void OnDataChanged() => MainThread.BeginInvokeOnMainThread(async () => await ReloadAsync());

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (!_initialized)
        {
            _initialized = true;
            PopulateOrgPicker();
        }
        await ReloadAsync();
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
        await ReloadAsync();
        await _data.SyncAsync(_state.SelectedOrgId);
    }

    private async void OnRefresh(object? sender, EventArgs e)
    {
        await _data.SyncAsync(_state.SelectedOrgId);
        await ReloadAsync();
        Refresh.IsRefreshing = false;
    }

    private async void OnSyncClicked(object? sender, EventArgs e)
    {
        await _data.SyncAsync(_state.SelectedOrgId);
        await ReloadAsync();
    }

    private async Task ReloadAsync()
    {
        if (_state.SelectedOrgId == 0)
        {
            return;
        }
        var tasks = await _data.GetTasksAsync(_state.SelectedOrgId);
        // Open tasks first, then by title; completed sink to the bottom.
        _tasks.Clear();
        foreach (var task in tasks.OrderBy(t => t.IsComplete).ThenBy(t => t.Title, StringComparer.OrdinalIgnoreCase))
        {
            _tasks.Add(task);
        }
        await UpdateStatusBarAsync();
    }

    private async Task UpdateStatusBarAsync()
    {
        var pending = await _data.PendingCountAsync();
        var online = DataService.IsOnline;
        if (online && pending == 0)
        {
            StatusBar.IsVisible = false;
            return;
        }
        StatusBar.IsVisible = true;
        StatusLabel.Text = (online, pending) switch
        {
            (false, 0) => "Offline — changes will sync when you're back online.",
            (false, _) => $"Offline — {pending} change(s) waiting to sync.",
            (true, _) => $"{pending} change(s) waiting to sync.",
        };
        SyncButton.IsVisible = online && pending > 0;
    }

    private async void OnAddClicked(object? sender, EventArgs e)
    {
        await Navigation.PushAsync(new EditTaskPage(_data, _state, task: null));
    }

    private async void OnTaskSelected(object? sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is not LocalTask task)
        {
            return;
        }
        TasksView.SelectedItem = null;
        await Navigation.PushAsync(new EditTaskPage(_data, _state, task));
    }
}
