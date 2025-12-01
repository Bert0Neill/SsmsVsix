using System;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.Windows.Forms;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlServer.Management.UI.VSIntegration.ObjectExplorer;
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
                // Get Object Explorer service
                var objectExplorerService = GetObjectExplorerService();
                if (objectExplorerService == null)
                {
                    cmd.Visible = false;
                    cmd.Enabled = false;
                    return;
                }

                // Get selected nodes
                objectExplorerService.GetSelectedNodes(out int arraySize, out INodeInformation[] nodes);
                
                // Visible only if exactly 1 node selected and it's a TABLE
                if (arraySize == 1 && nodes != null && nodes.Length == 1)
                {
                    var node = nodes[0];
                    var nodeContext = GetNodeContext(node);
                    
                    // Make visible if it's a table, otherwise hide
                    cmd.Visible = nodeContext == "Table";
                    cmd.Enabled = cmd.Visible;
                }
                else
                {
                    cmd.Visible = false;
                    cmd.Enabled = false;
                }
            }
            catch
            {
                // If we can't determine the selection, hide the command
                cmd.Visible = false;
                cmd.Enabled = false;
            }
        }

        private void Execute(object sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            try
            {
                // Get Object Explorer service
                var objectExplorerService = GetObjectExplorerService();
                if (objectExplorerService == null)
                {
                    MessageBox.Show("Object Explorer Service not available.", "SSMS Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                // Get selected nodes
                objectExplorerService.GetSelectedNodes(out int arraySize, out INodeInformation[] nodes);
                
                if (arraySize == 0 || nodes == null || nodes.Length == 0)
                {
                    MessageBox.Show("No table selected.", "SSMS", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                var node = nodes[0];
                
                // Check if it's a table node
                var nodeContext = GetNodeContext(node);
                if (nodeContext != "Table")
                {
                    MessageBox.Show($"Selected node is not a table. Context: {nodeContext}", "SSMS", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                // Get node information
                var nodeInfo = GetNodeInfo(node);
                
                // Build information message
                string info = $"Server: {nodeInfo.Server}\r\n" +
                             $"Database: {nodeInfo.Database}\r\n" +
                             $"Schema: {nodeInfo.Schema}\r\n" +
                             $"Table: {nodeInfo.Table}\r\n" +
                             $"URN Path: {nodeInfo.UrnPath}";

                // Try to get additional SMO table information
                var smoTableInfo = GetSmoTableInfo(node);
                if (!string.IsNullOrEmpty(smoTableInfo))
                {
                    info += "\r\n\r\n" + smoTableInfo;
                }

                MessageBox.Show(info, "SSMS Table Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"ShowTableNameCommand.Execute error: {ex.Message}\n{ex.StackTrace}");
                MessageBox.Show($"Error executing command: {ex.Message}", "SSMS Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private IObjectExplorerService GetObjectExplorerService()
        {
            try
            {
                // Use IServiceProvider to get the service
                var sp = _package as IServiceProvider;
                if (sp != null)
                {
                    return sp.GetService(typeof(IObjectExplorerService)) as IObjectExplorerService;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"GetObjectExplorerService error: {ex.Message}");
            }
            return null;
        }

        private NodeInfo GetNodeInfo(object node)
        {
            var nodeInfo = new NodeInfo();
            
            try
            {
                if (node is INodeInformation nodeInfoInterface)
                {
                    // Get URN path
                    nodeInfo.UrnPath = nodeInfoInterface.UrnPath ?? string.Empty;
                    
                    // Parse URN to extract server, database, schema, table
                    if (!string.IsNullOrEmpty(nodeInfo.UrnPath))
                    {
                        var parts = nodeInfo.UrnPath.Split('/');
                        foreach (var part in parts)
                        {
                            if (part.StartsWith("Server[@Name='", StringComparison.OrdinalIgnoreCase))
                            {
                                nodeInfo.Server = ExtractValue(part);
                            }
                            else if (part.StartsWith("Database[@Name='", StringComparison.OrdinalIgnoreCase))
                            {
                                nodeInfo.Database = ExtractValue(part);
                            }
                            else if (part.StartsWith("Schema[@Name='", StringComparison.OrdinalIgnoreCase))
                            {
                                nodeInfo.Schema = ExtractValue(part);
                            }
                            else if (part.StartsWith("Table[@Name='", StringComparison.OrdinalIgnoreCase))
                            {
                                nodeInfo.Table = ExtractValue(part);
                            }
                        }
                    }
                    
                    // Try to get name directly
                    if (string.IsNullOrEmpty(nodeInfo.Table))
                    {
                        nodeInfo.Table = nodeInfoInterface.InvariantName ?? nodeInfoInterface.Name ?? string.Empty;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"GetNodeInfo error: {ex.Message}");
            }
            
            return nodeInfo;
        }

        private string ExtractValue(string urnPart)
        {
            try
            {
                int start = urnPart.IndexOf("'", StringComparison.Ordinal) + 1;
                int end = urnPart.LastIndexOf("'", StringComparison.Ordinal);
                if (start > 0 && end > start)
                {
                    return urnPart.Substring(start, end - start);
                }
            }
            catch
            {
                // Ignore parsing errors
            }
            return string.Empty;
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
                if (node is INodeInformation nodeInfo)
                {
                    // Try to get context from UrnPath
                    var urnPath = nodeInfo.UrnPath ?? string.Empty;
                    if (urnPath.Contains("/Table[@Name='"))
                    {
                        return "Table";
                    }
                    
                    // Try NavigationContext
                    var navContext = nodeInfo.NavigationContext ?? string.Empty;
                    if (navContext.Contains("Table"))
                    {
                        return "Table";
                    }
                }
                
                // Fallback: use reflection
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

        private string GetSmoTableInfo(object node)
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
                        return $"SMO Information:\r\n" +
                               $"  Columns: {table.Columns.Count}\r\n" +
                               $"  Indexes: {table.Indexes.Count}\r\n" +
                               $"  Triggers: {table.Triggers.Count}\r\n";// +
                               //$"  Check Constraints: {table.CheckConstraints.Count}";
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"GetSmoTableInfo error: {ex.Message}");
            }

            return string.Empty;
        }

        // Helper class to hold node information
        private class NodeInfo
        {
            public string Server { get; set; } = string.Empty;
            public string Database { get; set; } = string.Empty;
            public string Schema { get; set; } = string.Empty;
            public string Table { get; set; } = string.Empty;
            public string UrnPath { get; set; } = string.Empty;
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
