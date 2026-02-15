using ModsAutomator.Core.Entities;
using ModsAutomator.Core.Enums;
using System;
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

        public ModInstallationDialogViewModel(InstalledMod entity)
        {
            Entity = entity;

            SaveCommand = new RelayCommand(_ => Close(true));
            CancelCommand = new RelayCommand(_ => Close(false));
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
    }
}