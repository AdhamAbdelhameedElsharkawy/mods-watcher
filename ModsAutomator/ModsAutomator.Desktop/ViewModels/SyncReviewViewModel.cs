using ModsAutomator.Services.Interfaces;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace ModsAutomator.Desktop.ViewModels
{
    public class SyncReviewViewModel : BaseViewModel
    {
        private readonly IStorageService _storageService;

        public ObservableCollection<SyncReviewItemViewModel> ReviewItems { get; } = new();

        public ICommand ResolveAllCommand { get; }
        public ICommand ApplySelectedCommand { get; }
        public ICommand CancelCommand { get; }

        public event Action? OnRequestClose;

        public SyncReviewViewModel(IStorageService storageService)
        {
            _storageService = storageService;

            // Using your RelayCommand. The 'async' here creates an async void lambda,
            // but since we await EVERYTHING inside, it will behave correctly until the end.
            ResolveAllCommand = new RelayCommand(async _ => await ResolveAll());
            ApplySelectedCommand = new RelayCommand(async _ => await ApplySelected());
            CancelCommand = new RelayCommand(_ => CloseOverlay());
        }

        private async Task ResolveAll()
        {
            foreach (var item in ReviewItems)
            {
                item.IsSelected = true;
            }
            await ApplySelected();
        }

        private async Task ApplySelected()
        {
            // 1. Snapshot the items to process
            var itemsToProcess = ReviewItems.Where(x => x.IsSelected).ToList();

            // 2. Await the loop so the DB work actually finishes
            foreach (var item in itemsToProcess)
            {
                await _storageService.CommitSyncChangeAsync(item.ModEntry.Id, item.ModEntry, item.ChangeType);
            }

            // 3. ONLY NOW close and trigger the refresh in the parent VM
            CloseOverlay();
        }

        private void CloseOverlay()
        {
            ReviewItems.Clear();
            OnRequestClose?.Invoke();
        }
    }
}