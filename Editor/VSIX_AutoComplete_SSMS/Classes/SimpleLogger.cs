using Microsoft.Toolkit.Uwp.Notifications;
using System;
using System.IO;
using SQL_Shortcuts.Resources;

namespace VSIX_AutoComplete_SSMS.Classes
{
    public static class SimpleLogger
    {
        private static string _logPath;
        private static readonly object _lock = new object();

        public static string Initialize(string logFolder)
        {
            string folder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                logFolder);

            if (!Directory.Exists(folder))
                Directory.CreateDirectory(folder);

            _logPath = Path.Combine(folder, $"log-{DateTime.Now:yyyy-MM-dd}.txt");

            return _logPath;
        }

        public static void Info(string message)
        {
            if (string.IsNullOrEmpty(_logPath)) return;

            try
            {
                lock (_lock)
                {
                    File.AppendAllText(_logPath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}{Environment.NewLine}");
                }
            }
            catch
            {
                // Suppress errors in logging
            }
        }

        public static void Error(string message)
        {
            if (string.IsNullOrEmpty(_logPath)) return;

            try
            {
                lock (_lock)
                {
                    // add to log file
                    File.AppendAllText(_logPath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {Resource.Error_Disdplay} {message}{Environment.NewLine}");

                    // display in SSMS statusbar
                    StatusBarService.UpdateStatusBarText(message);

                    // display error popup
                    new ToastContentBuilder()
                        .AddVisualChild(new AdaptiveText()
                        {
                            Text = Resource.Grid_Toast_Header,
                            HintStyle = AdaptiveTextStyle.Title
                        })
                        .AddVisualChild(new AdaptiveText()
                        {
                            Text = Resource.Logger_Error,
                            HintStyle = AdaptiveTextStyle.Body
                        })
                        .AddVisualChild(new AdaptiveText()
                        {
                            Text = message,
                            HintStyle = AdaptiveTextStyle.CaptionSubtle
                        })
                        .Show();
                }
            }
            catch
            {
                // Suppress errors in logging
            }
        }
    }
}
