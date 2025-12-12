using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Shell;
using System;
using Task = System.Threading.Tasks.Task;
using SQL_Shortcuts.Resources;

namespace VSIX_AutoComplete_SSMS.Classes
{
    public class StatusBarService
    {
        private readonly IVsStatusbar _statusBar;
        private static StatusBarService _instance;

        private StatusBarService(IVsStatusbar statusBar)
        {
            _statusBar = statusBar;
        }

        /// <summary>
        /// Initialize the status bar service with the VS status bar instance
        /// </summary>
        public static void Initialize(IVsStatusbar statusBar)
        {
            _instance = new StatusBarService(statusBar);
        }

        /// <summary>
        /// Gets the singleton instance of the status bar service
        /// </summary>
        public static StatusBarService Instance
        {
            get
            {
                if (_instance == null)
                {
                    throw new InvalidOperationException(Resource.StatusBar_Error);
                }
                return _instance;
            }
        }

        /// <summary>
        /// Updates the status bar with the specified text
        /// </summary>
        /// <param name="text">Text to display in the status bar</param>
        public void UpdateText(string text)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (_statusBar == null)
            {
                System.Diagnostics.Debug.WriteLine(Resource.StatusBar_Error_Body);
                return;
            }

            try
            {
                System.Windows.Forms.Cursor.Current = System.Windows.Forms.Cursors.WaitCursor;
                // Clear previous text
                _statusBar.FreezeOutput(0);
                _statusBar.Clear();

                // Set new text
                _statusBar.SetText(text);
                System.Diagnostics.Debug.WriteLine($"{Resource.StatusBar_Status} '{text}'");
            }
            catch (Exception ex)
            {
                SimpleLogger.Error($"{Resource.StatusBar_Error_Body} {ex.Message}");
            }
            finally
            {
                System.Windows.Forms.Cursor.Current = System.Windows.Forms.Cursors.Default;
            }
        }

        /// <summary>
        /// Updates the status bar with the specified text and icon
        /// </summary>
        /// <param name="text">Text to display in the status bar</param>
        /// <param name="icon">Icon to display alongside the text</param>
        public void UpdateText(string text, StatusBarIcon icon)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (_statusBar == null)
            {
                System.Diagnostics.Debug.WriteLine(Resource.StatusBar_Error_Body);
                return;
            }

            try
            {
                // Clear previous text
                _statusBar.FreezeOutput(0);
                _statusBar.Clear();

                // Set new text with icon
                _statusBar.SetText(text);
            }
            catch (Exception ex)
            {
                SimpleLogger.Error($"{Resource.StatusBar_Error_Body} {ex.Message}");
            }
        }

        /// <summary>
        /// Clears the status bar text
        /// </summary>
        public void Clear()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (_statusBar == null)
            {
                System.Diagnostics.Debug.WriteLine(Resource.StatusBar_Error_Body);
                return;
            }

            try
            {
                _statusBar.FreezeOutput(0);
                _statusBar.Clear();
                System.Diagnostics.Debug.WriteLine(Resource.StatusBar_Status);
            }
            catch (Exception ex)
            {
                SimpleLogger.Error($"{Resource.StatusBar_Clear} {ex.Message}");
            }
        }

        /// <summary>
        /// Async method to update status bar text from background thread
        /// </summary>
        public async Task UpdateTextAsync(string text)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            UpdateText(text);
        }

        public static void UpdateStatusBarText(string text)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            try
            {
                // Use our status bar service
                StatusBarService.Instance.UpdateText(text);

                SimpleLogger.Info($"{Resource.StatusBar_Status} {text}");
            }
            catch (Exception ex)
            {
                SimpleLogger.Error($"{Resource.StatusBar_Error_Update} {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Icons that can be displayed in the status bar
    /// </summary>
    public enum StatusBarIcon
    {
        None,
        Build,
        Save,
        Deploy,
        Find,
        Busy
    }
}
