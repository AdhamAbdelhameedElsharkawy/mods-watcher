using Microsoft.Extensions.Logging;
using ModsWatcher.Core.Entities;
using ModsWatcher.Desktop.Interfaces;
using ModsWatcher.Services;
using ModsWatcher.Services.Interfaces;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace ModsWatcher.Desktop.ViewModels
{
    public class ModHistoryViewModel : BaseViewModel, IInitializable<(ModItemViewModel Mod, ModdedApp App)>
    {
        private readonly INavigationService _navigationService;
        private readonly IStorageService _storageService;
        private readonly IDialogService _dialogService;
        private readonly CommonUtils _commonUtils;
        private ModItemViewModel _selectedItem;
        private Mod _mod;
        private ModdedApp _parentApp;
        private string _selectedModName = "Mod History";
        private bool _overrideRollbackRules;


        public ObservableCollection<ModHistoryItemViewModel> HistoryItems { get; set; }

        public bool HasHistory => HistoryItems.Count > 0;

        public string SelectedModName
        {
            get => _selectedModName;
            set
            {
                _selectedModName = value;
                OnPropertyChanged();
            }
        }

        public bool OverrideRollbackRules
        {
            get => _overrideRollbackRules;
            set
            {
                if (SetProperty(ref _overrideRollbackRules, value))
                {
                    // When the checkbox changes, tell all wrappers to re-check their button state
                    foreach (var item in HistoryItems) item.RefreshCompatibility();
                }
            }
        }

        public ModHistoryViewModel(INavigationService navigationService, IStorageService storageService,
            IDialogService dialog, CommonUtils commonUtils, ILogger logger) : base(logger)
        {
            _navigationService = navigationService;
            _storageService = storageService;
            _commonUtils = commonUtils;

            HistoryItems = new ObservableCollection<ModHistoryItemViewModel>();
            _dialogService = dialog;
        }

        public void Initialize((ModItemViewModel Mod, ModdedApp App) data)
        {
            _selectedItem = data.Mod;
            _mod = _selectedItem?.Shell;
            _parentApp = data.App;

            this._selectedModName = _mod.Name;

            LoadHistory();
        }

        private async void LoadHistory()
        {
            
            HistoryItems.Clear();
            var historyData = await _storageService.GetInstalledModHistoryAsync(_mod.Id);

            foreach (var entry in historyData)
            {
                // Create the wrapper, passing the current app version and a link to the override status
                var wrapper = new ModHistoryItemViewModel(entry, _parentApp.InstalledVersion, () => OverrideRollbackRules, _commonUtils, _logger);
                HistoryItems.Add(wrapper);
            }

            OnPropertyChanged(nameof(HasHistory));
        }

        public ICommand RollbackCommand => new RelayCommand(async o =>
        {
            if (o is ModHistoryItemViewModel wrapper)
            {
                // Safety check in case the command is triggered via shortcut/double-click
                if (!wrapper.CanRollback) return;

                _logger.LogInformation("User initiated rollback to version {Version} for mod {ModName} in app {AppName}", wrapper.History.Version, _mod.Name, _parentApp.Name);
                // Use the service instead of the static class!
                string msg = $"Rollback to version {wrapper.History.Version}?\n\nCompatibility: {(wrapper.IsCompatible ? "Matched" : "Forced")}";

                if (_dialogService.ShowConfirmation(msg, "Confirm Rollback"))
                {
                    await _storageService.RollbackToVersionAsync(wrapper.History, this._parentApp.InstalledVersion);
                    BackCommand.Execute(null);
                }
                
            }
        });

        public ICommand DeleteHistoryItemCommand => new RelayCommand(async o =>
        {
            if (o is ModHistoryItemViewModel wrapper)
            {
                _logger.LogInformation("User initiated deletion of history entry {Version} for mod {ModName} in app {AppName}", wrapper.History.Version, _mod.Name, _parentApp.Name);
                if (_dialogService.ShowConfirmation($"Delete history entry for version {wrapper.History.Version}?", "Confirm Deletion"))
                {
                    await _storageService.DeleteInstalledModHistoryAsync(wrapper.History.InternalId);
                    LoadHistory();
                }
            }
        });

        public ICommand OpenUrlCommand => new RelayCommand(obj => ExecuteOpenUrl(obj as string));

        public ICommand CopyUrlCommand => new RelayCommand(obj => ExecuteCopyUrl(obj as string));

        public ICommand BackCommand => new RelayCommand(o =>
        {
            _navigationService.NavigateTo<LibraryViewModel, (ModdedApp, ModItemViewModel)>((_parentApp, _selectedItem));
        });

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