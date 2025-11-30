using System;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.Windows.Forms;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.SqlServer.Management.Smo;
using System.Reflection;

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
            _package = package ?? throw new ArgumentNullException(nameof(package));
            
            if (commandService == null)
            {
                throw new ArgumentNullException(nameof(commandService), "CommandService cannot be null");
            }

            try
            {
                var menuCommandId = new CommandID(CommandSet, CommandId);
                Debug.WriteLine($"ShowTableNameCommand: Creating CommandID with Set={CommandSet}, Id={CommandId}");
                
                var menuItem = new OleMenuCommand(Execute, menuCommandId);
                Debug.WriteLine($"ShowTableNameCommand: Created OleMenuCommand");

                // Only show menu item when it's a TABLE node
                menuItem.BeforeQueryStatus += BeforeQueryStatus;
                Debug.WriteLine($"ShowTableNameCommand: Attached BeforeQueryStatus handler");

                commandService.AddCommand(menuItem);
                Debug.WriteLine($"ShowTableNameCommand: Menu command added to commandService successfully");
                
                // Verify the command was registered
                var verifyCmd = commandService.FindCommand(menuCommandId);
                if (verifyCmd != null)
                {
                    Debug.WriteLine($"ShowTableNameCommand: VERIFIED - Command found in command service!");
                }
                else
                {
                    Debug.WriteLine($"ShowTableNameCommand: WARNING - Command NOT found in command service after AddCommand!");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"ShowTableNameCommand: ERROR adding command: {ex.Message}\n{ex.StackTrace}");
                throw; // Re-throw so we know registration failed
            }
        }

        public static async System.Threading.Tasks.Task InitializeAsync(AsyncPackage package)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            try
            {
                // Get the command service - this is required for menu commands
                var commandService = await package.GetServiceAsync(typeof(IMenuCommandService)) as OleMenuCommandService;
                
                if (commandService == null)
                {
                    Debug.WriteLine("ShowTableNameCommand: ERROR - commandService is null! Cannot register command.");
                    // Try to get it synchronously as fallback using IServiceProvider
                    // We're already on UI thread after SwitchToMainThreadAsync above
                    var sp = package as IServiceProvider;
                    if (sp != null)
                    {
                        commandService = sp.GetService(typeof(IMenuCommandService)) as OleMenuCommandService;
                    }
                    
                    if (commandService == null)
                    {
                        Debug.WriteLine("ShowTableNameCommand: ERROR - commandService is still null after sync call!");
                        return;
                    }
                    Debug.WriteLine("ShowTableNameCommand: Got commandService via sync call");
                }

                Debug.WriteLine($"ShowTableNameCommand: Creating command instance. CommandId={CommandId}, CommandSet={CommandSet}");
                Instance = new ShowTableNameCommand(package, commandService);
                Debug.WriteLine($"ShowTableNameCommand: Command registered successfully!");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"ShowTableNameCommand: Initialization error: {ex.Message}\n{ex.StackTrace}");
                // Re-throw to let package know initialization failed
                throw;
            }
        }

        private void BeforeQueryStatus(object sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            var cmd = (OleMenuCommand)sender;

            try
            {
                // Get current Object Explorer selection using SqlWorkbench.Interfaces
                var selectedNodes = GetSelectedNodes();
                
                // Visible only if exactly 1 node selected and it's a TABLE
                if (selectedNodes != null && selectedNodes.Length == 1)
                {
                    var node = selectedNodes[0];
                    var nodeContext = GetNodeContext(node);
                    
                    // Make visible if it's a table, otherwise hide
                    cmd.Visible = nodeContext == "Table";
                    cmd.Enabled = cmd.Visible;
                }
                else
                {
                    // Show the command even if we can't determine selection (for testing)
                    // In production, you might want to set this to false
                    cmd.Visible = true;
                    cmd.Enabled = true;
                }
            }
            catch
            {
                // If we can't determine the selection, show the command anyway (for testing)
                // This helps debug if the menu appears at all
                cmd.Visible = true;
                cmd.Enabled = true;
            }
        }

        private void Execute(object sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            try
            {
                var selectedNodes = GetSelectedNodes();

                if (selectedNodes != null && selectedNodes.Length == 1)
                {
                    var node = selectedNodes[0];
                    var nodeContext = GetNodeContext(node);

                    if (nodeContext == "Table")
                    {
                        // Get table information using SMO
                        var tableInfo = GetTableInfo(node);
                        
                        if (!string.IsNullOrEmpty(tableInfo))
                        {
                            MessageBox.Show(tableInfo, "SSMS Table Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }
                        else
                        {
                            var nodeName = GetNodeName(node);
                            MessageBox.Show($"Selected Table: {nodeName}", "SSMS Table", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }
                    }
                    else
                    {
                        MessageBox.Show($"Selected node is not a table. Context: {nodeContext}", "SSMS", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                }
                else
                {
                    MessageBox.Show("No table selected or multiple items selected.", "SSMS", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error executing command: {ex.Message}", "SSMS Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private object[] GetSelectedNodes()
        {
            try
            {
                // Use reflection to access ServiceCache.SelectedNodes from SqlWorkbench.Interfaces
                var sqlWorkbenchAssembly = Assembly.LoadFrom(GetSqlWorkbenchInterfacesPath());
                var serviceCacheType = sqlWorkbenchAssembly.GetType("Microsoft.SqlServer.Management.UI.VSIntegration.ObjectExplorer.ServiceCache");
                
                if (serviceCacheType != null)
                {
                    var selectedNodesProperty = serviceCacheType.GetProperty("SelectedNodes", BindingFlags.Public | BindingFlags.Static);
                    if (selectedNodesProperty != null)
                    {
                        return selectedNodesProperty.GetValue(null) as object[];
                    }
                }
            }
            catch
            {
                // Fallback if reflection fails
            }

            return null;
        }

        private string GetNodeContext(object node)
        {
            try
            {
                if (node != null)
                {
                    var nodeType = node.GetType();
                    var contextProperty = nodeType.GetProperty("Context");
                    if (contextProperty != null)
                    {
                        return contextProperty.GetValue(node)?.ToString();
                    }
                }
            }
            catch
            {
                // Ignore errors
            }

            return string.Empty;
        }

        private string GetNodeName(object node)
        {
            try
            {
                if (node != null)
                {
                    var nodeType = node.GetType();
                    var nameProperty = nodeType.GetProperty("Name");
                    if (nameProperty != null)
                    {
                        return nameProperty.GetValue(node)?.ToString() ?? "Unknown";
                    }
                }
            }
            catch
            {
                // Ignore errors
            }

            return "Unknown";
        }

        private string GetTableInfo(object node)
        {
            try
            {
                // Try to get SMO table object from the node
                var nodeType = node.GetType();
                var smoObjectProperty = nodeType.GetProperty("SmoObject");
                
                if (smoObjectProperty != null)
                {
                    var smoObject = smoObjectProperty.GetValue(node);
                    if (smoObject is Table table)
                    {
                        return $"Table: {table.Schema}.{table.Name}\n" +
                               $"Database: {table.Parent.Name}\n" +
                               $"Server: {table.Parent.Parent.Name}\n" +
                               $"Columns: {table.Columns.Count}\n" +
                               $"Indexes: {table.Indexes.Count}";
                    }
                }

                // Alternative: Try to get Urn and parse it
                var urnProperty = nodeType.GetProperty("Urn");
                if (urnProperty != null)
                {
                    var urn = urnProperty.GetValue(node)?.ToString();
                    if (!string.IsNullOrEmpty(urn) && urn.Contains("Table"))
                    {
                        // Extract table name from Urn
                        var parts = urn.Split('/');
                        var tableName = parts[parts.Length - 1];
                        return $"Table: {tableName}\nUrn: {urn}";
                    }
                }
            }
            catch (Exception ex)
            {
                // Return error info for debugging
                return $"Error getting table info: {ex.Message}";
            }

            return string.Empty;
        }

        private string GetSqlWorkbenchInterfacesPath()
        {
            // Try different SSMS versions
            var possiblePaths = new[]
            {
                @"C:\Program Files (x86)\Microsoft SQL Server Management Studio 20\Common7\IDE\Extensions\Application\SqlWorkbench.Interfaces.dll",
                @"C:\Program Files (x86)\Microsoft SQL Server Management Studio 19\Common7\IDE\Extensions\Application\SqlWorkbench.Interfaces.dll",
                @"C:\Program Files (x86)\Microsoft SQL Server Management Studio 18\Common7\IDE\Extensions\Application\SqlWorkbench.Interfaces.dll",
                // Fallback to local copy
                System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "SqlWorkbench.Interfaces.dll")
            };

            foreach (var path in possiblePaths)
            {
                if (System.IO.File.Exists(path))
                {
                    return path;
                }
            }

            // Last resort: use the one in Misc folder
            var miscPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "Misc", "20", "SqlWorkbench.Interfaces.dll");
            if (System.IO.File.Exists(miscPath))
            {
                return System.IO.Path.GetFullPath(miscPath);
            }

            return possiblePaths[possiblePaths.Length - 1]; // Return fallback
        }
    }
}
