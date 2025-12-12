using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;
using Microsoft.VisualStudio;
using System;
using VSIX_Demo;
using Microsoft.VisualStudio.Text;
using VSIX_AutoComplete_SSMS.Models;
using System.Linq;
using System.Text.RegularExpressions;
using SQL_Shortcuts.Resources;

namespace VSIX_AutoComplete_SSMS.Classes
{
    internal class ShortcutEvaluation : IOleCommandTarget, IVsTextViewFilter
    {
        #region Class Variables
        private const string Table_TAG = "<table>";
        private readonly InlineCSharpHelper _inlineCSharpHelper;
        private readonly VSIX_DemoPackage _package;
        private readonly IVsTextView _textView;
        public IOleCommandTarget _nextFilter { get; set; }
        private IWpfTextView _wpfTextView;
        #endregion

        public ShortcutEvaluation(VSIX_DemoPackage package, IVsTextView textView)
        {
            _inlineCSharpHelper = new InlineCSharpHelper();
            _package = package;
            _textView = textView;
            // Get the WPF text view from the native text view
            var componentModel = ServiceProvider.GlobalProvider.GetService(typeof(SComponentModel)) as IComponentModel;
            if (componentModel != null)
            {
                var editorFactory = componentModel.GetService<IVsEditorAdaptersFactoryService>();
                if (editorFactory != null)
                {
                    _wpfTextView = editorFactory.GetWpfTextView(textView);
                }
            }
        }

        public int Exec(ref Guid pguidCmdGroup, uint nCmdID, uint nCmdexecopt, IntPtr pvaIn, IntPtr pvaOut)
        {
            if (pguidCmdGroup == VSConstants.VSStd2K && nCmdID == (uint)VSConstants.VSStd2KCmdID.TYPECHAR)
            {
                char typedChar = (char)(ushort)System.Runtime.InteropServices.Marshal.GetObjectForNativeVariant(pvaIn);

                if (typedChar == '`')
                {
                    try
                    {
                        if (_wpfTextView != null)
                        {
                            var caretPosition = _wpfTextView.Caret.Position.BufferPosition;
                            var line = caretPosition.GetContainingLine();
                            var snapshot = _wpfTextView.TextSnapshot;

                            var lineStart = line.Start;
                            var textBeforeCaret = snapshot.GetText(lineStart, caretPosition - lineStart);

                            int backtickIndex = textBeforeCaret.LastIndexOf('`');
                            if (backtickIndex >= 0)
                            {
                                int shortcutStart = textBeforeCaret.LastIndexOfAny(new[] { ' ', '\t' }, backtickIndex) + 1;
                                shortcutStart = Math.Max(shortcutStart, 0);

                                int spanStart = lineStart + shortcutStart;
                                int spanLength = backtickIndex - shortcutStart + 1;

                                if (spanStart >= 0 && spanLength > 0 && (spanStart + spanLength) <= snapshot.Length)
                                {
                                    var spanToReplace = new Span(spanStart, spanLength);
                                    string shortcutKey = snapshot.GetText(spanStart, backtickIndex - shortcutStart);

                                    using (var edit = _wpfTextView.TextBuffer.CreateEdit())
                                    {
                                        var sqlShortcut = FindSqlShortcut(shortcutKey);
                                        if (sqlShortcut != null)
                                        {
                                            string evaluatedQuery = _inlineCSharpHelper.EvaluateInlineCSharp(sqlShortcut.SqlComplete) + " ";

                                            if (!string.IsNullOrWhiteSpace(VSIX_DemoPackage._smoObjectExplorer.SelectedSmoNode))
                                            {
                                                evaluatedQuery = Regex.Replace(
                                                    evaluatedQuery,
                                                    Table_TAG,
                                                    VSIX_DemoPackage._smoObjectExplorer.SelectedSmoNode,
                                                    RegexOptions.IgnoreCase
                                                );
                                            }

                                            edit.Replace(spanToReplace, evaluatedQuery);
                                            edit.Apply();
                                            return VSConstants.S_OK; // consume input
                                        }
                                        else
                                        {
                                            edit.Apply();
                                        }
                                    }
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        SimpleLogger.Error(string.Format(Resource.Logger_Error, ex.Message));
                    }
                    finally
                    {
                        System.Windows.Forms.Cursor.Current = System.Windows.Forms.Cursors.Default;
                    }
                }
            }

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
        private SqlShortcuts FindSqlShortcut(string sqlShort)
        {
            return VSIX_DemoPackage._sqlShortcuts.FirstOrDefault(s =>
                s.SqlShort.Trim().Equals(sqlShort.Trim(), StringComparison.OrdinalIgnoreCase)
            );
        }
    }
}
