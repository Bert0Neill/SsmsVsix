using EnvDTE;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;
using SSMS_AI_Buddy.Models;
using SSMS_AI_Buddy.Services.Connection;
using SSMS_AI_Buddy.Services.SSMS;
using SSMS_AI_Buddy.Services.SSMS.SSMS_AI_Buddy.Services.SSMS;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace SSMS_AI_Buddy.Services
{
    internal class AIEditorService : IOleCommandTarget, IVsTextViewFilter
    {
        #region Class Variables
        private readonly SsmsQueryEditorAIPackage _package;
        private readonly IVsTextView _textView;
        public IOleCommandTarget _nextFilter { get; set; }
        private IWpfTextView _wpfTextView;
        private MonitorDbConnectionService _activeMonitor;
        private string _connectionString;
        private string _serverName;
        private string _databaseName;
        private string _connectionKey;
        private string _username;
        #endregion

        private bool _isSuggesting;
        private int _suggestionIndex;

        private int _insertLine;
        private int _insertColumn;
        private string _currentGhostText;

        private readonly List<string> _suggestions = new List<string>()
{
@"Customers c
JOIN Orders o ON c.CustomerID = o.CustomerID",

@"Orders o
JOIN OrderItems oi ON o.OrderID = oi.OrderID"
};




        public AIEditorService(SsmsQueryEditorAIPackage package, IVsTextView textView)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

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

            // hook into editor events
            _wpfTextView.GotAggregateFocus += OnEditorActivated;
            _wpfTextView.LostAggregateFocus += OnEditorDeactivated;
            _wpfTextView.Closed += OnTextViewClosed;
        }

        /// <summary>
        /// This method is called by Visual Studio for every command sent to the editor:
        /// Keystrokes (letters, numbers)
        /// Special keys(Tab, Enter, Backspace)
        /// Paste, Cut, Undo, Redo
        /// Menu commands
        /// IntelliSense triggers
        /// </summary>
        /// <param name="pguidCmdGroup"></param>
        /// <param name="nCmdID"></param>
        /// <param name="nCmdexecopt"></param>
        /// <param name="pvaIn"></param>
        /// <param name="pvaOut"></param>
        /// <returns></returns>
        //public int Exec(ref Guid pguidCmdGroup, uint nCmdID, uint nCmdexecopt, IntPtr pvaIn, IntPtr pvaOut)
        //{
        //    // Only handle regular typing
        //    if (pguidCmdGroup == VSConstants.VSStd2K && nCmdID == (uint)VSConstants.VSStd2KCmdID.TYPECHAR)
        //    {
        //        char typedChar = (char)(ushort)System.Runtime.InteropServices.Marshal.GetObjectForNativeVariant(pvaIn);

        //        try
        //        {
        //            if (_wpfTextView != null)
        //            {
        //                var caretPosition = _wpfTextView.Caret.Position.BufferPosition;
        //                var snapshot = _wpfTextView.TextSnapshot;

        //                // Get the entire text from the start of the buffer up to the caret
        //                string textUpToCaret = snapshot.GetText(0, caretPosition.Position);

        //                // Split into statements if needed (optional)
        //                // For simplicity, just work with the text up to caret
        //                string currentStatement = textUpToCaret;

        //                // Check if "FROM" keyword exists (case-insensitive)
        //                bool fromExists = Regex.IsMatch(currentStatement, @"\bfrom\b", RegexOptions.IgnoreCase);

        //                if (fromExists)
        //                {
        //                    Debug.WriteLine($"Typed char: {typedChar}, FROM exists: {fromExists}");

        //                    // Example: take action if backtick typed and FROM exists
        //                    if (typedChar == '`')
        //                    {
        //                        if (fromExists)
        //                        {
        //                            Debug.WriteLine("FROM already typed, do something...");
        //                        }
        //                        else
        //                        {
        //                            Debug.WriteLine("No FROM yet, do something else...");
        //                        }
        //                    }
        //                }
        //            }
        //        }
        //        catch (Exception ex)
        //        {
        //            Debug.WriteLine($"Error in Exec: {ex.Message}");
        //        }
        //    }

        //    // Call next filter to not block normal typing
        //    return _nextFilter.Exec(ref pguidCmdGroup, nCmdID, nCmdexecopt, pvaIn, pvaOut);
        //}

        //public int Exec(ref Guid pguidCmdGroup, uint nCmdID, uint nCmdexecopt, IntPtr pvaIn, IntPtr pvaOut)
        //{
        //    if (pguidCmdGroup == VSConstants.VSStd2K && nCmdID == (uint)VSConstants.VSStd2KCmdID.TYPECHAR)
        //    {
        //        char typedChar = (char)(ushort)Marshal.GetObjectForNativeVariant(pvaIn);

        //        if (typedChar == ' ' || typedChar == '\n')
        //        {
        //            var sql = EditorHelper.GetCurrentEditorText();

        //            if (SqlAnalyzer.ShouldTrigger(sql))
        //            {
        //                var schema = SchemaCache.GetOrAdd(
        //                    _connectionKey,
        //                    () => SchemaReader.LoadSchemaFromDatabase(_connectionString));

        //                var tables = SqlAnalyzer.ExtractTables(sql);

        //                var graph = JoinGraphBuilder.BuildGraph(schema.ForeignKeys);
        //                var expanded = JoinGraphBuilder.Expand(tables, graph);

        //                var json = OllamaSchemaBuilder.Build(_databaseName, schema, expanded);

        //                // 👉 SEND json + prompt to Ollama
        //            }
        //        }
        //    }

        //        // Call next filter to not block normal typing
        //        return _nextFilter.Exec(ref pguidCmdGroup, nCmdID, nCmdexecopt, pvaIn, pvaOut);
        //    }

        public int Exec(ref Guid pguidCmdGroup, uint nCmdID, uint nCmdexecopt,
                  IntPtr pvaIn, IntPtr pvaOut)
        {
            if (pguidCmdGroup == VSConstants.VSStd2K)
            {
                // Typed character
                if (nCmdID == (uint)VSConstants.VSStd2KCmdID.TYPECHAR)
                {
                    char ch = (char)(ushort)Marshal.GetObjectForNativeVariant(pvaIn);

                    if (_isSuggesting && !char.IsWhiteSpace(ch))
                    {
                        ClearSuggestion();
                    }

                    if (ch == ' ')
                    {
                        var sql = EditorHelper.GetCurrentEditorText();

                        if (sql.EndsWith(" from", StringComparison.OrdinalIgnoreCase))
                        {
                            _suggestionIndex = 0;
                            RenderOrReplaceSuggestion();
                            return VSConstants.S_OK;
                        }
                    }
                }

                // TAB → next suggestion
                if (_isSuggesting && nCmdID == (uint)VSConstants.VSStd2KCmdID.TAB)
                {
                    _suggestionIndex = (_suggestionIndex + 1) % _suggestions.Count;
                    RenderOrReplaceSuggestion();
                    return VSConstants.S_OK; // swallow TAB
                }


                // ENTER → accept suggestion
                if (_isSuggesting && nCmdID == (uint)VSConstants.VSStd2KCmdID.RETURN)
                {
                    // Stop suggestion mode
                    _isSuggesting = false;
                    _currentGhostText = null;

                    // Let SSMS insert the newline
                    return _nextFilter.Exec(
                        ref pguidCmdGroup,
                        nCmdID,
                        nCmdexecopt,
                        pvaIn,
                        pvaOut);
                }

                // ESC → cancel
                if (_isSuggesting && nCmdID == (uint)VSConstants.VSStd2KCmdID.CANCEL)
                {
                    ClearSuggestion();
                    return VSConstants.S_OK;
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

        #region Event Handlers
        /// <summary>
        /// Create a new monitor for this active sql editor
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnEditorActivated(object sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            //// *** original code
            //var view = sender as IWpfTextView;
            //if (view == null)
            //    return;
            //ITextSnapshot snapshot = view.TextSnapshot;
            //string fullText = snapshot.GetText();
            //Debug.WriteLine("Editor text length: " + fullText);

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
                _activeMonitor = new MonitorDbConnectionService();

                // Subscribe to events
                _activeMonitor.ConnectionChanged += OnActiveConnectionChanged;
                // _activeMonitor.SqlTypedWithConnection += OnActiveSqlTyped;

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
            // switching away from this editor (OnActive will pick up new connection string if different)
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

            _serverName = server;
            _databaseName = database;
            var connectionInfo = _activeMonitor.GetCurrentConnection();
            _username = connectionInfo.Username;
            _connectionString = $"Server={server};Database={database};Trusted_Connection=True;";
            _connectionKey = $"{server}|{database}|{connectionInfo.Username}";

            // get DB schema and cache it
            SchemaCache.GetOrAdd(
                          _connectionKey,
                          () => SchemaReader.LoadSchemaFromDatabase(_connectionString));
        }
        #endregion

        #region Schema Helpers
        private void OnActiveSqlTyped(string sql, string server, string database)
        {
            // Process each typed character/word with connection context
            var trimmedSql = sql.Trim();
            if (!string.IsNullOrEmpty(trimmedSql))
            {
                Debug.WriteLine($"Typed on {server}.{database}: '{trimmedSql}'");

                //// Example: Check for database-specific syntax
                //if (database.Equals("master", StringComparison.OrdinalIgnoreCase))
                //{
                //    if (trimmedSql.Contains("sp_"))
                //    {
                //        Debug.WriteLine("⚠ Using system stored procedures in master database");
                //    }
                //}
            }
        }

        private void SuggestTablesForDatabase(ConnectionInfo connection)
        {
            Debug.WriteLine($"Would suggest tables from {connection.Database} on {connection.Server}");

            // Here you would:
            // 1. Query the database for table names
            // 2. Show Intellisense suggestions
            // 3. Or do whatever you need with the connection info
        }

        private void SuggestSchemaObjects(ConnectionInfo connection, string textUpToCaret)
        {
            // Parse the text before the dot to understand context
            // Example: "SELECT * FROM dbo." -> suggest table names in dbo schema
            // Example: "SELECT Customer." -> suggest column names

            Debug.WriteLine($"Schema suggestion for {connection}");
        }

        private bool ShouldTriggerAi(string sql)
        {
            if (sql.Length < 10)
                return false;

            return Regex.IsMatch(
                sql,
                @"\b(from|join)\b",
                RegexOptions.IgnoreCase);
        }

    #endregion

    #region Interface IVsTextViewFilter Members
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
        #endregion
    }


}
