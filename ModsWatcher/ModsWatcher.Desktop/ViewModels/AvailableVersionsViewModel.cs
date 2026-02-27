using Microsoft.Extensions.Logging;
using ModsWatcher.Core.Entities;
using ModsWatcher.Desktop.Interfaces;
using ModsWatcher.Services;
using ModsWatcher.Services.Interfaces;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace ModsWatcher.Desktop.ViewModels
{
    public class AvailableVersionsViewModel : BaseViewModel, IInitializable<(ModItemViewModel? Shell, ModdedApp App)>
    {
        private readonly INavigationService _navigationService;
        private readonly IStorageService _storageService;
        private readonly IDialogService _dialogService;
        private readonly CommonUtils _commonUtils;

        private Mod? _targetShell;
        private ModdedApp _selectedApp;
        private ModItemViewModel? _selectedItem;

        public ObservableCollection<ModVersionGroupViewModel> GroupedAvailableMods { get; set; }

        // --- Commands ---
        public ICommand PromoteCommand { get; }
        public ICommand DeleteSingleCommand { get; }
        public ICommand DeleteSelectedCommand { get; }
        public ICommand BackCommand { get; }

        public ICommand CopyUrlCommand { get; }
        public ICommand OpenUrlCommand { get; }

        public AvailableVersionsViewModel(INavigationService navigationService, IStorageService storageService, 
            IDialogService dialogService, CommonUtils commonUtils, ILogger logger) : base(logger)
        {
            _navigationService = navigationService;
            _storageService = storageService;
            _dialogService = dialogService;
            _commonUtils = commonUtils;

            GroupedAvailableMods = new ObservableCollection<ModVersionGroupViewModel>();

            PromoteCommand = new RelayCommand(async obj => await PromoteAsync(obj as AvailableVersionItemViewModel));
            DeleteSingleCommand = new RelayCommand(async obj => await DeleteAsync(obj as AvailableVersionItemViewModel));
            DeleteSelectedCommand = new RelayCommand(async obj => await DeleteSelectedInGroupAsync(obj as ModVersionGroupViewModel));

            BackCommand = new RelayCommand(_ =>
                _navigationService.NavigateTo<LibraryViewModel, (ModdedApp, ModItemViewModel)>((_selectedApp, _selectedItem)));

            CopyUrlCommand = new RelayCommand(obj => ExecuteCopyUrl(obj as string));
            OpenUrlCommand = new RelayCommand(obj => ExecuteOpenUrl(obj as string));
        }

        public void Initialize((ModItemViewModel? Shell, ModdedApp App) data)
        {
            _selectedItem = data.Shell;
            _targetShell = data.Shell?.Shell;
            _selectedApp = data.App;
            _ = LoadVersions();
        }

        private async Task LoadVersions()
        {
            if (_selectedApp == null) return;

            
            string installedVersion = string.Empty;

            GroupedAvailableMods.Clear();

            // Passing the optional _targetShell?.Id to filter at the DB level if coming from a specific mod
            var results = await _storageService.GetAvailableVersionsByAppIdAsync(_selectedApp.Id, _targetShell?.Id);

            InstalledMod? currentInstalledMod = await _storageService.GetInstalledModsByModIdAsync(_targetShell?.Id);

         installedVersion = currentInstalledMod?.InstalledVersion ?? string.Empty;

            foreach (var (Shell, Versions) in results)
            {
                var group = new ModVersionGroupViewModel(_logger)
                {
                    ModId = Shell.Id,
                    ModName = Shell.Name,
                    RootSourceUrl = Shell.RootSourceUrl,
                    Versions = new ObservableCollection<AvailableVersionItemViewModel>(
                        Versions.Select(v => new AvailableVersionItemViewModel(v, _selectedApp.InstalledVersion, installedVersion, _commonUtils, _logger))
                    )

                    

                };

                GroupedAvailableMods.Add(group);
            }
        }

        private async Task PromoteAsync(AvailableVersionItemViewModel? item)
        {
            if (item == null) return;

            _logger.LogInformation("Attempting to promote Available version {Version} for Mod {ModId} to Installed status.", item.Entity.AvailableVersion, item.Entity.Name);
            if (_dialogService.ShowConfirmation($"Promote version {item.Entity.AvailableVersion} to Installed status?", "Confirm Promotion"))
            {
                // This updates the InstalledMod record in the DB
                await _storageService.PromoteAvailableToInstalledAsync(item.Entity, _selectedApp.InstalledVersion);
                _dialogService.ShowInfo("Promotion successful.", "Success");
            }
        }

        private async Task DeleteAsync(AvailableVersionItemViewModel? item)
        {
            if (item == null) return;

            _logger.LogInformation("Attempting to delete Available version {Version} for Mod {ModId}.", item.Entity.AvailableVersion, item.Entity.Name);
            if (_dialogService.ShowConfirmation("Delete this version entry?", "Confirm Delete"))
            {
                await _storageService.DeleteAvailableModAsync(item.Entity.InternalId);
                await LoadVersions();
            }
        }

        private async Task DeleteSelectedInGroupAsync(ModVersionGroupViewModel? group)
        {
            if (group == null) return;

            _logger.LogInformation("Attempting to delete {Count} selected versions for Mod {ModId}.", group.Versions.Count(v => v.IsSelected), group.ModId);
            var selected = group.Versions.Where(v => v.IsSelected).ToList();
            if (!selected.Any()) return;

            if (_dialogService.ShowConfirmation($"Delete {selected.Count} selected versions for '{group.ModName}'?", "Bulk Delete"))
            {
                var ids = selected.Select(v => v.Entity.InternalId).ToList();
                await _storageService.DeleteAvailableModsBatchAsync(ids);
                await LoadVersions();
            }
        }

        private void ExecuteCopyUrl(string? url)
        {
            if (string.IsNullOrWhiteSpace(url)) return;
            try
            {
                System.Windows.Clipboard.SetText(url);
            }
            catch { /* Handle clipboard access issues if necessary */ }
        }

        private void ExecuteOpenUrl(string? url)
        {
            if (string.IsNullOrWhiteSpace(url)) return;
            try
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = url,
                    UseShellExecute = true
                });
            }
            catch { /* Handle missing protocol handler/browser issues */ }
        }
    }

}