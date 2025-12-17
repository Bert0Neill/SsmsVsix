using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;
using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading;

namespace YourExtensionNamespace
{
    public class SsmsConnectionService : IDisposable
    {
        private DTE2 _dte;
        private bool _isDisposed = false;
        private Assembly _sqlStudioAssembly;
        private Type _sqlEditorType;
        private Type _connectionInfoType;

        public event Action<ConnectionInfo> ConnectionRetrieved;
        public event Action<string> DebugMessage;

        public void Initialize()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            try
            {
                // Get DTE
                _dte = (DTE2)Package.GetGlobalService(typeof(DTE));
                if (_dte == null)
                {
                    LogDebug("DTE service not available");
                    return;
                }

                LogDebug($"DTE version: {_dte.Version}");

                // Load SSMS assembly types
                LoadSsmsTypes();

                LogDebug("SSMS Connection Service initialized");
            }
            catch (Exception ex)
            {
                LogDebug($"Error initializing: {ex.Message}");
            }
        }

        private void LoadSsmsTypes()
        {
            try
            {
                // Find SSMS assembly in current domain
                _sqlStudioAssembly = FindSsmsAssembly();
                if (_sqlStudioAssembly == null)
                {
                    LogDebug("SSMS assembly not found in AppDomain");
                    return;
                }

                LogDebug($"Found SSMS assembly: {_sqlStudioAssembly.GetName().Name}");

                // Try to get SqlEditor type
                _sqlEditorType = _sqlStudioAssembly.GetType(
                    "Microsoft.SqlServer.Management.UI.VSIntegration.Editor.SqlEditor");

                if (_sqlEditorType == null)
                {
                    // Try alternative names
                    _sqlEditorType = _sqlStudioAssembly.GetTypes()
                        .FirstOrDefault(t => t.Name == "SqlEditor" ||
                                           t.FullName.Contains("SqlEditor") &&
                                           t.FullName.Contains("Microsoft.SqlServer"));
                }

                if (_sqlEditorType != null)
                {
                    LogDebug($"Found SqlEditor type: {_sqlEditorType.FullName}");
                }
                else
                {
                    LogDebug("Could not find SqlEditor type");
                }

                // Try to get ConnectionInfo type
                _connectionInfoType = _sqlStudioAssembly.GetType(
                    "Microsoft.SqlServer.Management.UI.Connection.ConnectionInfo");

                if (_connectionInfoType == null)
                {
                    _connectionInfoType = _sqlStudioAssembly.GetTypes()
                        .FirstOrDefault(t => t.Name == "ConnectionInfo" &&
                                           t.FullName.Contains("Microsoft.SqlServer"));
                }
            }
            catch (Exception ex)
            {
                LogDebug($"Error loading SSMS types: {ex.Message}");
            }
        }

        private Assembly FindSsmsAssembly()
        {
            try
            {
                // Look for SSMS assemblies
                var assemblies = AppDomain.CurrentDomain.GetAssemblies();

                // Try common SSMS assembly names
                var possibleNames = new[]
                {
                    "Microsoft.SqlServer.Management.SqlStudio",
                    "Microsoft.SqlServer.Management.UI.VSIntegration",
                    "Microsoft.SqlServer.Management.UI",
                    "Microsoft.SqlServer.ConnectionInfo"
                };

                foreach (var assembly in assemblies)
                {
                    var name = assembly.GetName().Name;
                    foreach (var possibleName in possibleNames)
                    {
                        if (name.Contains(possibleName))
                        {
                            return assembly;
                        }
                    }
                }

                return null;
            }
            catch
            {
                return null;
            }
        }

        public ConnectionInfo GetActiveConnection()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            var connectionInfo = new ConnectionInfo();

            try
            {
                if (_dte?.ActiveDocument == null)
                {
                    LogDebug("No active document");
                    return connectionInfo;
                }

                var document = _dte.ActiveDocument;
                if (!IsSqlDocument(document))
                {
                    LogDebug("Active document is not SQL");
                    return connectionInfo;
                }

                // Get the actual connection from SSMS objects
                connectionInfo = GetConnectionFromDocument(document);

                // Notify that connection was retrieved
                ConnectionRetrieved?.Invoke(connectionInfo);

                return connectionInfo;
            }
            catch (Exception ex)
            {
                LogDebug($"Error getting active connection: {ex.Message}");
                return connectionInfo;
            }
        }

        private ConnectionInfo GetConnectionFromDocument(Document document)
        {
            var info = new ConnectionInfo();

            try
            {
                // Method 1: Try to get via SqlEditor service
                var connection = GetConnectionViaSsmsService();
                if (connection != null)
                {
                    return ExtractConnectionInfo(connection);
                }

                // Method 2: Try to get from document's object
                if (_sqlEditorType != null)
                {
                    connection = GetConnectionFromDocumentObject(document);
                    if (connection != null)
                    {
                        return ExtractConnectionInfo(connection);
                    }
                }

                // Method 3: Try to get via reflection on DTE
                connection = GetConnectionViaReflection(document);
                if (connection != null)
                {
                    return ExtractConnectionInfo(connection);
                }
            }
            catch (Exception ex)
            {
                LogDebug($"GetConnectionFromDocument error: {ex.Message}");
            }

            return info;
        }

        private object GetConnectionViaSsmsService()
        {
            try
            {
                // Try to get SqlEditorService
                var serviceProvider = (IServiceProvider)Package.GetGlobalService(typeof(IServiceProvider));
                if (serviceProvider == null) return null;

                // Try different service types
                var serviceTypes = new[]
                {
                    "Microsoft.SqlServer.Management.UI.VSIntegration.Editor.ISqlEditorService",
                    "Microsoft.SqlServer.Management.UI.VSIntegration.Editor.SqlEditorService"
                };

                foreach (var typeName in serviceTypes)
                {
                    try
                    {
                        var serviceType = _sqlStudioAssembly?.GetType(typeName);
                        if (serviceType != null)
                        {
                            var service = serviceProvider.GetService(serviceType);
                            if (service != null)
                            {
                                // Try to get active editor
                                var activeEditor = GetActiveEditorFromService(service);
                                if (activeEditor != null)
                                {
                                    // Get connection from editor
                                    return GetConnectionFromEditor(activeEditor);
                                }
                            }
                        }
                    }
                    catch { }
                }

                return null;
            }
            catch (Exception ex)
            {
                LogDebug($"GetConnectionViaSsmsService error: {ex.Message}");
                return null;
            }
        }

        private object GetActiveEditorFromService(object service)
        {
            try
            {
                var serviceType = service.GetType();

                // Try ActiveSqlEditor property
                var activeEditorProp = serviceType.GetProperty("ActiveSqlEditor",
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (activeEditorProp != null)
                {
                    return activeEditorProp.GetValue(service);
                }

                // Try GetActiveSqlEditor method
                var getActiveMethod = serviceType.GetMethod("GetActiveSqlEditor",
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                    null, Type.EmptyTypes, null);
                if (getActiveMethod != null)
                {
                    return getActiveMethod.Invoke(service, null);
                }

                return null;
            }
            catch
            {
                return null;
            }
        }

        private object GetConnectionFromDocumentObject(Document document)
        {
            try
            {
                // Get document's Object property
                var objectProperty = document.GetType().GetProperty("Object",
                    BindingFlags.Instance | BindingFlags.Public);
                if (objectProperty == null) return null;

                var docObject = objectProperty.GetValue(document);
                if (docObject == null) return null;

                // Check if it's a SqlEditor
                if (!_sqlEditorType.IsInstanceOfType(docObject))
                {
                    LogDebug("Document object is not SqlEditor");
                    return null;
                }

                // Get connection from SqlEditor
                return GetConnectionFromEditor(docObject);
            }
            catch (Exception ex)
            {
                LogDebug($"GetConnectionFromDocumentObject error: {ex.Message}");
                return null;
            }
        }

        private object GetConnectionFromEditor(object editor)
        {
            try
            {
                var editorType = editor.GetType();

                // Try different connection property names
                var connectionPropNames = new[]
                {
                    "Connection",
                    "ActiveConnection",
                    "CurrentConnection",
                    "SqlConnection"
                };

                foreach (var propName in connectionPropNames)
                {
                    var prop = editorType.GetProperty(propName,
                        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    if (prop != null)
                    {
                        var connection = prop.GetValue(editor);
                        if (connection != null)
                        {
                            LogDebug($"Found connection via property: {propName}");
                            return connection;
                        }
                    }
                }

                return null;
            }
            catch (Exception ex)
            {
                LogDebug($"GetConnectionFromEditor error: {ex.Message}");
                return null;
            }
        }

        private object GetConnectionViaReflection(Document document)
        {
            try
            {
                // Try to walk through the object graph to find connection
                // This is a more aggressive approach

                // Get the document's window
                var window = document.ActiveWindow;
                if (window == null) return null;

                // Try to get window's object
                var windowObject = window.Object;
                if (windowObject == null) return null;

                // Look for a connection in window object
                return FindConnectionInObject(windowObject);
            }
            catch
            {
                return null;
            }
        }

        private object FindConnectionInObject(object obj, int depth = 0)
        {
            if (depth > 3) return null; // Prevent infinite recursion

            try
            {
                var objType = obj.GetType();
                var typeName = objType.FullName ?? "";

                // Check if this looks like a connection object
                if (typeName.Contains("Connection") &&
                    !typeName.Contains("String") &&
                    !typeName.Contains("Manager"))
                {
                    // Check if it has server/database properties
                    var serverProp = objType.GetProperty("ServerInstance") ??
                                    objType.GetProperty("ServerName");
                    var dbProp = objType.GetProperty("DatabaseName") ??
                                objType.GetProperty("Database");

                    if (serverProp != null || dbProp != null)
                    {
                        return obj;
                    }
                }

                // Recursively check properties
                var properties = objType.GetProperties(BindingFlags.Instance | BindingFlags.Public);
                foreach (var prop in properties)
                {
                    try
                    {
                        // Skip indexers and complex types
                        if (prop.GetIndexParameters().Length > 0) continue;

                        var value = prop.GetValue(obj);
                        if (value == null) continue;

                        // Skip strings and simple types
                        if (value.GetType().IsPrimitive ||
                            value is string ||
                            value is DateTime) continue;

                        var found = FindConnectionInObject(value, depth + 1);
                        if (found != null) return found;
                    }
                    catch { }
                }

                return null;
            }
            catch
            {
                return null;
            }
        }

        private ConnectionInfo ExtractConnectionInfo(object connection)
        {
            var info = new ConnectionInfo();

            if (connection == null) return info;

            try
            {
                var connType = connection.GetType();
                LogDebug($"Connection type: {connType.FullName}");

                // Debug: List all properties
                var props = connType.GetProperties(BindingFlags.Instance | BindingFlags.Public);
                foreach (var prop in props)
                {
                    try
                    {
                        var value = prop.GetValue(connection);
                        LogDebug($"  {prop.Name}: {value}");
                    }
                    catch { }
                }

                // Extract server name
                var serverProp = GetProperty(connType, "ServerInstance", "ServerName", "DataSource", "Server");
                if (serverProp != null)
                {
                    info.ServerName = serverProp.GetValue(connection)?.ToString();
                }

                // Extract database name
                var dbProp = GetProperty(connType, "DatabaseName", "Database", "InitialCatalog");
                if (dbProp != null)
                {
                    info.DatabaseName = dbProp.GetValue(connection)?.ToString();
                }

                // Check if connected
                var stateProp = GetProperty(connType, "IsOpen", "State", "IsConnected");
                if (stateProp != null)
                {
                    var stateValue = stateProp.GetValue(connection);
                    if (stateValue is bool boolState)
                    {
                        info.IsConnected = boolState;
                    }
                    else
                    {
                        var stateString = stateValue?.ToString();
                        info.IsConnected = stateString == "Open" || stateString == "Connected";
                    }
                }

                // Get connection string if available
                var connStringProp = connType.GetProperty("ConnectionString",
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (connStringProp != null)
                {
                    info.ConnectionString = connStringProp.GetValue(connection)?.ToString();
                }

                // Default database if server exists
                if (!string.IsNullOrEmpty(info.ServerName) && string.IsNullOrEmpty(info.DatabaseName))
                {
                    info.DatabaseName = "master";
                }

                info.LastChecked = DateTime.Now;

                LogDebug($"Extracted: {info.ServerName}.{info.DatabaseName} (Connected: {info.IsConnected})");
            }
            catch (Exception ex)
            {
                LogDebug($"ExtractConnectionInfo error: {ex.Message}");
            }

            return info;
        }

        private PropertyInfo GetProperty(Type type, params string[] propertyNames)
        {
            foreach (var name in propertyNames)
            {
                var prop = type.GetProperty(name,
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (prop != null) return prop;
            }
            return null;
        }

        private bool IsSqlDocument(Document doc)
        {
            if (doc == null) return false;

            return doc.Name.EndsWith(".sql", StringComparison.OrdinalIgnoreCase) ||
                   doc.FullName.EndsWith(".sql", StringComparison.OrdinalIgnoreCase);
        }

        private void LogDebug(string message)
        {
            Debug.WriteLine($"[SsmsConnectionService] {message}");
            DebugMessage?.Invoke(message);
        }

        public void Dispose()
        {
            if (_isDisposed) return;

            _isDisposed = true;
            LogDebug("Disposed");
        }
    }

    public class ConnectionInfo
    {
        public string ServerName { get; set; }
        public string DatabaseName { get; set; }
        public string ConnectionString { get; set; }
        public bool IsConnected { get; set; }
        public DateTime LastChecked { get; set; }

        public override string ToString()
        {
            if (IsConnected && !string.IsNullOrEmpty(ServerName))
            {
                return $"{ServerName}.{DatabaseName}";
            }
            return "[Not Connected]";
        }
    }
}