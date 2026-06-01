using System;
using System.IO;

namespace SSMS_AI_Buddy.Services
{
    /// <summary>
    /// User-writable settings location. SSMS installs extensions under Program Files, which is not writable.
    /// </summary>
    internal static class SettingsPathService
    {
        private const string AppDataFolderName = "SsmsAIBuddy";

        public static string SettingsDirectory
        {
            get
            {
                var path = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    AppDataFolderName);
                Directory.CreateDirectory(path);
                return path;
            }
        }

        public static string GetSettingsFilePath(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
            {
                throw new ArgumentException("File name is required.", nameof(fileName));
            }

            return Path.Combine(SettingsDirectory, fileName);
        }

        /// <summary>
        /// Copies default JSON from the extension folder (read-only) into AppData when user settings are missing.
        /// </summary>
        public static bool TrySeedFromExtension(string fileName)
        {
            var targetPath = GetSettingsFilePath(fileName);
            if (File.Exists(targetPath))
            {
                return true;
            }

            var extensionBase = AppDomain.CurrentDomain.BaseDirectory;
            var candidates = new[]
            {
                Path.Combine(extensionBase, "File", fileName),
                Path.Combine(extensionBase, fileName),
            };

            foreach (var sourcePath in candidates)
            {
                if (!File.Exists(sourcePath))
                {
                    continue;
                }

                File.Copy(sourcePath, targetPath);
                return true;
            }

            return false;
        }
    }
}
