using ModsAutomator.Core.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace ModsAutomator.Desktop.ViewModels
{
    public class ModItemViewModel : BaseViewModel
    {
        public Mod Shell { get; set; }
        public InstalledMod Installed { get; set; } // Can be null if not setup

        public string Name => Shell.Name;
        public string Version => Installed?.InstalledVersion ?? "Not Installed";
        public bool IsUsed
        {
            get => Installed?.IsUsed ?? false;
            set
            {
                if (Installed != null)
                {
                    Installed.IsUsed = value;

                    // 1. Notifies the Badge (via CallerMemberName "IsUsed")
                    OnPropertyChanged();

                    // 2. Manually notifies the Summary Text
                    OnPropertyChanged(nameof(Summary));
                }
            }
        }

        // Summary as requested: Size and Used status
        public string Summary => Installed != null
            ? $"{Installed.InstalledSizeMB} MB | {(IsUsed ? "Active" : "Disabled")}"
            : "Pending Setup";

        public ModItemViewModel(Mod shell, InstalledMod installed = null)
        {
            Shell = shell;
            Installed = installed;
        }
    }
}
