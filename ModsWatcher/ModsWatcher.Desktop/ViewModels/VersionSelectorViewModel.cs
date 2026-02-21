using Microsoft.Extensions.Logging;
using ModsWatcher.Core.DTO;
using ModsWatcher.Core.Entities;
using ModsWatcher.Desktop.ViewModels;
using System.Collections.ObjectModel;
using System.Windows.Input;

public class VersionSelectorViewModel : BaseViewModel
{
    public ObservableCollection<VersionSelectionDto> DisplayMods { get; }
    public ICommand AcceptCommand { get; }
    public List<AvailableMod> SelectedMods { get; private set; } = new();
    public AvailableMod? PrimaryMod { get; private set; }
    public Action<bool?>? RequestClose { get; set; }

    public VersionSelectorViewModel(IEnumerable<AvailableMod> mods, ILogger logger) : base(logger)
    {
        var dtos = mods.Select(m => new VersionSelectionDto(m));
        DisplayMods = new ObservableCollection<VersionSelectionDto>(dtos);

        // FIX IS HERE: Lambda discards the 'object' parameter
        AcceptCommand = new RelayCommand(_ => OnAccept());
    }

    private void OnAccept()
    {
        // 1. Get all checked for history
        SelectedMods = DisplayMods
            .Where(x => x.IsSelected)
            .Select(x => x.Mod)
            .ToList();

        // 2. Get the one to install
        PrimaryMod = DisplayMods
            .FirstOrDefault(x => x.IsPrimary)?.Mod;

        // Validation: If they picked a primary, it must be in the selected list
        if (PrimaryMod != null && !SelectedMods.Contains(PrimaryMod))
        {
            SelectedMods.Add(PrimaryMod);
        }

        if (!SelectedMods.Any())
        {
            // Optional: add a message box or just prevent close
            return;
        }

        RequestClose?.Invoke(true);
    }
}