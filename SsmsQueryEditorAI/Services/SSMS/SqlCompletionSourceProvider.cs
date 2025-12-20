using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

    using Microsoft.VisualStudio.Language.Intellisense;
    using Microsoft.VisualStudio.Text;
    using Microsoft.VisualStudio.Utilities;
    using System.ComponentModel.Composition;
namespace SSMS_AI_Buddy.Services.SSMS
{

    [Export(typeof(ICompletionSourceProvider))]
    [Name("SqlAICompletion")]
    [ContentType("SQL")]
    internal sealed class SqlCompletionSourceProvider : ICompletionSourceProvider
    {
        public ICompletionSource TryCreateCompletionSource(ITextBuffer textBuffer)
            => new SqlCompletionSource(textBuffer);
    }

}
