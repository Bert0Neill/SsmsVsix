// ==============================================
// VSIX_DemoPackage Class
//
// Description:
// This class is the main package for the VSIX extension. It interacts with Visual Studio's
// environment (DTE) to manage text editor windows, load SQL shortcuts, and interact with 
// status bars and text views. The package also initializes logging and handles events 
// related to editor window activation and document type checks for SQL files.
//
// Features:
// - Initializes the VSIX package with logging and configuration.
// - Loads and manages SQL shortcuts from a configuration file.
// - Monitors active editor windows and applies filters for SQL file documents.
// - Updates Visual Studio's status bar based on actions in the editor.
//
// License Agreement:
// © [Your Company Name] [Year]. All rights reserved.
//
// This Software and any associated materials are confidential and proprietary to Bert O'Neill ("Licensor").
// By using this Software, you are agreeing to comply with the terms of this License Agreement.
//
// 1. Grant of License:
//    Licensor grants you a limited, non-exclusive, non-transferable license to use the Software for internal business operations on a single machine or as otherwise specified in the applicable agreement with Licensor.
//
// 2. Restrictions:
//    You may not:
//    - Copy, modify, distribute, or create derivative works of the Software, in whole or in part, except as expressly permitted in writing by Licensor.
//    - Reverse-engineer, decompile, or disassemble the Software, or attempt to discover its source code or underlying algorithms.
//    - Use the Software for any illegal purpose or in a manner inconsistent with the terms of this License.
//
// 3. Ownership:
//    All rights, title, and interest in the Software, including any updates or modifications, remain exclusively with Licensor. The Software is licensed, not sold.
//
// 4. Warranties and Liability:
//    The Software is provided "as-is," and Licensor makes no warranties, either express or implied, regarding its functionality, fitness for a particular purpose, or any other aspect of its use. In no event shall Licensor be liable for any direct, indirect, incidental, or consequential damages arising from the use or inability to use the Software.
//
// 5. License Termination:
//    Licensor may terminate this license at any time if you breach any of the terms of this License Agreement. Upon termination, you must immediately cease all use of the Software and return or destroy all copies of the Software.
//
// 6. Support:
//    Licensor may offer support for the Software under a separate agreement. You must have an active support contract to receive any updates or assistance.
//
// For licensing inquiries, support, or additional information, please contact:
// Bert O'Neill
// Email: BertONeill@Yahoo.Com
//
// ==============================================

extern alias ssms18;
extern alias ssms19;
extern alias ssms20;
extern alias ssms21;

using EnvDTE;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TextManager.Interop;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using Task = System.Threading.Tasks.Task;
using VSIX_AutoComplete_SSMS.Classes;
using System.IO;
using System.ComponentModel;
using VSIX_AutoComplete_SSMS.Models;
using System.Resources;
using SSMS_SqlAdjust_Interfaces;
using System.Linq;
using SQL_Shortcuts.Resources;
using System.Globalization;

namespace VSIX_Demo
{
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [InstalledProductRegistration("#110", "#112", "1.0", IconResourceID = 400)]
    [ProvideMenuResource("Menus.ctmenu", 1)]
    [Guid(PackageGuidString)]
    [ProvideAutoLoad(UIContextGuids80.SolutionExists, PackageAutoLoadFlags.BackgroundLoad)]
    [ProvideAutoLoad(UIContextGuids80.NoSolution, PackageAutoLoadFlags.BackgroundLoad)]
    public sealed class VSIX_DemoPackage : AsyncPackage
    {
        // class variables
        public const string PackageGuidString = "4607758b-acdd-4838-adb8-1dc2cfa69b00";
        private DTE _dte;
        private IVsTextViewFilter _textViewFilter;
        private IVsStatusbar _statusBar;
        private string _filePath = string.Empty;
        private FileService _fileService;
        public static IObjectExplorerHelper _smoObjectExplorer;
        private const string SELECTION_EVENT = "Provider_SelectionChanged";
        private SsmsHelper _helperService;
        public static BindingList<SqlShortcuts> _sqlShortcuts = new BindingList<SqlShortcuts>(); // store shortcuts in memory
        protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            await this.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            try
            {
                //// Test - Set the culture to French
                //System.Threading.Thread.CurrentThread.CurrentUICulture = new CultureInfo("fr");

                _helperService = new SsmsHelper();
                
                BindSmoEvent(); // bind to the selection event of the SMO table\view node

                // initialise logger
                string logFilename = SimpleLogger.Initialize(SQL_Shortcuts.Properties.Settings.Default.LogFilename);
                SimpleLogger.Info(Resource.Package_Initializing);

                LoadSqlShortcuts(); // load any existing shortcuts
                await LoadSsmsStatusBarReference(); // allow ext to update SSMS statusbar
                await MonitorEditorWindow(); // capture events when user types into SQL editor
                await VSIX_AutoComplete_SSMS.Commands.cmdShortcuts.InitializeAsync(this); // initialise Tools menu command
                
                StatusBarService.UpdateStatusBarText(string.Format(Resource.StatusBar_Status, $"SQL Shortcut Ext. - Log file: {logFilename}")); // update SSMS statusbar with log file path
            }
            catch (Exception ex)
            {
                SimpleLogger.Error($"{ex.Message}");
            }
        }

        private void BindSmoEvent()
        {
            switch(_helperService.RetrieveSsmsVersion())
            {
                case "SSMS 18":
                    _smoObjectExplorer = new ssms18::SSMS_18_SqlAdjust.SmoObjectExplorer(this);
                    break;
                case "SSMS 19":
                    _smoObjectExplorer = new ssms19::SSMS_19_SqlAdjust.SmoObjectExplorer(this);
                    break;
                case "SSMS 20":
                    _smoObjectExplorer = new ssms20::SSMS_20_SqlAdjust.SmoObjectExplorer(this);
                    break;
                case "SSMS 21":
                    _smoObjectExplorer = new ssms21::SSMS_21_SqlAdjust.SmoObjectExplorer(this);
                    break;
                default:
                    // default to SSMS 20 - most common version
                    _smoObjectExplorer = new ssms20::SSMS_20_SqlAdjust.SmoObjectExplorer(this);
                    break;
            }

            _smoObjectExplorer.HookSelectionChanged(SELECTION_EVENT);
        }

        private async Task MonitorEditorWindow()
        {
            try
            {
                _dte = await GetServiceAsync(typeof(DTE)) as DTE;
                if (_dte == null)
                {
                    Debug.WriteLine(Resource.Smo_No_Obj_Explorer);
                    return;
                }
                var textManager = await GetServiceAsync(typeof(SVsTextManager)) as IVsTextManager;
                if (textManager == null)
                {
                    Debug.WriteLine(Resource.Smo_No_Container);
                }
                var componentModel = await GetServiceAsync(typeof(SComponentModel)) as IComponentModel;
                if (componentModel == null)
                {
                    Debug.WriteLine(Resource.Smo_No_Components);
                    return;
                }
                var editorFactory = componentModel.GetService<IVsEditorAdaptersFactoryService>();
                if (editorFactory == null)
                {
                    Debug.WriteLine(Resource.Smo_No_ContextService);
                    return;
                }

                // Monitor documents
                var runningDocumentTable = new RunningDocumentTable(this);

                // Monitor window activation
                var events = _dte.Events;
                if (events != null)
                {
                    var windowEvents = events.WindowEvents;
                    if (windowEvents != null)
                    {
                        windowEvents.WindowActivated += OnWindowActivated;
                    }                    
                }
                else
                {
                    Debug.WriteLine(Resource.Smo_No_Access_ObjectExplorerContext);
                }

                // Check if there's an active window already and hook into it
                if (_dte.ActiveWindow != null && _dte.ActiveWindow.Document != null)
                {
                    Debug.WriteLine(Resource.Grid_Loading);
                    OnWindowActivated(_dte.ActiveWindow, null);
                }
            }
            catch (Exception ex)
            {
                SimpleLogger.Error($"{ex.Message}");
            }
        }

        private async Task LoadSsmsStatusBarReference()
        {
            _statusBar = await GetServiceAsync(typeof(SVsStatusbar)) as IVsStatusbar; // reference SSMS status bar

            if (_statusBar != null)
            {
                StatusBarService.Initialize(_statusBar); // Initialize our status bar service
            }
        }

        private void LoadSqlShortcuts()
        {
            try
            {
                string FOLDER_FILENAME = SQL_Shortcuts.Properties.Settings.Default.ConfigFile;

                _fileService = new FileService();
                _filePath = Path.Combine(Directory.GetCurrentDirectory(), FOLDER_FILENAME);

                if (File.Exists(_filePath))
                {
                    _sqlShortcuts = _fileService.LoadFromJson<BindingList<SqlShortcuts>>(_filePath);
                }
                else
                {
                    // load defaults if no file found, to get user started
                    _sqlShortcuts = new BindingList<SqlShortcuts>()
                    {
                        new SqlShortcuts { SqlShort = "w", SqlComplete = "Select * From <Table> Where CreatedDate > {DateTime.Now.ToString(\"d\")} " },
                        new SqlShortcuts { SqlShort = "s", SqlComplete = "Select * From <Table>" },
                        new SqlShortcuts { SqlShort = "d", SqlComplete = "Delete * From " },
                        new SqlShortcuts { SqlShort = "i", SqlComplete = "Insert Into " }
                    };
                }

                // if in demo mode, only return first shortcut or default shortcut
                if (SQL_Shortcuts.Properties.Settings.Default.IsDemo)
                {
                    SimpleLogger.Info(Resource.Grid_Demo_Limit);
                }

            }
            catch (Exception ex)
            {
                SimpleLogger.Error($"{ex.Message}");
            }
        }

        private void OnWindowActivated(EnvDTE.Window gotFocus, EnvDTE.Window lostFocus)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            try
            {
                const string ACCEPT_FILE_TYPE = "SQL";
                const string ACCEPT_FILE_EXT = ".sql";

                // Check if document exists
                if (gotFocus?.Document == null)
                {
                    Debug.WriteLine("VSIX Package: No document in focus");
                    return;
                }

                // Check if it's a SQL document
                bool isSqlFile = false;
                try
                {
                    string docLanguage = gotFocus.Document.Language;
                    string docFileName = gotFocus.Document.FullName;

                    SimpleLogger.Info($"VSIX Package: Document language: {docLanguage}, Filename: {docFileName}");

                    isSqlFile = docLanguage == ACCEPT_FILE_TYPE || docFileName.EndsWith(ACCEPT_FILE_EXT, StringComparison.OrdinalIgnoreCase);
                }
                catch (Exception ex)
                {
                    SimpleLogger.Error($"{ex.Message}");
                    return;
                }

                if (isSqlFile)
                {
                    Debug.WriteLine("VSIX Package: SQL file detected, setting up event handlers");

                    var componentModel = this.GetService(typeof(SComponentModel)) as IComponentModel;
                    if (componentModel == null)
                    {
                        Debug.WriteLine("VSIX Package: ComponentModel is null");
                        return;
                    }

                    var editorFactory = componentModel.GetService<IVsEditorAdaptersFactoryService>();
                    var textManager = this.GetService(typeof(SVsTextManager)) as IVsTextManager;

                    if (textManager == null || editorFactory == null)
                    {
                        Debug.WriteLine("VSIX Package: TextManager or EditorFactory is null");
                        return;
                    }

                    IVsTextView textView;
                    int result = textManager.GetActiveView(1, null, out textView);
                    if (ErrorHandler.Failed(result) || textView == null)
                    {
                        Debug.WriteLine($"VSIX Package: Failed to get active text view, result: {result}");
                        return;
                    }

                    var wpfTextView = editorFactory.GetWpfTextView(textView);
                    if (wpfTextView != null)
                    {
                        Debug.WriteLine("VSIX Package: Setting up text view filter");

                        IVsTextView nativeTextView = editorFactory.GetViewAdapter(wpfTextView);
                        if (nativeTextView != null)
                        {
                            _textViewFilter = new ShortcutEvaluation(this, nativeTextView);
                            result = nativeTextView.AddCommandFilter(
                                (IOleCommandTarget)_textViewFilter,
                                out IOleCommandTarget nextCommandFilter // Correct type
                            );
                            if (result == VSConstants.S_OK)
                            {
                                ((ShortcutEvaluation)_textViewFilter)._nextFilter = nextCommandFilter;
                                Debug.WriteLine("VSIX Package: Tab key filter installed successfully");
                            }
                            else
                            {
                                Debug.WriteLine($"VSIX Package: Failed to install tab key filter, result: {result}");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                SimpleLogger.Error($"{ex.Message}");
            }
        }

        protected override void Dispose(bool disposing)
        {
            try { 
            if (disposing)
            {
                // unhook class level\static fields
                _smoObjectExplorer?.Dispose();
                _smoObjectExplorer = null;                                
                _sqlShortcuts?.Clear();
                _sqlShortcuts = null;
                _fileService = null;

                if (_textViewFilter is IDisposable disposableFilter)
                    disposableFilter.Dispose();

                var windowEvents = _dte?.Events?.WindowEvents;
                if (windowEvents != null)
                {
                    windowEvents.WindowActivated -= OnWindowActivated;
                }
            }

            base.Dispose(disposing);
            }
            catch (InvalidCastException ex)
            {
                SimpleLogger.Info($"Unable to access WindowEvents during disposal: {ex.Message}");
            }
            catch (Exception ex)
            {
                SimpleLogger.Info($"Unexpected error in Dispose: {ex.Message}");
            }
        }

    }
}

