using Newtonsoft.Json;
using System;
using System.IO;

namespace VSIX_AutoComplete_SSMS.Classes
{
    internal class FileService
    {
        public void SaveToJson<T>(T obj, string filePath)
        {
            try
            {
                System.Windows.Forms.Cursor.Current = System.Windows.Forms.Cursors.WaitCursor;

                // Create directory if it doesn't exist
                string directoryPath = Path.GetDirectoryName(filePath);
                if (!string.IsNullOrEmpty(directoryPath) && !Directory.Exists(directoryPath))
                {
                    Directory.CreateDirectory(directoryPath);
                }

                // Serialize and write to file
                string json = JsonConvert.SerializeObject(obj, Formatting.Indented);
                File.WriteAllText(filePath, json);
            }
            catch (Exception ex)
            {
                SimpleLogger.Error($"{ex.Message}");
            }
            finally
            {
                System.Windows.Forms.Cursor.Current = System.Windows.Forms.Cursors.Default;
            }
        }

        public T LoadFromJson<T>(string filePath)
        {
            if (!File.Exists(filePath)) return default;
            string json = File.ReadAllText(filePath);
            return JsonConvert.DeserializeObject<T>(json);
        }
    }
}
