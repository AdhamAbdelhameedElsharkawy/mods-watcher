using ModsAutomator.Core.DTO;
using ModsAutomator.Desktop.ViewModels;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;

public class LinkSelectorViewModel : BaseViewModel
{
    public ObservableCollection<CrawledLink> Links { get; }
    public ICommand ConfirmCommand { get; }
    
    // NEW: UI Commands
    public ICommand CopyUrlCommand { get; }
    public ICommand OpenUrlCommand { get; }

    public LinkSelectorViewModel(IEnumerable<CrawledLink> discoveredLinks)
    {
        Links = new ObservableCollection<CrawledLink>(discoveredLinks);

        ConfirmCommand = new RelayCommand(obj =>
        {
            if (obj is Window window)
            {
                window.DialogResult = true;
                window.Close();
            }
        });

        // Initialize new commands
        CopyUrlCommand = new RelayCommand(url => ExecuteCopyUrl(url as string));
        OpenUrlCommand = new RelayCommand(url => ExecuteOpenUrl(url as string));
    }

    private void ExecuteCopyUrl(string? url)
    {
        if (string.IsNullOrEmpty(url)) return;
        try
        {
            Clipboard.SetText(url);
        }
        catch { /* Handle clipboard lock if necessary */ }
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