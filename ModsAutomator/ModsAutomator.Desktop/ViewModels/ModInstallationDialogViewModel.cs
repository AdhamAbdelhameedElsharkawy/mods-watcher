using ModsAutomator.Core.Entities;
using ModsAutomator.Core.Enums;
using System;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Windows;
using System.Windows.Input;

namespace ModsAutomator.Desktop.ViewModels
{
    public class ModInstallationDialogViewModel : BaseViewModel
    {
        public InstalledMod Entity { get; }

        [Required(ErrorMessage = "Version is required.")]
        public string InstalledVersion
        {
            get => Entity.InstalledVersion;
            set { Entity.InstalledVersion = value; ValidateProperty(value); OnPropertyChanged(); }
        }

        [Required(ErrorMessage = "URL is required.")]
        [Url(ErrorMessage = "Invalid URL format.")]
        public string? DownloadUrl
        {
            get => Entity.DownloadUrl;
            set { Entity.DownloadUrl = value; ValidateProperty(value); OnPropertyChanged(); }
        }

        [Required(ErrorMessage = "Supported App Versions is required. Format must be X.X,X.X or X.X.X,X.X.X")]
        [RegularExpression(@"^(\d+\.\d+(\.\d+)?)(,\s*\d+\.\d+(\.\d+)?)*$", ErrorMessage = "Format must be X.X,X.X or X.X.X,X.X.X")]
        public string? SupportedAppVersions
        {
            get => Entity.SupportedAppVersions;
            set { Entity.SupportedAppVersions = value; ValidateProperty(value); OnPropertyChanged(); }
        }

        public Array PackageTypes => Enum.GetValues(typeof(PackageType));

        public ICommand SaveCommand { get; }
        public ICommand CancelCommand { get; }

        public ICommand OpenUrlCommand { get; }

        public ModInstallationDialogViewModel(InstalledMod entity)
        {
            Entity = entity;

            SaveCommand = new RelayCommand(_ => Close(true), _ => !HasErrors);
            CancelCommand = new RelayCommand(_ => Close(false));

            OpenUrlCommand = new RelayCommand(url => ExecuteOpenUrl(url?.ToString()));

            ValidateAll(); 
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