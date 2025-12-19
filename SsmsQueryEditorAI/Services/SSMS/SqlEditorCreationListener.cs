using Microsoft.VisualStudio.Editor;
    using Microsoft.VisualStudio.Language.Intellisense;
    using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell;
    using Microsoft.VisualStudio.Text.Editor;
    using Microsoft.VisualStudio.TextManager.Interop;
    using Microsoft.VisualStudio.Utilities;
using System;
using System.Collections.Generic;
    using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
namespace SSMS_AI_Buddy.Services.SSMS
{
    

    [Export(typeof(IVsTextViewCreationListener))]
    [ContentType("SQL Server")]
    [TextViewRole(PredefinedTextViewRoles.Editable)]
    internal sealed class SqlEditorCreationListener : IVsTextViewCreationListener
    {
        [Import] internal IVsEditorAdaptersFactoryService AdapterService;
        [Import] internal ICompletionBroker CompletionBroker;

        public void VsTextViewCreated(IVsTextView textViewAdapter)
        {
            var view = AdapterService.GetWpfTextView(textViewAdapter);
            if (view == null)
                return;

            // ✅ Resolve your package correctly
            var package = ServiceProvider.GlobalProvider
                .GetService(typeof(SsmsQueryEditorAIPackage)) as SsmsQueryEditorAIPackage;

            if (package == null)
                return;

            var filter = new AIEditorService(
    package,
    textViewAdapter);


            textViewAdapter.AddCommandFilter(filter, out IOleCommandTarget next);
            filter._nextFilter = next;
        }
    }

}
