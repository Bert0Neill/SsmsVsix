using Microsoft.SqlServer.Management.UI.VSIntegration.ObjectExplorer;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SmoService
{
    public class SmoObjectExplorer : IDisposable
    {
        private readonly object _instance;
        private Delegate _handlerDelegate;

        public SmoObjectExplorer(object instance)
        {
            _instance = instance ?? throw new ArgumentNullException(nameof(instance));
        }

        public void HookSelectionChanged(string methodName)
        {
            try
            {
                var objectExplorer = GetObjectExplorer();
                if (objectExplorer == null)
                {
                    MessageBox.Show("Object Explorer service not available", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                var objectExplorerType = objectExplorer.GetType();

                // Get Container
                var piContainer = objectExplorerType.GetProperty("Container", BindingFlags.Public | BindingFlags.Instance);
                var objectExplorerContainer = piContainer?.GetValue(objectExplorer);

                if (objectExplorerContainer == null)
                    throw new InvalidOperationException("Cannot access ObjectExplorer Container.");

                // Get Components
                var piComponents = objectExplorerContainer.GetType().GetProperty("Components", BindingFlags.Public | BindingFlags.Instance);
                var components = piComponents?.GetValue(objectExplorerContainer) as ComponentCollection;

                if (components == null)
                    throw new InvalidOperationException("Cannot access ObjectExplorer Components.");

                // Find ContextService
                object contextService = null;
                foreach (Component component in components)
                {
                    if (component.GetType().FullName.Contains("ContextService"))
                    {
                        contextService = component;
                        break;
                    }
                }

                if (contextService == null)
                    throw new NullReferenceException("ContextService not found in ObjectExplorer.");

                // Get ObjectExplorerContext
                var piContext = contextService.GetType().GetProperty("ObjectExplorerContext", BindingFlags.Public | BindingFlags.Instance);
                var objectExplorerContext = piContext?.GetValue(contextService);

                if (objectExplorerContext == null)
                    throw new InvalidOperationException("Cannot access ObjectExplorerContext.");

                // Subscribe to CurrentContextChanged
                var ei = objectExplorerContext.GetType().GetEvent("CurrentContextChanged", BindingFlags.Public | BindingFlags.Instance);
                if (ei == null)
                    throw new InvalidOperationException("CurrentContextChanged event not found.");

                var mi = _instance.GetType().GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
                if (mi == null)
                    throw new InvalidOperationException($"Method '{methodName}' not found in target instance.");

                _handlerDelegate = Delegate.CreateDelegate(ei.EventHandlerType, _instance, mi);
                ei.AddEventHandler(objectExplorerContext, _handlerDelegate);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Hooking Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private IObjectExplorerService GetObjectExplorer()
        {
            var serviceProvider = _instance as System.IServiceProvider;
            return serviceProvider?.GetService(typeof(IObjectExplorerService)) as IObjectExplorerService;
        }

        private void UnhookSelectionChanged()
        {
            try
            {
                if (_handlerDelegate == null)
                    return;

                var objectExplorer = GetObjectExplorer();
                if (objectExplorer == null)
                    return;

                var objectExplorerType = objectExplorer.GetType();
                var piContainer = objectExplorerType.GetProperty("Container", BindingFlags.Public | BindingFlags.Instance);
                var objectExplorerContainer = piContainer?.GetValue(objectExplorer);

                var piComponents = objectExplorerContainer?.GetType().GetProperty("Components", BindingFlags.Public | BindingFlags.Instance);
                var components = piComponents?.GetValue(objectExplorerContainer) as ComponentCollection;

                object contextService = null;
                foreach (Component component in components)
                {
                    if (component.GetType().FullName.Contains("ContextService"))
                    {
                        contextService = component;
                        break;
                    }
                }

                var piContext = contextService?.GetType().GetProperty("ObjectExplorerContext", BindingFlags.Public | BindingFlags.Instance);
                var objectExplorerContext = piContext?.GetValue(contextService);

                var ei = objectExplorerContext?.GetType().GetEvent("CurrentContextChanged", BindingFlags.Public | BindingFlags.Instance);
                ei?.RemoveEventHandler(objectExplorerContext, _handlerDelegate);

                _handlerDelegate = null;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Unbinding Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        public void Provider_SelectionChanged(object sender, NodesChangedEventArgs args)
        {
            try
            {
                if (args?.ChangedNodes == null || args.ChangedNodes.Count == 0)
                    return;

                var node = args.ChangedNodes[0];
                if (node == null)
                    return;

                string serverName = GetProperty(node, "ServerName");
                string databaseName = GetProperty(node, "DatabaseName");
                //string schemaName = GetProperty(node, "Schema");
                string tableName = GetProperty(node, "Name");
                string fulltableName = GetProperty(node, "InvariantName");

                string schemaName = GetProperty(node, "Schema") ??
                           GetProperty(node, "SchemaName") ??
                           GetProperty(node, "InvariantName");

                Debug.WriteLine($"Selected: {schemaName} - {databaseName} - {fulltableName} - {tableName}");

                // You can also update status bar etc.
                // StatusBarService.UpdateStatusBarText($"Selected: {serverName} - {databaseName} - {schemaName} - {tableName}");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        //public void Provider_SelectionChanged(object sender, NodesChangedEventArgs args)
        //{
        //    try
        //    {
        //        if (args?.ChangedNodes == null || args.ChangedNodes.Count == 0)
        //            return;

        //        var node = args.ChangedNodes[0];

        //        if (node == null)
        //            return;

        //        string serverName = GetProperty(node, "ServerName");
        //        string databaseName = GetProperty(node, "DatabaseName");
        //        //string schemaName = GetProperty(node, "Schema");
        //        string tableName = GetProperty(node, "Name");
        //        string fulltableName = GetProperty(node, "InvariantName");
        //        string schemaName = GetProperty(node, "Schema") ??
        //                   GetProperty(node, "SchemaName") ??
        //                   GetProperty(node, "InvariantName");

        //        string nodeType = GetProperty(node, "NodeType");
        //        string typeName = GetProperty(node, "UrnType");
        //        Debug.WriteLine($"Node Type: {nodeType} UrnType: {typeName} Schema: {schemaName} - {databaseName} - {fulltableName} - {tableName}");
        //        //Debug.WriteLine($"Schema: {schemaName} - {databaseName} - {fulltableName} - {tableName}");

        //        if (string.Equals(typeName, "View", StringComparison.OrdinalIgnoreCase))
        //        {
        //            // View-specific logic here
        //            Debug.WriteLine("A VIEW was selected.");
        //        }

        //        string urnString = GetProperty(node, "Urn");
        //        if (!string.IsNullOrEmpty(urnString))
        //        {
        //            Urn urn = new Urn(urnString);
        //            // Load the view object via SMO server instance
        //        }

        //        // You can also update status bar etc.
        //        // StatusBarService.UpdateStatusBarText($"Selected: {serverName} - {databaseName} - {schemaName} - {tableName}");

        //    }

        //    catch (Exception ex)

        //    {

        //        MessageBox.Show(ex.ToString(), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);

        //    }

        //}

        private string GetProperty(INodeInformation node, string propertyName, bool debugAll = true)
        {
            if (node == null) return null;

            try
            {
                if (debugAll)
                {
                    // Dump all properties and their values for debugging
                    var properties = node.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);
                    Debug.WriteLine($"Available properties for {node.GetType().Name}:");
                    foreach (var prop in properties)
                    {
                        try
                        {
                            var value = prop.GetValue(node);
                            Debug.WriteLine($"  {prop.Name}: {value}");
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine($"  {prop.Name}: Error: {ex.Message}");
                        }
                    }
                }

                // Use reflection to access the property dynamically
                var propertyInfo = node.GetType().GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);
                if (propertyInfo != null)
                {
                    var value = propertyInfo.GetValue(node);
                    return value?.ToString();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error accessing property '{propertyName}': {ex.Message}");
            }
            return null;
        }

        public void Dispose()
        {
            UnhookSelectionChanged();

            GC.SuppressFinalize(this); // clean up other resources if needed
        }
    }
}
