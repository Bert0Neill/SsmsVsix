using System;

namespace SSMS_SqlAdjust_Interfaces
{
    public interface IObjectExplorerHelper : IDisposable
    {
        void HookSelectionChanged(string methodName);
        void HookSelectionChanged(string eventName, Action<string, string> callback);
        string SelectedSmoNode { get; set; }
    }
}
