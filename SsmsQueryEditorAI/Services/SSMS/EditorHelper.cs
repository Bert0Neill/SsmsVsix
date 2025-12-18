using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SSMS_AI_Buddy.Services.SSMS
{
    using Microsoft.VisualStudio.Shell;
    using Microsoft.VisualStudio.TextManager.Interop;

    namespace SSMS_AI_Buddy.Services.SSMS
    {
        public static class EditorHelper
        {
            public static string GetCurrentEditorText()
            {
                ThreadHelper.ThrowIfNotOnUIThread();

                var mgr = (IVsTextManager)
                    ServiceProvider.GlobalProvider.GetService(typeof(SVsTextManager));

                if (mgr == null)
                    return string.Empty;

                IVsTextView view;
                mgr.GetActiveView(1, null, out view);

                if (view == null)
                    return string.Empty;

                IVsTextLines lines;
                view.GetBuffer(out lines);

                int lastLine, lastIndex;
                lines.GetLastLineIndex(out lastLine, out lastIndex);

                string text;
                lines.GetLineText(0, 0, lastLine, lastIndex, out text);

                return text ?? string.Empty;
            }
        }
    }

}
