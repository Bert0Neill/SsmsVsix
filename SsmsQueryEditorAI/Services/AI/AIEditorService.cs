using Microsoft.VisualStudio;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;
using Newtonsoft.Json;
using SSMS_AI_Buddy.Models;
using SSMS_AI_Buddy.Resources;
using SSMS_AI_Buddy.Services.AI;
using SSMS_AI_Buddy.Services.Connection;
using SSMS_AI_Buddy.Services.SSMS;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Threading;
using static SSMS_AI_Buddy.Models.AppEnums;

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

        public static AIEditorService ActiveService { get; private set; }
        #endregion

        public AIEditorService(SsmsQueryEditorAIPackage package, IVsTextView textView)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            _package = package;
            _textView = textView;

            ActiveService = this;

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

            EditorHelper.ActiveView = _wpfTextView; // ✔ always valid

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
        public int Exec(ref Guid pguidCmdGroup, uint nCmdID, uint nCmdexecopt, IntPtr pvaIn, IntPtr pvaOut)
        {
            // 🔥 HANDLE POPUP KEYS FIRST
            if (SqlSuggestionPopup.IsOpen && pguidCmdGroup == VSConstants.VSStd2K)
            {
                switch ((VSConstants.VSStd2KCmdID)nCmdID)
                {
                    case VSConstants.VSStd2KCmdID.TAB:
                    case VSConstants.VSStd2KCmdID.RETURN:
                        SqlSuggestionPopup.AcceptSelection();
                        return VSConstants.S_OK;   // swallow

                    case VSConstants.VSStd2KCmdID.UP:
                        SqlSuggestionPopup.SelectPrevious();
                        return VSConstants.S_OK;

                    case VSConstants.VSStd2KCmdID.DOWN:
                        SqlSuggestionPopup.SelectNext();
                        return VSConstants.S_OK;

                    case VSConstants.VSStd2KCmdID.CANCEL: // ESC
                        SqlSuggestionPopup.Close();
                        return VSConstants.S_OK;
                }
            }

            //// 🔽 NORMAL TYPING HANDLING
            //if (pguidCmdGroup == VSConstants.VSStd2K && nCmdID == (uint)VSConstants.VSStd2KCmdID.TYPECHAR)
            //{
            //    char typedChar = (char)(ushort)Marshal.GetObjectForNativeVariant(pvaIn);

            //    if (typedChar == ' ' || typedChar == '\n')
            //    {
            //        var sql = EditorHelper.GetCurrentEditorText();

            //        if (SqlAnalyzer.ShouldTrigger(sql))
            //        {
            //            var schema = SchemaCache.GetOrAdd(_connectionKey, () => SchemaReader.LoadSchemaFromDatabase(_connectionString));
            //            var tables = SqlAnalyzer.ExtractTables(sql, schema.Tables);
            //            var graph = JoinGraphBuilder.BuildGraph(schema.ForeignKeys);
            //            var expanded = JoinGraphBuilder.Expand(tables, graph);
            //            var schemaJson = OllamaSchemaBuilder.Build(_databaseName, schema, expanded);

            //            // 👉 SEND json + prompt to Ollama
            //            var suggestions = new List<SqlSuggestion>();
            //            try
            //            {
            //                ThreadHelper.JoinableTaskFactory.Run(async () =>
            //                {
            //                    suggestions = await OllamaSqlSuggestionService.GetSqlSuggestionsAsync(schemaJson, sql)
            //                        ?? new List<SqlSuggestion>();
            //                });
            //            }
            //            catch (Exception ex)
            //            {
            //                Debug.WriteLine($"Ollama GetSqlSuggestionsAsync failed: {ex}");
            //            }

            //            // Show after this command chain finishes so the trigger char is committed and caret/layout match the popup.
            //            var view = _wpfTextView;
            //            var dispatcher = view?.VisualElement?.Dispatcher;
            //            if (dispatcher != null)
            //            {
            //                dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() =>
            //                {
            //                    ShowSqlSuggestions(view, suggestions);
            //                }));
            //            }
            //        }
            //    }
            //}

            return _nextFilter.Exec(ref pguidCmdGroup, nCmdID, nCmdexecopt, pvaIn, pvaOut);
        }

        public int QueryStatus(ref Guid pguidCmdGroup, uint cCmds, OLECMD[] prgCmds, IntPtr pCmdText)
        {
            return _nextFilter.QueryStatus(ref pguidCmdGroup, cCmds, prgCmds, pCmdText); // Pass the query to the next filter in the chain
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
            _connectionString = $"Server={server};Database={database};Trusted_Connection=True;";
            _connectionKey = $"{server}|{database}";

            // get DB schema and cache it
            SchemaCache.GetOrAdd(_connectionKey, () => SchemaReader.LoadSchemaFromDatabase(_connectionString));
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

        //private void ShowSqlSuggestions(IWpfTextView view, IList<SqlSuggestion> suggestions)
        //{
        //    if (view == null || suggestions == null || suggestions.Count == 0)
        //        return;

        //    var caret = view.Caret.Position.BufferPosition;
        //    var line = view.GetTextViewLineContainingBufferPosition(caret);
        //    if (line == null) return;

        //    var bounds = line.GetCharacterBounds(caret);
        //    var location = new Point(bounds.Left, bounds.Bottom + 2);

        //    SqlSuggestionPopup.Show(view.VisualElement, location, suggestions, selected =>
        //        {
        //            EditorHelper.InsertText(" " + selected.Suggestion); // insert only SQL text
        //        });
        //}

        private void ShowSqlSuggestions(IWpfTextView view, IList<SqlSuggestion> suggestions)
        {
            if (view == null || suggestions == null || suggestions.Count == 0)
                return;

            var caret = view.Caret.Position.BufferPosition;
            var line = view.GetTextViewLineContainingBufferPosition(caret);
            if (line == null) return;

            var bounds = line.GetCharacterBounds(caret);
            var location = new Point(bounds.Left, bounds.Bottom + 2);

            SqlSuggestionPopup.Show(view.VisualElement, location, suggestions, selected =>
            {
                var buffer = view.TextBuffer;
                var selectionEnd = view.Selection.StreamSelectionSpan.End.Position;

                using (var edit = buffer.CreateEdit())
                {
                    edit.Insert(selectionEnd, " " + selected.Suggestion);
                    edit.Apply();
                }
            });
        }

        public void TriggerSqlSuggestions(string sql, AIActionType aIActionType)
        {
            if (_wpfTextView == null || string.IsNullOrWhiteSpace(sql))
                return;

            ThreadHelper.JoinableTaskFactory.Run(async () =>
            {
                var schema = SchemaCache.GetOrAdd(_connectionKey, () => SchemaReader.LoadSchemaFromDatabase(_connectionString));
                var tables = SqlAnalyzer.ExtractTables(sql, schema.Tables);
                var graph = JoinGraphBuilder.BuildGraph(schema.ForeignKeys);
                var expanded = JoinGraphBuilder.Expand(tables, graph);
                var schemaJson = OllamaSchemaBuilder.Build(_databaseName, schema, expanded);
                var suggestions = await OllamaSqlSuggestionService.GetSqlSuggestionsAsync(schemaJson, sql, aIActionType) ?? new List<SqlSuggestion>();

                //var suggestions = MockAIResponse();

                var view = _wpfTextView;
                view.VisualElement.Dispatcher.BeginInvoke(new Action(() =>
                {
                    ShowSqlSuggestions(view, suggestions);
                }));
            });
        }

        private List<SqlSuggestion> MockAIResponse()
        {
            string json = @"[
              {
                ""type"": ""join"",
                ""suggestion"": ""INNER JOIN SalesLT.CustomerAddress ca ON SalesLT.Address.AddressID = ca.AddressID"",
                ""description"": ""Join Address to CustomerAddress using AddressID""
              },
              {
                ""type"": ""join"",
                ""suggestion"": ""LEFT JOIN SalesLT.SalesOrderHeader soh ON SalesLT.Address.AddressID = soh.ShipToAddressID"",
                ""description"": ""Join Address to SalesOrderHeader using ShipToAddressID""
              },
              {
                ""type"": ""join"",
                ""suggestion"": ""LEFT JOIN SalesLT.SalesOrderHeader soh ON SalesLT.Address.AddressID = soh.BillToAddressID"",
                ""description"": ""Join Address to SalesOrderHeader using BillToAddressID""
              },
              {
                ""type"": ""where"",
                ""suggestion"": ""WHERE SalesLT.Address.City = 'London'"",
                ""description"": ""Filter addresses by city""
              },
              {
                ""type"": ""where"",
                ""suggestion"": ""WHERE SalesLT.Address.CountryRegion = 'United States'"",
                ""description"": ""Filter addresses by country or region""
              },
              {
                ""type"": ""where"",
                ""suggestion"": ""WHERE SalesLT.Address.PostalCode = '98052'"",
                ""description"": ""Filter addresses by postal code""
              },
              {
                ""type"": ""groupby"",
                ""suggestion"": ""GROUP BY SalesLT.Address.City"",
                ""description"": ""Group addresses by city""
              },
              {
                ""type"": ""groupby"",
                ""suggestion"": ""GROUP BY SalesLT.Address.CountryRegion, SalesLT.Address.StateProvince"",
                ""description"": ""Group addresses by country and state/province""
              },
              {
                ""type"": ""having"",
                ""suggestion"": ""HAVING COUNT(SalesLT.Address.AddressID) > 1"",
                ""description"": ""Return only groups with more than one address""
              },
              {
                ""type"": ""orderby"",
                ""suggestion"": ""ORDER BY SalesLT.Address.City"",
                ""description"": ""Sort results by city""
              },
              {
                ""type"": ""orderby"",
                ""suggestion"": ""ORDER BY SalesLT.Address.ModifiedDate DESC"",
                ""description"": ""Sort results by most recently modified addresses""
              }
            ]";

            List<SqlSuggestion> suggestions =
                JsonConvert.DeserializeObject<List<SqlSuggestion>>(json);
            return suggestions;
        }

    }
}


