using Microsoft.SqlServer.Management.Sdk.Sfc;
using Microsoft.SqlServer.Management.UI.VSIntegration.ObjectExplorer;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;
using System.Windows.Forms;
using VSIX_AutoComplete_SSMS.Resources;

namespace VSIX_AutoComplete_SSMS.Classes
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
                    MessageBox.Show(Resource.Smo_No_Obj_Explorer, Resource.Grid_Error_Title, MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                var objectExplorerType = objectExplorer.GetType();

                // Get Container
                var piContainer = objectExplorerType.GetProperty("Container", BindingFlags.Public | BindingFlags.Instance);
                var objectExplorerContainer = piContainer?.GetValue(objectExplorer);

                if (objectExplorerContainer == null)
                    throw new InvalidOperationException(Resource.Smo_No_Container);

                // Get Components
                var piComponents = objectExplorerContainer.GetType().GetProperty("Components", BindingFlags.Public | BindingFlags.Instance);
                var components = piComponents?.GetValue(objectExplorerContainer) as ComponentCollection;

                if (components == null)
                    throw new InvalidOperationException(Resource.Smo_No_Components);

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
                    throw new NullReferenceException(Resource.Smo_No_ContextService);

                // Get ObjectExplorerContext
                var piContext = contextService.GetType().GetProperty("ObjectExplorerContext", BindingFlags.Public | BindingFlags.Instance);
                var objectExplorerContext = piContext?.GetValue(contextService);

                if (objectExplorerContext == null)
                    throw new InvalidOperationException(Resource.Smo_No_Access_ObjectExplorerContext);

                // Subscribe to CurrentContextChanged
                var ei = objectExplorerContext.GetType().GetEvent("CurrentContextChanged", BindingFlags.Public | BindingFlags.Instance);
                if (ei == null)
                    throw new InvalidOperationException(Resource.Smo_No_ContextChanged);

                var mi = _instance.GetType().GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
                if (mi == null)
                    throw new InvalidOperationException(string.Format(Resource.Smo_No_Method, methodName));

                _handlerDelegate = Delegate.CreateDelegate(ei.EventHandlerType, _instance, mi);
                ei.AddEventHandler(objectExplorerContext, _handlerDelegate);
            }
            catch (Exception ex)
            {
                SimpleLogger.Error(ex.ToString());
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
                MessageBox.Show(ex.Message, Resource.Grid_Hooking_Error, MessageBoxButtons.OK, MessageBoxIcon.Error);
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
                string tableName = GetProperty(node, "Name");
                string fulltableName = GetProperty(node, "InvariantName");
                string schemaName = GetProperty(node, "Schema") ??
                           GetProperty(node, "SchemaName") ??
                           GetProperty(node, "InvariantName");

                Debug.WriteLine(string.Format(Resource.Smo_Selected_Node, schemaName, databaseName, fulltableName, tableName));
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), Resource.Grid_Error_Title, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private string GetProperty(INodeInformation node, string propertyName, bool debugAll = true)
        {
            if (node == null) return null;

            try
            {
                if (debugAll)
                {
                    // Dump all properties and their values for debugging
                    var properties = node.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);
                    Debug.WriteLine(string.Format(Resource.Smo_Available_Properties, node.GetType().Name));
                    foreach (var prop in properties)
                    {
                        try
                        {
                            var value = prop.GetValue(node);
                            Debug.WriteLine(string.Format(Resource.Smo_Property_Value, prop.Name, value));
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine(string.Format(Resource.Smo_Property_Error, prop.Name, ex.Message));
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
                Debug.WriteLine($"{Resource.SMO_No_Property}: {ex.Message}");
            }
            return null;
        }

        public void Dispose()
        {
            UnhookSelectionChanged();
            GC.SuppressFinalize(this);
        }
    }
}
