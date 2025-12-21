using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using SSMS_AI_Buddy.Models;
namespace SSMS_AI_Buddy.Services.SSMS
{
    internal sealed class SqlCompletionSource : ICompletionSource
    {
        private readonly ITextBuffer _buffer;
        private bool _disposed;

        public SqlCompletionSource(ITextBuffer buffer)
        {
            _buffer = buffer;
        }

        public void AugmentCompletionSession(
            ICompletionSession session,
            IList<CompletionSet> completionSets)
        {
            if (_disposed)
                return;

            if (!_buffer.Properties.TryGetProperty(
                typeof(SqlCompletionContext),
                out SqlCompletionContext context))
                return;

            ITrackingPoint triggerPoint = session.GetTriggerPoint(_buffer);
            if (triggerPoint == null)
                return;

            SnapshotPoint point = triggerPoint.GetPoint(_buffer.CurrentSnapshot);

            var completions = context.Items
                .Distinct()
                .Select(x => new Completion(x))
                .ToList();

            if (completions.Count == 0)
                return;

            var applicableTo = _buffer.CurrentSnapshot.CreateTrackingSpan(
                point.Position,
                0,
                SpanTrackingMode.EdgeInclusive);

            completionSets.Add(
                new CompletionSet(
                    "SqlAI",
                    "SqlAI",
                    applicableTo,
                    completions,
                    Enumerable.Empty<Completion>()));
        }

        public void Dispose()
        {
            _disposed = true;
        }
    }
}
