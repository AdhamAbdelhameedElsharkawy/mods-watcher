using System;
using System.Collections.Generic;
using System.Text;

namespace ModsAutomator.Desktop.Interfaces
{
    // Every ViewModel that needs data on arrival will implement this
    public interface IInitializable<TData>
    {
        void Initialize(TData data);
    }
}
