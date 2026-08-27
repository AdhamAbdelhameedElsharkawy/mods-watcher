using Microsoft.Extensions.Logging;
using ModsWatcher.Core.Entities;
using ModsWatcher.Desktop.Interfaces;
using ModsWatcher.Services.Interfaces;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace ModsWatcher.Desktop.ViewModels
{
    public class ModPackageViewModel : BaseViewModel, IInitializable<(ModdedApp App, ModItemViewModel Mod)>
    {
        private readonly INavigationService _navigationService;
        private readonly IStorageService _storageService;
        private readonly IDialogService _dialogService;

        private ModdedApp _parentApp = null!;
        private ModItemViewModel _modItem = null!;

        public ObservableCollection<ModPackageMember> Members { get; } = new();

        private string _newMemberName = string.Empty;
        public string NewMemberName
        {
            get => _newMemberName;
            set => SetProperty(ref _newMemberName, value);
        }

        private string _newMemberNotes = string.Empty;
        public string NewMemberNotes
        {
            get => _newMemberNotes;
            set => SetProperty(ref _newMemberNotes, value);
        }

        private string _newMemberUrl = string.Empty;
        public string NewMemberUrl
        {
            get => _newMemberUrl;
            set => SetProperty(ref _newMemberUrl, value);
        }

        public string ModName => _modItem?.Shell?.Name ?? string.Empty;
        public bool HasMembers => Members.Count > 0;

        public ICommand GoBackCommand { get; }
        public ICommand AddMemberCommand { get; }
        public ICommand RemoveMemberCommand { get; }
        public ICommand MoveMemberUpCommand { get; }
        public ICommand MoveMemberDownCommand { get; }

        public ModPackageViewModel(
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

            AddMemberCommand = new RelayCommand(
                async _ => await AddMemberAsync(),
                _ => !string.IsNullOrWhiteSpace(NewMemberName));

            RemoveMemberCommand = new RelayCommand(
                async obj => await RemoveMemberAsync(obj as ModPackageMember));

            MoveMemberUpCommand = new RelayCommand(
                async obj => await MoveMemberAsync(obj as ModPackageMember, -1));

            MoveMemberDownCommand = new RelayCommand(
                async obj => await MoveMemberAsync(obj as ModPackageMember, 1));
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
            Members.Clear();

            var members = await _storageService.GetPackageMembersAsync(_modItem.Shell.Id);
            foreach (var member in members.OrderBy(m => m.PriorityOrder))
                Members.Add(member);

            NotifyCollectionStateChanged();
        }

        private async Task AddMemberAsync()
        {
            if (string.IsNullOrWhiteSpace(NewMemberName)) return;

            try
            {
                await _storageService.AddPackageMemberAsync(
                    _modItem.Shell.Id,
                    NewMemberName.Trim(),
                    string.IsNullOrWhiteSpace(NewMemberNotes) ? null : NewMemberNotes.Trim(),
                    string.IsNullOrWhiteSpace(NewMemberUrl) ? null : NewMemberUrl.Trim());

                _logger.LogInformation("Added package member '{Name}' to '{ModName}'", NewMemberName, _modItem.Shell.Name);

                NewMemberName = string.Empty;
                NewMemberNotes = string.Empty;
                NewMemberUrl = string.Empty;

                await LoadAsync();
            }
            catch (Exception ex)
            {
                _dialogService.ShowError($"Failed to add package member: {ex.Message}");
                _logger.LogError(ex, "Unexpected error adding package member");
            }
        }

        private async Task RemoveMemberAsync(ModPackageMember? member)
        {
            if (member == null) return;

            bool confirmed = _dialogService.ShowConfirmation(
                $"Remove '{member.Name}' from this package?",
                "Remove Package Member");

            if (!confirmed) return;

            try
            {
                await _storageService.RemovePackageMemberAsync(member.InternalId);

                _logger.LogInformation("Removed package member '{Name}' from '{ModName}'", member.Name, _modItem.Shell.Name);

                await LoadAsync();
            }
            catch (Exception ex)
            {
                _dialogService.ShowError($"Failed to remove package member: {ex.Message}");
                _logger.LogError(ex, "Unexpected error removing package member");
            }
        }

        private async Task MoveMemberAsync(ModPackageMember? member, int direction)
        {
            if (member == null) return;

            int oldIndex = Members.IndexOf(member);
            int newIndex = oldIndex + direction;

            if (oldIndex < 0 || newIndex < 0 || newIndex >= Members.Count) return;

            Members.Move(oldIndex, newIndex);

            try
            {
                await _storageService.ReorderPackageMembersAsync(Members);
            }
            catch (Exception ex)
            {
                _dialogService.ShowError($"Failed to save the new order: {ex.Message}");
                _logger.LogError(ex, "Unexpected error reordering package members");
            }
        }

        private void NotifyCollectionStateChanged()
        {
            OnPropertyChanged(nameof(HasMembers));
        }
    }
}
