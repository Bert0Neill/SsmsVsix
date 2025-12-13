using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.TextManager.Interop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AI_Buddy.Services
{
    internal class EditorService
    {
        public EditorService() { }
        public async Task<string> GetSelectedTextAsync(AsyncPackage _package)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            var textManager = await _package.GetServiceAsync(typeof(SVsTextManager)) as IVsTextManager;
            if (textManager == null) return string.Empty;

            textManager.GetActiveView(1, null, out IVsTextView textView);
            if (textView == null) return string.Empty;

            textView.GetSelectedText(out string selectedText);
            return selectedText;
        }
    }
}
