using System;
using System.Diagnostics;
using Process = System.Diagnostics.Process;

namespace SsmsEditorAI.Services
{
    internal class SsmsHelper
    {
        public SsmsHelper() { }

        public string RetrieveSsmsVersion()
        {
            try
            {
                System.Windows.Forms.Cursor.Current = System.Windows.Forms.Cursors.WaitCursor;
                string ssmsVersion = "Unknown";
                var exePath = Process.GetCurrentProcess().MainModule.FileName;
                var versionInfo = FileVersionInfo.GetVersionInfo(exePath);

                // Example:
                // SSMS 17.x → 14.x.y.z
                // SSMS 18.x → 15.x.y.z
                // SSMS 19.x → 19.x.y.z
                // SSMS 20.x → 20.x.y.z

                var fileVersion = versionInfo.FileVersion;  // "15.0.18338.0" etc.
                var majorVersion = versionInfo.ProductMajorPart;

                switch (majorVersion)
                {
                    case 14:
                        ssmsVersion = "SSMS 17";
                        break;
                    case 15:
                        ssmsVersion = "SSMS 18";
                        break;
                    case 19:
                        ssmsVersion = "SSMS 19";
                        break;
                    case 20:
                        ssmsVersion = "SSMS 20";
                        break;
                    case 21:
                        ssmsVersion = "SSMS 21";
                        break;
                    default:
                        ssmsVersion = "Unknown";
                        break;
                }

                return ssmsVersion;
            }
            catch (Exception ex)
            {
                LoggerService.Error($"{ex.Message}");
                return string.Empty;
            }
            finally
            {
                System.Windows.Forms.Cursor.Current = System.Windows.Forms.Cursors.Default;
            }
        }
    }
}
