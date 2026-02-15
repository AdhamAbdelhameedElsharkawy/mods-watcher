using ModsAutomator.Core.Entities;
using ModsAutomator.Desktop.ViewModels;
using ModsAutomator.Services.Interfaces;
using System.Windows;
using System.Windows.Input;

public class AppDialogViewModel : BaseViewModel
{
    private readonly IStorageService _storageService;
    public ModdedApp App { get; }
    public bool IsEditMode { get; }

    // Logic-driven properties for the UI
    public bool CanEditName => !IsEditMode;

    public string Name
    {
        get => App.Name;
        set { App.Name = value; OnPropertyChanged(); }
    }

    public string Description
    {
        get => App.Description;
        set { App.Description = value; OnPropertyChanged(); }
    }

    public string InstalledVersion
    {
        get => App.InstalledVersion;
        set { App.InstalledVersion = value; OnPropertyChanged(); }
    }

    public string LatestVersion
    {
        get => App.LatestVersion;
        set { App.LatestVersion = value; OnPropertyChanged(); }
    }

    public ICommand SaveCommand { get; }
    public ICommand CancelCommand { get; }
    public event Action RequestClose;

    public AppDialogViewModel(IStorageService storageService, ModdedApp? existingApp = null)
    {
        _storageService = storageService;
        IsEditMode = existingApp != null;

        // Use provided app for Edit, or new app for Add
        App = existingApp ?? new ModdedApp { InstalledVersion = "0.0.0", LatestVersion = "0.0.0" };

        SaveCommand = new RelayCommand(async _ => await SaveAsync());
        CancelCommand = new RelayCommand(_ => Close(false));
    }

    private async Task SaveAsync()
    {
        // Implicitly update for both modes
        App.LastUpdatedDate = DateOnly.FromDateTime(DateTime.Now);

        if (IsEditMode)
            await _storageService.UpdateAppAsync(App);
        else
            await _storageService.AddAppAsync(App);

        Close(true);
    }

    private void Close(bool result)
    {
        // Add this guard clause for Unit Tests
        if (Application.Current?.Windows == null) return;

        foreach (Window window in Application.Current.Windows)
        {
            if (window.DataContext == this)
            {
                window.DialogResult = result;
                window.Close();
            }
        }
    }
}