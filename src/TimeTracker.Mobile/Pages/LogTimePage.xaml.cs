using System.Collections.ObjectModel;
using TimeTracker.Contracts.TimeEntries;
using TimeTracker.Mobile.Services;

namespace TimeTracker.Mobile.Pages;

public partial class LogTimePage : ContentPage
{
    private const int PageSize = 50;

    private readonly ApiClient _api;
    private readonly AppState _state;
    private readonly ObservableCollection<TimeEntryDto> _entries = [];
    private bool _initialized;
    private bool _suppressOrgEvent;

    public LogTimePage(ApiClient api, AppState state)
    {
        InitializeComponent();
        _api = api;
        _state = state;
        EntriesView.ItemsSource = _entries;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (!_initialized)
        {
            _initialized = true;
            PopulateOrgPicker();
        }
        await LoadEntriesAsync();
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
        await LoadEntriesAsync();
    }

    private async void OnRefresh(object? sender, EventArgs e)
    {
        await LoadEntriesAsync();
        Refresh.IsRefreshing = false;
    }

    private async Task LoadEntriesAsync()
    {
        if (_state.SelectedOrgId == 0)
        {
            return;
        }
        try
        {
            var page = await _api.GetTimeEntriesAsync(_state.SelectedOrgId, 1, PageSize);
            _entries.Clear();
            foreach (var entry in page.Items)
            {
                _entries.Add(entry);
            }
        }
        catch (ApiException ex)
        {
            await DisplayAlertAsync("Couldn't load entries", ex.Message, "OK");
        }
    }

    private async void OnAddClicked(object? sender, EventArgs e)
    {
        await Navigation.PushAsync(new AddTimeEntryPage(_api, _state, entry: null));
    }

    private async void OnEntrySelected(object? sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is not TimeEntryDto entry)
        {
            return;
        }
        EntriesView.SelectedItem = null; // clear highlight
        await Navigation.PushAsync(new AddTimeEntryPage(_api, _state, entry));
    }
}
