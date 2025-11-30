using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using Microsoft.VisualStudio.Shell;

namespace SsmsAiToolbar.Commands
{
    /// <summary>
    /// SSMS Object Explorer menu handler for table nodes
    /// This implements the SSMS LocalMenuHandler interface via reflection
    /// </summary>
    public class ObjectExplorerMenuHandler
    {
        private static object _menuHandlerInstance;
        private static Type _localMenuHandlerType;
        private static Type _menuItemType;
        private static Type _nodeInformationType;

        /// <summary>
        /// Initialize the menu handler - this should be called from the package
        /// </summary>
        public static void Initialize(AsyncPackage package)
        {
            try
            {
                Debug.WriteLine("ObjectExplorerMenuHandler: Attempting to initialize...");
                
                // Try to load the SSMS menu handler types via reflection
                var sqlWorkbenchAssembly = LoadSqlWorkbenchAssembly();
                if (sqlWorkbenchAssembly == null)
                {
                    Debug.WriteLine("ObjectExplorerMenuHandler: Failed to load SqlWorkbench.Interfaces assembly");
                    return;
                }

                // Look for LocalMenuHandler interface or similar
                _localMenuHandlerType = sqlWorkbenchAssembly.GetType("Microsoft.SqlServer.Management.UI.VSIntegration.ObjectExplorer.ILocalMenuHandler");
                if (_localMenuHandlerType == null)
                {
                    Debug.WriteLine("ObjectExplorerMenuHandler: ILocalMenuHandler type not found");
                    // Try alternative names
                    _localMenuHandlerType = sqlWorkbenchAssembly.GetType("Microsoft.SqlServer.Management.UI.VSIntegration.ObjectExplorer.LocalMenuHandler");
                }

                if (_localMenuHandlerType != null)
                {
                    Debug.WriteLine($"ObjectExplorerMenuHandler: Found menu handler type: {_localMenuHandlerType.FullName}");
                }
                else
                {
                    Debug.WriteLine("ObjectExplorerMenuHandler: Could not find menu handler type");
                }

                // Look for menu item types
                _menuItemType = sqlWorkbenchAssembly.GetType("Microsoft.SqlServer.Management.UI.VSIntegration.ObjectExplorer.MenuItem");
                _nodeInformationType = sqlWorkbenchAssembly.GetType("Microsoft.SqlServer.Management.UI.VSIntegration.ObjectExplorer.INodeInformation");

                Debug.WriteLine("ObjectExplorerMenuHandler: Initialization complete");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"ObjectExplorerMenuHandler: Initialization error: {ex.Message}\n{ex.StackTrace}");
            }
        }

        /// <summary>
        /// Get menu items for a node - this would be called by SSMS
        /// </summary>
        public static object GetMenuItems(object nodeInformation)
        {
            try
            {
                if (nodeInformation == null)
                {
                    return null;
                }

                // Check if this is a table node
                var nodeType = nodeInformation.GetType();
                var contextProperty = nodeType.GetProperty("Context");
                var context = contextProperty?.GetValue(nodeInformation)?.ToString();

                Debug.WriteLine($"ObjectExplorerMenuHandler.GetMenuItems: Context = {context}");

                if (context == "Table")
                {
                    // Create a menu item for our command
                    Debug.WriteLine("ObjectExplorerMenuHandler.GetMenuItems: Table node detected, creating menu item");
                    
                    // We would create menu items here, but this requires the SSMS menu item type
                    // For now, we'll return null and rely on VSCT registration
                    return null;
                }

                return null;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"ObjectExplorerMenuHandler.GetMenuItems error: {ex.Message}\n{ex.StackTrace}");
                return null;
            }
        }

        private static Assembly LoadSqlWorkbenchAssembly()
        {
            try
            {
                var possiblePaths = new[]
                {
                    @"C:\Program Files (x86)\Microsoft SQL Server Management Studio 20\Common7\IDE\Extensions\Application\SqlWorkbench.Interfaces.dll",
                    @"C:\Program Files (x86)\Microsoft SQL Server Management Studio 19\Common7\IDE\Extensions\Application\SqlWorkbench.Interfaces.dll",
                    @"C:\Program Files (x86)\Microsoft SQL Server Management Studio 18\Common7\IDE\Extensions\Application\SqlWorkbench.Interfaces.dll",
                    System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "SqlWorkbench.Interfaces.dll")
                };

                foreach (var path in possiblePaths)
                {
                    if (System.IO.File.Exists(path))
                    {
                        Debug.WriteLine($"ObjectExplorerMenuHandler: Loading SqlWorkbench.Interfaces from {path}");
                        return Assembly.LoadFrom(path);
                    }
                }

                // Try loading from current directory or GAC
                return Assembly.Load("SqlWorkbench.Interfaces, Version=15.0.0.0, Culture=neutral, PublicKeyToken=89845dcd8080cc91");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"ObjectExplorerMenuHandler: Error loading assembly: {ex.Message}");
                return null;
            }
        }
    }
}

