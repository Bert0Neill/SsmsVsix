using System;
using System.ComponentModel.Design;
using System.Windows.Forms;
using Microsoft.VisualStudio.Shell;

namespace SsmsAiToolbar.Commands
{
    internal sealed class ShowTableNameCommand
    {
        public const int CommandId = 0x5001; // must match VSCT
        public static readonly Guid CommandSet = new Guid("0d082b0f-1fe3-40d2-a95d-a06cad922055"); // guidSsmsAiToolbarPackageCmdSet

        private readonly AsyncPackage _package;

        public static ShowTableNameCommand Instance { get; private set; }

        private ShowTableNameCommand(AsyncPackage package, OleMenuCommandService commandService)
        {
            _package = package;

            var menuCommandId = new CommandID(CommandSet, CommandId);
            var menuItem = new OleMenuCommand(Execute, menuCommandId);

            // Optional: Only show menu item when it's a TABLE node
            menuItem.BeforeQueryStatus += BeforeQueryStatus;

            commandService.AddCommand(menuItem);
        }

        public static async System.Threading.Tasks.Task InitializeAsync(AsyncPackage package)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            var commandService = await package.GetServiceAsync(typeof(IMenuCommandService)) as OleMenuCommandService;

            Instance = new ShowTableNameCommand(package, commandService);
        }

        private void BeforeQueryStatus(object sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            var cmd = (OleMenuCommand)sender;

            // Get current Object Explorer selection
            //var sel = ServiceCache.SelectedNodes;

            //// Visible only if exactly 1 node selected and it's a TABLE
            //if (sel?.Length == 1 && sel[0] is INodeInformation node)
            //{
            //    cmd.Visible = node.Context == "Table";
            //    cmd.Enabled = cmd.Visible;
            //}
            //else
            //{
            //    cmd.Visible = false;
            //    cmd.Enabled = false;
            //}
        }

        private void Execute(object sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            //var sel = ServiceCache.SelectedNodes;

            //if (sel?.Length == 1 && sel[0] is INodeInformation node)
            //{
            //    string tableName = node.Name;
            //    MessageBox.Show($"Selected Table: {tableName}", "SSMS Table");
            //}
            //else
            //{
            //    MessageBox.Show("No table selected.", "SSMS");
            //}

            MessageBox.Show("No table selected.", "SSMS");
        }
    }
}
