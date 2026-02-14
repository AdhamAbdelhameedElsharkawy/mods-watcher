using ModsAutomator.Core.Entities;
using ModsAutomator.Core.Enums;
using System;

namespace ModsAutomator.Desktop.ViewModels
{
    public class SyncReviewItemViewModel : BaseViewModel
    {
        public AvailableMod ModEntry { get; }
        public SyncChangeType ChangeType { get; }

        // Use a backing field and SetProperty so the Checkbox in XAML updates 
        // when you click "Resolve All" in the parent VM.
        private bool _isSelected = true;
        public bool IsSelected
        {
            get => _isSelected;
            set => SetProperty(ref _isSelected, value);
        }

        private string _changeDescription;
        public string ChangeDescription
        {
            get => _changeDescription;
            set => SetProperty(ref _changeDescription, value);
        }

        public SyncReviewItemViewModel(AvailableMod mod, SyncChangeType type, string description = "")
        {
            ModEntry = mod;
            ChangeType = type;
            _changeDescription = description;
        }
    }
}