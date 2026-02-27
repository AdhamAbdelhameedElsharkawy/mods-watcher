using Microsoft.Extensions.Logging;
using ModsWatcher.Core.Entities;
using ModsWatcher.Desktop.ViewModels;
using ModsWatcher.Services.Interfaces;
using System.ComponentModel.DataAnnotations;
using System.Windows;
using System.Windows.Input;

public class AppDialogViewModel : BaseViewModel
{
    private readonly IStorageService _storageService;
    public ModdedApp App { get; }
    public bool IsEditMode { get; }

    // Logic-driven properties for the UI
    public bool CanEditName => !IsEditMode;

    [Required(ErrorMessage = "Name is required.")]
    public string Name
    {
        get => App.Name;
        set { 
            App.Name = value;
            ValidateProperty(value);
            OnPropertyChanged(); 
        }
    }

    public string Description
    {
        get => App.Description;
        set { App.Description = value; OnPropertyChanged(); }
    }
    [Required(ErrorMessage = "Installed Version is required.\n Format must be X.X or X.X.X")]
    [RegularExpression(@"^\d+\.\d+(\.\d+)?$", ErrorMessage = "Format must be X.X or X.X.X")]
    public string InstalledVersion
    {
        get => App.InstalledVersion;
        set { 
            App.InstalledVersion = value;
            ValidateProperty(value);
            ValidateVersionComparison();
            OnPropertyChanged(); }
    }
    [Required(ErrorMessage = "Latest Version is required.\n Format must be X.X or X.X.X")]
    [RegularExpression(@"^\d+\.\d+(\.\d+)?$", ErrorMessage = "Format must be X.X or X.X.X")]
    public string LatestVersion
    {
        get => App.LatestVersion;
        set { App.LatestVersion = value; ValidateProperty(value);
            ValidateVersionComparison();
            OnPropertyChanged(); }
    }

    public ICommand SaveCommand { get; }
    public ICommand CancelCommand { get; }
    public event Action RequestClose;

    public AppDialogViewModel(IStorageService storageService, ILogger logger, ModdedApp? existingApp = null):base(logger)
    {
        _storageService = storageService;
        IsEditMode = existingApp != null;

        // Use provided app for Edit, or new app for Add
        App = existingApp ?? new ModdedApp { InstalledVersion = "0.0.0" };

        SaveCommand = new RelayCommand(async _ => await SaveAsync(), _ => !HasErrors);
        CancelCommand = new RelayCommand(_ => Close(false));

        ValidateAll();
    }

    private async Task SaveAsync()
    {
        
        _logger.LogInformation(IsEditMode ? "Updating app: {AppName}" : "Adding new app: {AppName}", App.Name);
        // Implicitly update for both modes
        App.LastUpdatedDate = DateOnly.FromDateTime(DateTime.Now);

        if (!HasErrors)
        {
            if (IsEditMode)
                await _storageService.UpdateAppAsync(App);
            else
                await _storageService.AddAppAsync(App);

            Close(true);
        }
       
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

    private void ValidateVersionComparison()
    {
        const string errorMsg = "Latest version must be\n greater than or equal to installed.";

        // Version.TryParse handles strings like "1.2" or "1.2.3" automatically
        if (Version.TryParse(InstalledVersion, out var vInst) &&
            Version.TryParse(LatestVersion, out var vLatest))
        {
            if (vLatest < vInst)
            {
                AddCustomError(nameof(LatestVersion), errorMsg);
            }
            else
            {
                RemoveCustomError(nameof(LatestVersion), errorMsg);
            }
        }
    }
}