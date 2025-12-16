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
using SsmsQueryEditorAI.Services;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using Task = System.Threading.Tasks.Task;

namespace SsmsQueryEditorAI
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
        /// <summary>
        /// SsmsQueryEditorAIPackage GUID string.
        /// </summary>
        public const string PackageGuidString = "f79b07ee-abe6-4e30-bb4f-c6808f451d62";

        private DTE _dte;
        private IVsTextViewFilter _textViewFilter;
        private IVsStatusbar _statusBar;
        private const string SELECTION_EVENT = "Provider_SelectionChanged";
        private string _filePath = string.Empty;
        //private FileService _fileService;
        public static IObjectExplorerHelper _smoObjectExplorer;
        private SsmsHelper _helperService;

        #region Package Members

        /// <summary>
        /// Initialization of the package; this method is called right after the package is sited, so this is the place
        /// where you can put all the initialization code that rely on services provided by VisualStudio.
        /// </summary>
        /// <param name="cancellationToken">A cancellation token to monitor for initialization cancellation, which can occur when VS is shutting down.</param>
        /// <param name="progress">A provider for progress updates.</param>
        /// <returns>A task representing the async work of package initialization, or an already completed task if there is none. Do not return null from this method.</returns>
        /// 

        protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            // When initialized asynchronously, the current thread may be a background thread at this point.
            // Do any initialization that requires the UI thread after switching to the UI thread.
            await this.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
            await SsmsQueryEditorAI.Commands.ConfigureAI_Cmd.InitializeAsync(this);

            _helperService = new SsmsHelper();

            BindSmoEvent(); // bind to the selection event of the SMO table\view node

            await MonitorEditorWindow(); // capture events when user types into SQL editor
        }

        private async Task MonitorEditorWindow()
        {
            try
            {
                _dte = await GetServiceAsync(typeof(DTE)) as DTE;
                if (_dte == null)
                {
                    //Debug.WriteLine(Resource.Smo_No_Obj_Explorer);
                    return;
                }
                var textManager = await GetServiceAsync(typeof(SVsTextManager)) as IVsTextManager;
                if (textManager == null)
                {
                    //Debug.WriteLine(Resource.Smo_No_Container);
                }
                var componentModel = await GetServiceAsync(typeof(SComponentModel)) as IComponentModel;
                if (componentModel == null)
                {
                    //Debug.WriteLine(Resource.Smo_No_Components);
                    return;
                }
                var editorFactory = componentModel.GetService<IVsEditorAdaptersFactoryService>();
                if (editorFactory == null)
                {
                    //Debug.WriteLine(Resource.Smo_No_ContextService);
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
                    //Debug.WriteLine(Resource.Smo_No_Access_ObjectExplorerContext);
                }

                // Check if there's an active window already and hook into it
                if (_dte.ActiveWindow != null && _dte.ActiveWindow.Document != null)
                {
                    //Debug.WriteLine(Resource.Grid_Loading);
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
                LoggerService.Error($"{ex.Message}");
            }
        }

        private void BindSmoEvent()
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

        #endregion
    }
}
