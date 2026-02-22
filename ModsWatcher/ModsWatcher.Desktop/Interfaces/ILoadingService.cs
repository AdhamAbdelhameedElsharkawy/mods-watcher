using System;
using System.Collections.Generic;
using System.Text;

namespace ModsWatcher.Desktop.Interfaces
{
    public interface ILoadingService
    {
        bool IsBusy { get; set; }
        string BusyMessage { get; set; }
    }
}
