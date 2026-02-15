using ModsAutomator.Core.DTO;
using ModsAutomator.Desktop.ViewModels;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;

public class LinkSelectorViewModel : BaseViewModel
{
    public ObservableCollection<CrawledLink> Links { get; }
    public ICommand ConfirmCommand { get; }

    public LinkSelectorViewModel(IEnumerable<CrawledLink> discoveredLinks)
    {
        // Use the DTO exactly as defined in your Core namespace
        Links = new ObservableCollection<CrawledLink>(discoveredLinks);

        ConfirmCommand = new RelayCommand(obj =>
        {
            if (obj is Window window)
            {
                window.DialogResult = true;
                window.Close();
            }
        });
    }
}