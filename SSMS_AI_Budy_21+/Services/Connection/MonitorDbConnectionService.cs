using Microsoft.VisualStudio.Shell;
using EnvDTE;
using System;
using System.Diagnostics;
using System.Threading;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using SSMS_AI_Buddy.Models;

namespace SSMS_AI_Buddy.Services.Connection
{
    public class MonitorDbConnectionService
    {
        private DTE _dte;
        private IWpfTextView _currentView;
        private string _currentServer;
        private string _currentDatabase;
        private string _currentUsername;
        private string _lastKnownCaption;
        private bool _isMonitoring;
        private Timer _pollingTimer;

        public event Action<string, string> ConnectionChanged;
        public event Action<string> ConnectionLost;
        public event Action<string, string, string> SqlTypedWithConnection;

        public void InitializeForView(IWpfTextView view)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            _currentView = view;

            // Get DTE
            _dte = Package.GetGlobalService(typeof(DTE)) as DTE;
            if (_dte == null)
            {
                Debug.WriteLine("DTE not available");
                return;
            }

            // Initial connection check
            UpdateConnectionFromCaption();

            // Hook up text changes
            view.TextBuffer.Changed += OnTextBufferChanged;

            // Hook up view focus
            view.GotAggregateFocus += OnViewGotFocus;
            view.LostAggregateFocus += OnViewLostFocus;

            // Start polling timer for caption changes
            _pollingTimer = new Timer(PollForCaptionChanges, null, 1000, 1000);

            _isMonitoring = true;

            Debug.WriteLine($"Connection monitor initialized for view: {view}");
        }

        public void StopMonitoring()
        {
            _isMonitoring = false;

            if (_pollingTimer != null)
            {
                _pollingTimer.Dispose();
                _pollingTimer = null;
            }

            // Unhook events
            if (_currentView != null)
            {
                _currentView.TextBuffer.Changed -= OnTextBufferChanged;
                _currentView.GotAggregateFocus -= OnViewGotFocus;
                _currentView.LostAggregateFocus -= OnViewLostFocus;
            }

            Debug.WriteLine("Connection monitoring stopped");
        }

        private void PollForCaptionChanges(object state)
        {
            if (!_isMonitoring) return; // has monitoring stopped

            try
            {
                // Get the current caption of the active sql tab on UI thread
                ThreadHelper.JoinableTaskFactory.Run(async () =>
                {
                    await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

                    if (_dte?.ActiveDocument?.ActiveWindow == null) return;

                    var currentCaption = _dte.ActiveDocument.ActiveWindow.Caption;
                    if (currentCaption != _lastKnownCaption)
                    {
                        _lastKnownCaption = currentCaption;
                        UpdateConnectionFromCaption();
                    }
                });
            }
            catch { }
        }

        private void OnTextBufferChanged(object sender, TextContentChangedEventArgs e)
        {
            if (!_isMonitoring) return;

            ThreadHelper.JoinableTaskFactory.Run(async () =>
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

                try
                {
                    // Get the typed text
                    foreach (var change in e.Changes)
                    {
                        if (change.NewLength > 0)
                        {
                            var typedText = change.NewText;

                            // Notify about typed SQL with connection context
                            if (!string.IsNullOrEmpty(_currentServer))
                            {
                                SqlTypedWithConnection?.Invoke(typedText, _currentServer, _currentDatabase);
                            }

                            // Check for USE statements
                            CheckForUseStatement(typedText);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error in text buffer change: {ex.Message}");
                }
            });
        }

        private void OnViewGotFocus(object sender, EventArgs e)
        {
            if (!_isMonitoring) return;

            ThreadHelper.ThrowIfNotOnUIThread();
            UpdateConnectionFromCaption();
            Debug.WriteLine("View got focus");
        }

        private void OnViewLostFocus(object sender, EventArgs e)
        {
            if (!_isMonitoring) return;

            Debug.WriteLine("View lost focus");
        }

        private void UpdateConnectionFromCaption()
        {
            try
            {
                if (_dte?.ActiveDocument?.ActiveWindow == null) return;

                var caption = _dte.ActiveDocument.ActiveWindow.Caption;
                if (string.IsNullOrEmpty(caption)) return;

                // Parse caption for connection info
                if (TryParseConnectionFromCaption(caption, out string server, out string database, out string username))
                {
                    if (_currentServer != server || _currentDatabase != database)
                    {
                        var oldServer = _currentServer;
                        var oldDatabase = _currentDatabase;

                        _currentServer = server;
                        _currentDatabase = database;
                        _currentUsername = username;

                        Debug.WriteLine($"Sql AI : Connection updated: {server}.{database}");

                        // Notify about connection change
                        ConnectionChanged?.Invoke(server, database);

                        // If this is a new connection (was null before)
                        if (string.IsNullOrEmpty(oldServer) && !string.IsNullOrEmpty(server))
                        {
                            Debug.WriteLine($"New connection established: {server}.{database}.{username}");
                        }
                    }
                }
                else
                {
                    // No connection in caption
                    if (!string.IsNullOrEmpty(_currentServer))
                    {
                        Debug.WriteLine($"Connection lost: {_currentServer}.{_currentDatabase}.{_currentUsername}");
                        ConnectionLost?.Invoke($"{_currentServer}.{_currentDatabase}.{_currentUsername}");

                        _currentServer = null;
                        _currentDatabase = null;
                        _currentUsername = null;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error updating connection: {ex.Message}");
            }
        }

        private bool TryParseConnectionFromCaption(string caption, out string server, out string database, out string userName)
        {
            server = null;
            database = null;
            userName = null;

            if (string.IsNullOrWhiteSpace(caption))
                return false;

            int dashIndex = caption.LastIndexOf(" - ");

            if (dashIndex < 0)
                return false;

            string connectionPart =
                caption.Substring(dashIndex + 3).Trim();

            // Ignore transient states
            if (connectionPart.Equals(
                "Connecting...",
                StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            // Remove dirty tab marker
            connectionPart = connectionPart.TrimEnd('*');

            // Remove protocol prefixes
            connectionPart = connectionPart
                .Replace("tcp:", "")
                .Replace("np:", "")
                .Replace("lpc:", "");

            // Extract login
            int loginStart = connectionPart.IndexOf(" (");

            if (loginStart > 0)
            {
                int loginEnd = connectionPart.IndexOf(')', loginStart);

                if (loginEnd > loginStart)
                {
                    userName = connectionPart
                        .Substring(
                            loginStart + 2,
                            loginEnd - loginStart - 2)
                        .Trim();

                    connectionPart =
                        connectionPart.Substring(0, loginStart)
                                      .Trim();
                }
            }

            // Parse SERVER.DATABASE
            int dotIndex = connectionPart.LastIndexOf('.');

            if (dotIndex <= 0)
                return false;

            server =
                connectionPart.Substring(0, dotIndex).Trim();

            database =
                connectionPart.Substring(dotIndex + 1).Trim();

            return
                !string.IsNullOrWhiteSpace(server) &&
                !string.IsNullOrWhiteSpace(database);
        }

        //private bool TryParseConnectionFromCaption(string caption, out string server, out string database, out string userName)
        //{
        //    server = null;
        //    database = null;
        //    userName = null;

        //    if (string.IsNullOrWhiteSpace(caption) || !caption.Contains(" - "))
        //        return false;

        //    var parts = caption.Split(new[] { " - " }, StringSplitOptions.RemoveEmptyEntries);
        //    if (parts.Length < 2)
        //        return false;

        //    var connectionPart = parts[1].Trim();

        //    if (connectionPart.Equals("Connecting...", StringComparison.OrdinalIgnoreCase))
        //        return false;

        //    // Extract username if present: (user)
        //    var openParen = connectionPart.IndexOf('(');
        //    if (openParen > 0)
        //    {
        //        var closeParen = connectionPart.IndexOf(')', openParen);
        //        if (closeParen > openParen)
        //        {
        //            userName = connectionPart
        //                .Substring(openParen + 1, closeParen - openParen - 1)
        //                .Trim();

        //            connectionPart = connectionPart.Substring(0, openParen).Trim();
        //        }
        //    }

        //    // server.database
        //    var dotIndex = connectionPart.IndexOf('.');
        //    if (dotIndex > 0)
        //    {
        //        server = connectionPart.Substring(0, dotIndex);
        //        database = connectionPart.Substring(dotIndex + 1);
        //        return true;
        //    }

        //    // server only
        //    server = connectionPart;
        //    database = "master";
        //    return true;
        //}



        private void CheckForUseStatement(string typedText)
        {
            if (string.IsNullOrEmpty(typedText)) return;

            try
            {
                if (typedText.IndexOf("USE ", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    var lines = typedText.Split('\n');
                    foreach (var line in lines)
                    {
                        var trimmed = line.Trim();
                        if (trimmed.StartsWith("USE ", StringComparison.OrdinalIgnoreCase))
                        {
                            var dbName = trimmed.Substring(4).Trim().TrimEnd(';', ' ', '\r');
                            Debug.WriteLine($"USE statement detected: {dbName}");

                            if (!string.IsNullOrEmpty(_currentServer))
                            {
                                Debug.WriteLine($"Database context would change to: {_currentServer}.{dbName}");
                            }
                            break;
                        }
                    }
                }
            }
            catch { }
        }

        public ConnectionInfo GetCurrentConnection()
        {
            return new ConnectionInfo
            {
                Server = _currentServer,
                Database = _currentDatabase,
                IsConnected = !string.IsNullOrEmpty(_currentServer),
                Caption = _lastKnownCaption,
                Username = _currentUsername
            };
        }

        public string GetCurrentCaption()
        {
            return _lastKnownCaption;
        }
    }

}