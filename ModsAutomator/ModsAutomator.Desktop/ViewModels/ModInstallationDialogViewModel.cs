using ModsAutomator.Core.Entities;
using ModsAutomator.Core.Enums;
using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Input;

namespace ModsAutomator.Desktop.ViewModels
{
    public class ModInstallationDialogViewModel : BaseViewModel
    {
        public InstalledMod Entity { get; }
        public Array PackageTypes => Enum.GetValues(typeof(PackageType));

        public ICommand SaveCommand { get; }
        public ICommand CancelCommand { get; }

        public ICommand OpenUrlCommand { get; }

        public ModInstallationDialogViewModel(InstalledMod entity)
        {
            Entity = entity;

            SaveCommand = new RelayCommand(_ => Close(true));
            CancelCommand = new RelayCommand(_ => Close(false));

            OpenUrlCommand = new RelayCommand(url => ExecuteOpenUrl(url?.ToString()));
        }

        private void Close(bool result)
        {
            // The ?. operator handles Application.Current being null
            // The null check handles the Windows collection being null
            var windows = Application.Current?.Windows;
            if (windows == null) return;

            foreach (Window window in windows)
            {
                if (window.DataContext == this)
                {
                    window.DialogResult = result;
                    window.Close();
                    break;
                }
            }
        }

        private void ExecuteOpenUrl(string? url)
        {
            if (string.IsNullOrEmpty(url)) return;
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = url,
                    UseShellExecute = true
                });
            }
            catch { /* Handle missing browser/protocol handler */ }
        }
    }
}