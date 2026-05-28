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
extern alias ssms22;
using EnvDTE;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TextManager.Interop;
using SSMS_AI_Buddy.Resources;
using SSMS_AI_Buddy.Services;
using SSMS_AI_Buddy.Services.SSMS;
using SSMS_SqlAdjust_Interfaces;
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
    [ProvideToolWindow(typeof(SSMS_AI_Buddy.Components.PromptWindow),Style = VsDockStyle.Float,Orientation = ToolWindowOrientation.Right,Window = null)]
    public sealed class SsmsQueryEditorAIPackage : AsyncPackage
    {
        public const string PackageGuidString = "f79b07ee-abe6-4e30-bb4f-c6808f451d62";
        private DTE _dte;
        private IVsTextViewFilter _textViewFilter;
        private IVsStatusbar _statusBar;
        private const string SELECTION_EVENT = "Provider_SelectionChanged";
        public static IObjectExplorerHelper _smoObjectExplorer;
        private SsmsHelper _helperService;
        private string _lastServer;
        private string _lastDb;
        #region Package Members

        protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            await this.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
            
            Debug.WriteLine("Initializing SSMS AI Service...");
            await SSMS_AI_Buddy.Commands.ConfigureAICommand.InitializeAsync(this);
            await SSMS_AI_Buddy.Commands.AIBuddySqlEditorCommands.InitializeAsync(this);

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
                if (gotFocus?.Document == null)
                    return;

                string docLanguage;
                string docFileName;

                try
                {
                    docLanguage = gotFocus.Document.Language;
                    docFileName = gotFocus.Document.FullName;
                }
                catch
                {
                    return;
                }

                bool isSqlFile =
                    docLanguage == "SQL" ||
                    docFileName.EndsWith(".sql", StringComparison.OrdinalIgnoreCase);

                if (isSqlFile)
                {
                    AttachEditorFilterIfNeeded(gotFocus);
                }

            }
            catch (Exception ex)
            {
                LoggerService.Error(ex.ToString());
            }
        }

        private void AttachEditorFilterIfNeeded(EnvDTE.Window gotFocus)
        {
            var componentModel = GetService(typeof(SComponentModel)) as IComponentModel;
            var editorFactory = componentModel?.GetService<IVsEditorAdaptersFactoryService>();
            var textManager = GetService(typeof(SVsTextManager)) as IVsTextManager;

            if (textManager == null || editorFactory == null)
                return;

            IVsTextView textView;
            if (ErrorHandler.Failed(textManager.GetActiveView(1, null, out textView)) || textView == null)
                return;

            var wpfTextView = editorFactory.GetWpfTextView(textView);
            if (wpfTextView == null)
                return;

            var nativeTextView = editorFactory.GetViewAdapter(wpfTextView);
            if (nativeTextView == null)
                return;

            _textViewFilter = new AIEditorService(this, nativeTextView);

            nativeTextView.AddCommandFilter(
                (IOleCommandTarget)_textViewFilter,
                out IOleCommandTarget next
            );

            ((AIEditorService)_textViewFilter)._nextFilter = next;
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
                case "SSMS 22":
                    _smoObjectExplorer = new ssms22::SSMS_22_SqlAdjust.SmoObjectExplorer(this);
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

