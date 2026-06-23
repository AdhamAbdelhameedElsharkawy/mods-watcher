using Microsoft.Extensions.Logging;
using ModsWatcher.Core.DTO;
using ModsWatcher.Core.Entities;
using ModsWatcher.Desktop.Interfaces;
using ModsWatcher.Services.Interfaces;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace ModsWatcher.Desktop.ViewModels
{
    public class ModDependenciesViewModel : BaseViewModel, IInitializable<(ModdedApp App, ModItemViewModel Mod)>
    {
        private readonly INavigationService _navigationService;
        private readonly IStorageService _storageService;
        private readonly IDialogService _dialogService;

        private ModdedApp _parentApp = null!;
        private ModItemViewModel _modItem = null!;

        public ObservableCollection<ModDependencyDisplayDto> Parents { get; } = new();
        public ObservableCollection<ModDependencyDisplayDto> Dependents { get; } = new();
        public ObservableCollection<ModDependencyDisplayDto> AvailableParents { get; } = new();

        private ModDependencyDisplayDto? _selectedAvailableParent;
        public ModDependencyDisplayDto? SelectedAvailableParent
        {
            get => _selectedAvailableParent;
            set => SetProperty(ref _selectedAvailableParent, value);
        }

        public string ModName => _modItem?.Shell?.Name ?? string.Empty;
        public bool HasParents => Parents.Count > 0;
        public bool HasDependents => Dependents.Count > 0;
        public bool HasAvailableParents => AvailableParents.Count > 0;

        public ICommand GoBackCommand { get; }
        public ICommand AddDependencyCommand { get; }
        public ICommand RemoveDependencyCommand { get; }

        public ModDependenciesViewModel(
            INavigationService navigationService,
            IStorageService storageService,
            IDialogService dialogService,
            ILogger logger) : base(logger)
        {
            _navigationService = navigationService;
            _storageService = storageService;
            _dialogService = dialogService;

            GoBackCommand = new RelayCommand(_ =>
                _navigationService.NavigateTo<LibraryViewModel, (ModdedApp, ModItemViewModel)>((_parentApp, _modItem)));

            AddDependencyCommand = new RelayCommand(
                async _ => await AddDependencyAsync(),
                _ => SelectedAvailableParent != null);

            RemoveDependencyCommand = new RelayCommand(
                async obj => await RemoveDependencyAsync(obj as ModDependencyDisplayDto));
        }

        public async void Initialize((ModdedApp App, ModItemViewModel Mod) data)
        {
            _parentApp = data.App;
            _modItem = data.Mod;
            OnPropertyChanged(nameof(ModName));
            await LoadAsync();
        }

        private async Task LoadAsync()
        {
            Parents.Clear();
            Dependents.Clear();
            AvailableParents.Clear();
            SelectedAvailableParent = null;

            var currentModId = _modItem.Shell.Id;

            var allMods = (await _storageService.GetFullModsByAppId(_parentApp.Id)).ToList();
            var nameMap = allMods.ToDictionary(m => m.Shell.Id, m => m.Shell.Name);

            // Parents — what does this mod depend on?
            var parentRelations = await _storageService.GetParentsAsync(currentModId);
            foreach (var relation in parentRelations)
            {
                Parents.Add(new ModDependencyDisplayDto
                {
                    ModId = relation.ParentModId.ToString(),
                    ModName = nameMap.GetValueOrDefault(relation.ParentModId, relation.ParentModId.ToString())
                });
            }

            // Dependents — what depends on this mod?
            var dependentRelations = await _storageService.GetDependentsAsync(currentModId);
            foreach (var relation in dependentRelations)
            {
                Dependents.Add(new ModDependencyDisplayDto
                {
                    ModId = relation.DependentModId.ToString(),
                    ModName = nameMap.GetValueOrDefault(relation.DependentModId, relation.DependentModId.ToString())
                });
            }

            // Filtered dropdown
            var existingParentIds = Parents.Select(p => Guid.Parse(p.ModId)).ToHashSet();

            foreach (var mod in allMods)
            {
                var modId = mod.Shell.Id;

                if (modId == currentModId) continue;
                if (existingParentIds.Contains(modId)) continue;

                bool wouldCircle = await _storageService.WouldCreateCircularDependencyAsync(currentModId, modId);
                if (wouldCircle) continue;

                AvailableParents.Add(new ModDependencyDisplayDto
                {
                    ModId = modId.ToString(),
                    ModName = mod.Shell.Name
                });
            }

            NotifyCollectionStateChanged();
        }

        private async Task AddDependencyAsync()
        {
            if (SelectedAvailableParent == null) return;

            try
            {
                await _storageService.AddDependencyAsync(
                    _modItem.Shell.Id,
                    Guid.Parse(SelectedAvailableParent.ModId));

                _logger.LogInformation("Added dependency: {ModName} now depends on {ParentName}",
                    _modItem.Shell.Name, SelectedAvailableParent.ModName);

                await LoadAsync();
            }
            catch (InvalidOperationException ex)
            {
                _dialogService.ShowError(ex.Message);
                _logger.LogWarning(ex, "Failed to add dependency");
            }
            catch (Exception ex)
            {
                _dialogService.ShowError($"Failed to add dependency: {ex.Message}");
                _logger.LogError(ex, "Unexpected error adding dependency");
            }
        }

        private async Task RemoveDependencyAsync(ModDependencyDisplayDto? parent)
        {
            if (parent == null) return;

            bool confirmed = _dialogService.ShowConfirmation(
                $"Remove dependency on '{parent.ModName}'?",
                "Remove Dependency");

            if (!confirmed) return;

            try
            {
                await _storageService.RemoveDependencyAsync(
                    _modItem.Shell.Id,
                    Guid.Parse(parent.ModId));

                _logger.LogInformation("Removed dependency: {ModName} no longer depends on {ParentName}",
                    _modItem.Shell.Name, parent.ModName);

                await LoadAsync();
            }
            catch (Exception ex)
            {
                _dialogService.ShowError($"Failed to remove dependency: {ex.Message}");
                _logger.LogError(ex, "Unexpected error removing dependency");
            }
        }

        private void NotifyCollectionStateChanged()
        {
            OnPropertyChanged(nameof(HasParents));
            OnPropertyChanged(nameof(HasDependents));
            OnPropertyChanged(nameof(HasAvailableParents));
        }
    }
}