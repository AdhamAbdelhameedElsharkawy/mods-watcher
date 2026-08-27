using Microsoft.Extensions.Logging;
using ModsWatcher.Core.DTO;
using ModsWatcher.Core.Entities;
using ModsWatcher.Desktop.Interfaces;
using ModsWatcher.Services.Interfaces;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace ModsWatcher.Desktop.ViewModels
{
    public class ModAlternativesViewModel : BaseViewModel, IInitializable<(ModdedApp App, ModItemViewModel Mod)>
    {
        private readonly INavigationService _navigationService;
        private readonly IStorageService _storageService;
        private readonly IDialogService _dialogService;

        private ModdedApp _parentApp = null!;
        private ModItemViewModel _modItem = null!;

        public ObservableCollection<ModAlternativeDisplayDto> GroupMembers { get; } = new();
        public ObservableCollection<ModAlternativeDisplayDto> AvailableMods { get; } = new();

        private ModAlternativeDisplayDto? _selectedAvailableMod;
        public ModAlternativeDisplayDto? SelectedAvailableMod
        {
            get => _selectedAvailableMod;
            set => SetProperty(ref _selectedAvailableMod, value);
        }

        public string ModName => _modItem?.Shell?.Name ?? string.Empty;
        public bool HasGroupMembers => GroupMembers.Count > 0;
        public bool HasAvailableMods => AvailableMods.Count > 0;

        public ICommand GoBackCommand { get; }
        public ICommand AddAlternativeCommand { get; }
        public ICommand RemoveAlternativeCommand { get; }

        public ModAlternativesViewModel(
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

            AddAlternativeCommand = new RelayCommand(
                async _ => await AddAlternativeAsync(),
                _ => SelectedAvailableMod != null);

            RemoveAlternativeCommand = new RelayCommand(
                async obj => await RemoveAlternativeAsync(obj as ModAlternativeDisplayDto));
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
            GroupMembers.Clear();
            AvailableMods.Clear();
            SelectedAvailableMod = null;

            var currentModId = _modItem.Shell.Id;

            var allMods = (await _storageService.GetFullModsByAppId(_parentApp.Id)).ToList();

            var group = await _storageService.GetAlternativeGroupAsync(currentModId);
            foreach (var member in group)
                GroupMembers.Add(member);

            // Filtered dropdown — exclude self and mods already in the group
            var groupIds = GroupMembers.Select(m => Guid.Parse(m.ModId)).ToHashSet();

            foreach (var mod in allMods)
            {
                var modId = mod.Shell.Id;

                if (modId == currentModId) continue;
                if (groupIds.Contains(modId)) continue;

                AvailableMods.Add(new ModAlternativeDisplayDto
                {
                    ModId = modId.ToString(),
                    ModName = mod.Shell.Name,
                    IsActive = mod.Shell.IsUsed
                });
            }

            NotifyCollectionStateChanged();
        }

        private async Task AddAlternativeAsync()
        {
            if (SelectedAvailableMod == null) return;

            try
            {
                await _storageService.AddAlternativeAsync(
                    _modItem.Shell.Id,
                    Guid.Parse(SelectedAvailableMod.ModId));

                _logger.LogInformation("Added alternative relation: {ModName} <-> {AlternativeName}",
                    _modItem.Shell.Name, SelectedAvailableMod.ModName);

                await LoadAsync();
            }
            catch (InvalidOperationException ex)
            {
                _dialogService.ShowError(ex.Message);
                _logger.LogWarning(ex, "Failed to add alternative relation");
            }
            catch (Exception ex)
            {
                _dialogService.ShowError($"Failed to add alternative: {ex.Message}");
                _logger.LogError(ex, "Unexpected error adding alternative relation");
            }
        }

        private async Task RemoveAlternativeAsync(ModAlternativeDisplayDto? alternative)
        {
            if (alternative == null) return;

            bool confirmed = _dialogService.ShowConfirmation(
                $"Remove alternative relation with '{alternative.ModName}'?",
                "Remove Alternative");

            if (!confirmed) return;

            try
            {
                await _storageService.RemoveAlternativeAsync(
                    _modItem.Shell.Id,
                    Guid.Parse(alternative.ModId));

                _logger.LogInformation("Removed alternative relation: {ModName} <-> {AlternativeName}",
                    _modItem.Shell.Name, alternative.ModName);

                await LoadAsync();
            }
            catch (Exception ex)
            {
                _dialogService.ShowError($"Failed to remove alternative: {ex.Message}");
                _logger.LogError(ex, "Unexpected error removing alternative relation");
            }
        }

        private void NotifyCollectionStateChanged()
        {
            OnPropertyChanged(nameof(HasGroupMembers));
            OnPropertyChanged(nameof(HasAvailableMods));
        }
    }
}
