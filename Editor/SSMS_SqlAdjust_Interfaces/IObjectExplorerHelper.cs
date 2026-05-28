using System;

namespace SSMS_SqlAdjust_Interfaces
{
    public interface IObjectExplorerHelper : IDisposable
    {
        void HookSelectionChanged(string methodName);
        string SelectedSmoNode { get; set; }
    }
}
