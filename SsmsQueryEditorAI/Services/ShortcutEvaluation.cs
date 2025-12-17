using Microsoft.VisualStudio;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell;
//using VSIX_Demo;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;
using SsmsAiToolbar;
using System;
using System.Diagnostics;

//using VSIX_AutoComplete_SSMS.Models;
using System.Linq;
using System.Text.RegularExpressions;
//using SQL_Shortcuts.Resources;

namespace SsmsQueryEditorAI.Services
{
    internal class ShortcutEvaluation : IOleCommandTarget, IVsTextViewFilter
    {
        #region Class Variables
        private const string Table_TAG = "<table>";
        private readonly InlineCSharpHelper _inlineCSharpHelper;
        private readonly SsmsQueryEditorAIPackage _package;
        private readonly IVsTextView _textView;
        public IOleCommandTarget _nextFilter { get; set; }
        private IWpfTextView _wpfTextView;
        #endregion

        private SqlConnectionMonitorService _activeMonitor;

        public ShortcutEvaluation(SsmsQueryEditorAIPackage package, IVsTextView textView)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            _inlineCSharpHelper = new InlineCSharpHelper();
            _package = package;
            _textView = textView;

            var componentModel = ServiceProvider.GlobalProvider
                .GetService(typeof(SComponentModel)) as IComponentModel;

            if (componentModel == null)
                return;

            var editorFactory =
                componentModel.GetService<IVsEditorAdaptersFactoryService>();

            if (editorFactory == null)
                return;
            
            _wpfTextView = editorFactory.GetWpfTextView(textView);

            if (_wpfTextView == null)
                return;

            _wpfTextView.GotAggregateFocus += OnEditorActivated;
            _wpfTextView.LostAggregateFocus += OnEditorDeactivated;
            _wpfTextView.Closed += OnTextViewClosed;
        }

        public int Exec(ref Guid pguidCmdGroup, uint nCmdID, uint nCmdexecopt, IntPtr pvaIn, IntPtr pvaOut)
        {
            // Only handle regular typing
            if (pguidCmdGroup == VSConstants.VSStd2K &&
                nCmdID == (uint)VSConstants.VSStd2KCmdID.TYPECHAR)
            {
                char typedChar = (char)(ushort)System.Runtime.InteropServices.Marshal.GetObjectForNativeVariant(pvaIn);

                try
                {
                    if (_wpfTextView != null)
                    {
                        var caretPosition = _wpfTextView.Caret.Position.BufferPosition;
                        var snapshot = _wpfTextView.TextSnapshot;

                        // Get the entire text from the start of the buffer up to the caret
                        string textUpToCaret = snapshot.GetText(0, caretPosition.Position);

                        // Split into statements if needed (optional)
                        // For simplicity, just work with the text up to caret
                        string currentStatement = textUpToCaret;

                        // Check if "FROM" keyword exists (case-insensitive)
                        bool fromExists = Regex.IsMatch(currentStatement, @"\bfrom\b", RegexOptions.IgnoreCase);

                        if (fromExists)
                        {

                            Debug.WriteLine($"Typed char: {typedChar}, FROM exists: {fromExists}");

                            // Example: take action if backtick typed and FROM exists
                            if (typedChar == '`')
                            {
                                if (fromExists)
                                {
                                    Debug.WriteLine("FROM already typed, do something...");
                                }
                                else
                                {
                                    Debug.WriteLine("No FROM yet, do something else...");
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error in Exec: {ex.Message}");
                }
            }

            // Call next filter to not block normal typing
            return _nextFilter.Exec(ref pguidCmdGroup, nCmdID, nCmdexecopt, pvaIn, pvaOut);
        }


        public int QueryStatus(ref Guid pguidCmdGroup, uint cCmds, OLECMD[] prgCmds, IntPtr pCmdText)
        {
            // Pass the query to the next filter in the chain
            return _nextFilter.QueryStatus(ref pguidCmdGroup, cCmds, prgCmds, pCmdText);
        }

        public int GetDataTipText(TextSpan[] pSpan, out string pbstrText)
        {
            var nextTextViewFilter = _nextFilter as IVsTextViewFilter;
            if (nextTextViewFilter != null)
            {
                return nextTextViewFilter.GetDataTipText(pSpan, out pbstrText);
            }
            pbstrText = null;
            return VSConstants.E_NOTIMPL;
        }
        public int GetPairExtents(int iLine, int iIndex, TextSpan[] pSpan)
        {
            var nextTextViewFilter = _nextFilter as IVsTextViewFilter;
            if (nextTextViewFilter != null)
            {
                return nextTextViewFilter.GetPairExtents(iLine, iIndex, pSpan);
            }
            return VSConstants.E_NOTIMPL;
        }
        public int GetWordExtent(int iLine, int iIndex, uint dwFlags, TextSpan[] pSpan)
        {
            var nextTextViewFilter = _nextFilter as IVsTextViewFilter;
            if (nextTextViewFilter != null)
            {
                return nextTextViewFilter.GetWordExtent(iLine, iIndex, dwFlags, pSpan);
            }
            return VSConstants.E_NOTIMPL;
        }
      

        private void OnEditorActivated(object sender, EventArgs e)
        {
            //var view = sender as IWpfTextView;
            //if (view == null)
            //    return;

            //ITextSnapshot snapshot = view.TextSnapshot;
            //string fullText = snapshot.GetText();

            //Debug.WriteLine("Editor text length: " + fullText);

            ThreadHelper.ThrowIfNotOnUIThread();

            var view = sender as IWpfTextView;
            if (view == null) return;

            try
            {
                // Stop previous monitor if any
                if (_activeMonitor != null)
                {
                    _activeMonitor.StopMonitoring();
                    _activeMonitor = null;
                }

                // Create and initialize new monitor
                _activeMonitor = new SqlConnectionMonitorService();

                // Subscribe to events
                _activeMonitor.ConnectionChanged += OnActiveConnectionChanged;
                _activeMonitor.SqlTypedWithConnection += OnActiveSqlTyped;

                // Initialize
                _activeMonitor.InitializeForView(view);

                // Get initial state
                var connection = _activeMonitor.GetCurrentConnection();
                Debug.WriteLine($"Initial connection: {connection}");

                // Get editor text
                ITextSnapshot snapshot = view.TextSnapshot;
                string fullText = snapshot.GetText();
                Debug.WriteLine($"Editor text length: {fullText.Length}");

            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in OnEditorActivated: {ex.Message}");
            }
        }

        private void OnEditorDeactivated(object sender, EventArgs e)
        {
            // Editor tab lost focus
        }

        private void OnTextViewClosed(object sender, EventArgs e)
        {
            _wpfTextView.GotAggregateFocus -= OnEditorActivated;
            _wpfTextView.LostAggregateFocus -= OnEditorDeactivated;
            _wpfTextView.Closed -= OnTextViewClosed;
        }


        private void OnActiveConnectionChanged(string server, string database)
        {
            Debug.WriteLine($"Active connection changed: {server}.{database}");

            // You can now:
            // 1. Update UI to show current connection
            // 2. Load connection-specific settings
            // 3. Validate current SQL against this connection
            // 4. Fetch schema information for IntelliSense
        }

        private void OnActiveSqlTyped(string sql, string server, string database)
        {
            // Process each typed character/word with connection context
            var trimmedSql = sql.Trim();
            if (!string.IsNullOrEmpty(trimmedSql))
            {
                Debug.WriteLine($"Typed on {server}.{database}: '{trimmedSql}'");

                // Example: Check for database-specific syntax
                if (database.Equals("master", StringComparison.OrdinalIgnoreCase))
                {
                    if (trimmedSql.Contains("sp_"))
                    {
                        Debug.WriteLine("⚠ Using system stored procedures in master database");
                    }
                }
            }
        }
    }
}
