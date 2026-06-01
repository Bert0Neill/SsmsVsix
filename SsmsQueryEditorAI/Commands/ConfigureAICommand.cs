using SSMS_AI_Buddy.Forms;
using SSMS_AI_Buddy.Services;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.ComponentModel.Design;
using System.Globalization;
using System.Windows.Forms;
using System.Threading;
using System.Threading.Tasks;
using Task = System.Threading.Tasks.Task;

namespace SSMS_AI_Buddy.Commands
{
    /// <summary>
    /// Command handler
    /// </summary>
    internal sealed class ConfigureAICommand
    {
        /// <summary>
        /// Command ID.
        /// </summary>
        public const int CommandId = 0x0100;

        /// <summary>
        /// Command menu group (command set GUID).
        /// </summary>
        public static readonly Guid CommandSet = new Guid("d5cfa9e8-a7de-4eb4-b34f-0ab39f9e31a3");

        /// <summary>
        /// VS Package that provides this command, not null.
        /// </summary>
        private readonly AsyncPackage package;

        /// <summary>
        /// Initializes a new instance of the <see cref="ConfigureAICommand"/> class.
        /// Adds our command handlers for menu (commands must exist in the command table file)
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        /// <param name="commandService">Command service to add command to, not null.</param>
        private ConfigureAICommand(AsyncPackage package, OleMenuCommandService commandService)
        {
            this.package = package ?? throw new ArgumentNullException(nameof(package));
            commandService = commandService ?? throw new ArgumentNullException(nameof(commandService));

            var menuCommandID = new CommandID(CommandSet, CommandId);
            var menuItem = new MenuCommand(this.Execute, menuCommandID);
            commandService.AddCommand(menuItem);
        }

        /// <summary>
        /// Gets the instance of the command.
        /// </summary>
        public static ConfigureAICommand Instance
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
            // Switch to the main thread - the call to AddCommand in ConfigureAI_Cmd's constructor requires
            // the UI thread.
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(package.DisposalToken);

            OleMenuCommandService commandService = await package.GetServiceAsync(typeof(IMenuCommandService)) as OleMenuCommandService;
            Instance = new ConfigureAICommand(package, commandService);
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
            try
            {
                using (var propertiesForm = new PropertiesFrm())
                {
                    if (TryGetDialogOwner(out var owner))
                    {
                        propertiesForm.ShowDialog(owner);
                    }
                    else
                    {
                        propertiesForm.ShowDialog();
                    }
                }
            }
            catch (Exception ex)
            {
                LoggerService.Error($"ConfigureAICommand.Execute failed: {ex}");
                VsShellUtilities.ShowMessageBox(
                    this.package,
                    $"Could not open AI settings.\n\n{ex.Message}",
                    "AI Settings Error",
                    OLEMSGICON.OLEMSGICON_CRITICAL,
                    OLEMSGBUTTON.OLEMSGBUTTON_OK,
                    OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
            }
        }

        private bool TryGetDialogOwner(out IWin32Window owner)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            owner = null;

            var uiShell = ServiceProvider.GetServiceAsync(typeof(SVsUIShell))
                .ConfigureAwait(false)
                .GetAwaiter()
                .GetResult() as IVsUIShell;

            if (uiShell == null)
            {
                return false;
            }

            uiShell.GetDialogOwnerHwnd(out var hwnd);
            if (hwnd == IntPtr.Zero)
            {
                return false;
            }

            owner = new Win32WindowWrapper(hwnd);
            return true;
        }

        private sealed class Win32WindowWrapper : IWin32Window
        {
            public Win32WindowWrapper(IntPtr handle)
            {
                Handle = handle;
            }

            public IntPtr Handle { get; }
        }
    }
}
