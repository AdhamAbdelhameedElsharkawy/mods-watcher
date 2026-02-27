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

        // Source of truth (unfiltered)
        private List<ModHistoryItemViewModel> _allHistoryRecords = new();

        // UI Binding collection
        private ObservableCollection<ModHistoryItemViewModel> _historyItems = new();
        public ObservableCollection<ModHistoryItemViewModel> HistoryItems
        {
            get => _historyItems;
            set => SetProperty(ref _historyItems, value);
        }

        // Filter Options
        public ObservableCollection<string> AppVersionFilterOptions { get; set; } = new();

        private string _selectedAppVersionFilter = "All";
        public string SelectedAppVersionFilter
        {
            get => _selectedAppVersionFilter;
            set
            {
                if (SetProperty(ref _selectedAppVersionFilter, value))
                    ApplyFilter();
            }
        }

        public bool HasHistory => HistoryItems?.Count > 0;

        public string SelectedModName
        {
            get => _selectedModName;
            set => SetProperty(ref _selectedModName, value);
        }

        public bool OverrideRollbackRules
        {
            get => _overrideRollbackRules;
            set
            {
                if (SetProperty(ref _overrideRollbackRules, value))
                {
                    // Update compatibility status for all records when override toggle changes
                    foreach (var item in _allHistoryRecords) item.RefreshCompatibility();
                }
            }
        }

        public ModHistoryViewModel(INavigationService navigationService, IStorageService storageService,
            IDialogService dialog, CommonUtils commonUtils, ILogger logger) : base(logger)
        {
            _navigationService = navigationService;
            _storageService = storageService;
            _commonUtils = commonUtils;
            _dialogService = dialog;

            HistoryItems = new ObservableCollection<ModHistoryItemViewModel>();
        }

        public void Initialize((ModItemViewModel Mod, ModdedApp App) data)
        {
            _selectedItem = data.Mod;
            _mod = _selectedItem?.Shell;
            _parentApp = data.App;
            this.SelectedModName = _mod?.Name ?? "Mod History";

            LoadHistory();
        }

        private async void LoadHistory()
        {
            _allHistoryRecords.Clear();
            var historyData = await _storageService.GetInstalledModHistoryAsync(_mod.Id);

            foreach (var entry in historyData)
            {
                var wrapper = new ModHistoryItemViewModel(entry, _parentApp.InstalledVersion, () => OverrideRollbackRules, _commonUtils, _logger);
                _allHistoryRecords.Add(wrapper);
            }

            // Generate unique App Versions for the filter dropdown
            var apps = _allHistoryRecords
                .Select(x => x.History.AppVersion)
                .Where(v => !string.IsNullOrEmpty(v))
                .Distinct()
                .OrderByDescending(v => v)
                .ToList();

            apps.Insert(0, "All");
            AppVersionFilterOptions = new ObservableCollection<string>(apps);
            OnPropertyChanged(nameof(AppVersionFilterOptions));

            ApplyFilter();
        }

        private void ApplyFilter()
        {
            if (SelectedAppVersionFilter == "All")
            {
                HistoryItems = new ObservableCollection<ModHistoryItemViewModel>(_allHistoryRecords);
            }
            else
            {
                var filtered = _allHistoryRecords.Where(x => x.History.AppVersion == SelectedAppVersionFilter);
                HistoryItems = new ObservableCollection<ModHistoryItemViewModel>(filtered);
            }

            OnPropertyChanged(nameof(HasHistory));
        }

        public ICommand RollbackCommand => new RelayCommand(async o =>
        {
            if (o is ModHistoryItemViewModel wrapper)
            {
                if (!wrapper.CanRollback) return;

                _logger.LogInformation("User initiated rollback to version {Version} for mod {ModName}", wrapper.History.Version, _mod.Name);

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
            try { System.Windows.Clipboard.SetText(url); } catch { }
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
            catch { }
        }
    }
}