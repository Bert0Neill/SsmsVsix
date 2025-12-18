// ==============================================
// SsmsQueryEditorAIPackage Class
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
using SSMS_SqlAdjust_Interfaces;
using SSMS_AI_Buddy.Resources;
using SSMS_AI_Buddy.Services;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using Task = System.Threading.Tasks.Task;

namespace SSMS_AI_Buddy
{
    /// <summary>
    /// This is the class that implements the package exposed by this assembly.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The minimum requirement for a class to be considered a valid package for Visual Studio
    /// is to implement the IVsPackage interface and register itself with the shell.
    /// This package uses the helper classes defined inside the Managed Package Framework (MPF)
    /// to do it: it derives from the Package class that provides the implementation of the
    /// IVsPackage interface and uses the registration attributes defined in the framework to
    /// register itself and its components with the shell. These attributes tell the pkgdef creation
    /// utility what data to put into .pkgdef file.
    /// </para>
    /// <para>
    /// To get loaded into VS, the package must be referred by &lt;Asset Type="Microsoft.VisualStudio.VsPackage" ...&gt; in .vsixmanifest file.
    /// </para>
    /// </remarks>
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [Guid(SsmsQueryEditorAIPackage.PackageGuidString)]
    [ProvideMenuResource("Menus.ctmenu", 1)]

    [ProvideAutoLoad(UIContextGuids80.SolutionExists, PackageAutoLoadFlags.BackgroundLoad)]
    [ProvideAutoLoad(UIContextGuids80.NoSolution, PackageAutoLoadFlags.BackgroundLoad)]
    public sealed class SsmsQueryEditorAIPackage : AsyncPackage
    {
        public const string PackageGuidString = "f79b07ee-abe6-4e30-bb4f-c6808f451d62";
        private DTE _dte;
        private IVsTextViewFilter _textViewFilter;
        private IVsStatusbar _statusBar;
        private const string SELECTION_EVENT = "Provider_SelectionChanged";
        public static IObjectExplorerHelper _smoObjectExplorer;
        private SsmsHelper _helperService;


        #region Package Members

        protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            await this.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
            
            Debug.WriteLine("Initializing SSMS AI Service...");
            await SSMS_AI_Buddy.Commands.ConfigureAI_Cmd.InitializeAsync(this);

            _helperService = new SsmsHelper();
            await LoadSsmsStatusBarReference(); // allow ext to update SSMS statusbar
            InitializeSmoObjectExplorer(); // register the correct SSMS version to its Obj. Explorer
            await MonitorEditorWindow(); // capture events when user types into SQL editor
           
            StatusBarService.UpdateStatusBarText($"AI Buddy Integrated Into SSMS"); // update SSMS statusbar with log file path
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
                LoggerService.Error($"{ex.Message}");
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

                    LoggerService.Info($"VSIX Package: Document language: {docLanguage}, Filename: {docFileName}");

                    isSqlFile = docLanguage == ACCEPT_FILE_TYPE || docFileName.EndsWith(ACCEPT_FILE_EXT, StringComparison.OrdinalIgnoreCase);
                }
                catch (Exception ex)
                {
                    LoggerService.Error($"{ex.Message}");
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
                            _textViewFilter = new AIEditorService(this, nativeTextView); // AIEditorService - hooks into editor events (monitoring of active SQL editors for connection string)

                            // Before you process commands (typing, paste, enter, shortcuts, etc.), let my object see them first (YourFilter → ExistingFilter → Editor → Default behavior)
                            result = nativeTextView.AddCommandFilter(
                                (IOleCommandTarget)_textViewFilter,
                                out IOleCommandTarget nextCommandFilter);

                            if (result == VSConstants.S_OK)
                            {
                                ((AIEditorService)_textViewFilter)._nextFilter = nextCommandFilter;
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
                LoggerService.Error($"{ex.Message}");
            }
        }

        private void InitializeSmoObjectExplorer()
        {
            switch (_helperService.RetrieveSsmsVersion())
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

        private async Task LoadSsmsStatusBarReference()
        {
            _statusBar = await GetServiceAsync(typeof(SVsStatusbar)) as IVsStatusbar; // reference SSMS status bar

            if (_statusBar != null)
            {
                StatusBarService.Initialize(_statusBar); // Initialize our status bar service
            }
        }

        protected override void Dispose(bool disposing)
        {
            try
            {
                if (disposing)
                {
                    // unhook class level\static fields
                    _smoObjectExplorer?.Dispose();
                    _smoObjectExplorer = null;
                    //_sqlShortcuts?.Clear();
                    //_sqlShortcuts = null;
                    //_fileService = null;

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
                LoggerService.Info($"Unable to access WindowEvents during disposal: {ex.Message}");
            }
            catch (Exception ex)
            {
                LoggerService.Info($"Unexpected error in Dispose: {ex.Message}");
            }
        }

    } // class

    #endregion
}

