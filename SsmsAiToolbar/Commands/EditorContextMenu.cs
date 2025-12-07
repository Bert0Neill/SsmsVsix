using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.ComponentModel.Design;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Task = System.Threading.Tasks.Task;

namespace SsmsAiToolbar.Commands
{
    /// <summary>
    /// Command handler
    /// </summary>
    internal sealed class EditorContextMenu
    {
        /// <summary>
        /// Command ID.
        /// </summary>
        public const int CommandId = 0x0100;

        /// <summary>
        /// Command menu group (command set GUID).
        /// </summary>
        public static readonly Guid CommandSet = new Guid("0d082b0f-1fe3-40d2-a95d-a06cad922055");

        /// <summary>
        /// VS Package that provides this command, not null.
        /// </summary>
        private readonly AsyncPackage package;

        /// <summary>
        /// Initializes a new instance of the <see cref="EditorContextMenu"/> class.
        /// Adds our command handlers for menu (commands must exist in the command table file)
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        /// <param name="commandService">Command service to add command to, not null.</param>
        private EditorContextMenu(AsyncPackage package, OleMenuCommandService commandService)
        {
            this.package = package ?? throw new ArgumentNullException(nameof(package));
            commandService = commandService ?? throw new ArgumentNullException(nameof(commandService));

            var menuCommandID = new CommandID(CommandSet, CommandId);
            var menuItem = new OleMenuCommand(this.Execute, menuCommandID);
            commandService.AddCommand(menuItem);
        }

        /// <summary>
        /// Gets the instance of the command.
        /// </summary>
        public static EditorContextMenu Instance
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the service provider from the owner package.
        /// </summary>
        private Microsoft.VisualStudio.Shell.IAsyncServiceProvider ServiceProvider
        {
            get
            {
                return this.package;
            }
        }

        /// <summary>
        /// Initializes the singleton instance of the command.
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        public static async Task InitializeAsync(AsyncPackage package)
        {
            // Switch to the main thread - the call to AddCommand in EditorContextMenu's constructor requires
            // the UI thread.
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(package.DisposalToken);

            try
            {
                OleMenuCommandService commandService = await package.GetServiceAsync(typeof(IMenuCommandService)) as OleMenuCommandService;
                
                if (commandService == null)
                {
                    System.Diagnostics.Debug.WriteLine("EditorContextMenu: ERROR - commandService is null! Trying sync call...");
                    // Try to get it synchronously as fallback using IServiceProvider
                    // We're already on UI thread after SwitchToMainThreadAsync above
                    var sp = package as IServiceProvider;
                    if (sp != null)
                    {
                        commandService = sp.GetService(typeof(IMenuCommandService)) as OleMenuCommandService;
                    }
                    
                    if (commandService == null)
                    {
                        System.Diagnostics.Debug.WriteLine("EditorContextMenu: ERROR - commandService is still null!");
                        return;
                    }
                }

                System.Diagnostics.Debug.WriteLine($"EditorContextMenu: Creating instance. CommandId={CommandId}, CommandSet={CommandSet}");
                Instance = new EditorContextMenu(package, commandService);
                System.Diagnostics.Debug.WriteLine("EditorContextMenu: Command registered successfully");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"EditorContextMenu: Initialization error: {ex.Message}\n{ex.StackTrace}");
                throw;
            }
        }

        /// <summary>
        /// This function is the callback used to execute the command when the menu item is clicked.
        /// See the constructor to see how the menu item is associated with this function using
        /// OleMenuCommandService service and MenuCommand class.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event args.</param>
        private void Execute(object sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            string message = string.Format(CultureInfo.CurrentCulture, "Inside {0}.MenuItemCallback()", this.GetType().FullName);
            string title = "EditorContextMenu";

            // Show a message box to prove we were here
            VsShellUtilities.ShowMessageBox(
                this.package,
                message,
                title,
                OLEMSGICON.OLEMSGICON_INFO,
                OLEMSGBUTTON.OLEMSGBUTTON_OK,
                OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
        }

        //private void Execute(object sender, EventArgs e)
        //{
        //    ThreadHelper.ThrowIfNotOnUIThread();

        //    var dte = Package.GetGlobalService(typeof(DTE)) as DTE2;

        //    if (dte == null)
        //    {
        //        System.Windows.Forms.MessageBox.Show("DTE2 is null - not running inside SSMS editor.");
        //        return;
        //    }

        //    var activeDoc = dte.ActiveDocument;
        //    var textDoc = activeDoc?.Object("TextDocument") as TextDocument;

        //    string selectedText = textDoc?.Selection?.Text;
        //    string fullText = textDoc?.CreateEditPoint()?.GetText(textDoc.EndPoint);

        //    System.Windows.Forms.MessageBox.Show($"Selected: {selectedText}");

        //}

    }
}
