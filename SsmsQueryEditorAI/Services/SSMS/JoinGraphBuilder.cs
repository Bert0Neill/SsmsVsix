using SSMS_AI_Buddy.Models;
using System.Collections.Generic;

namespace SSMS_AI_Buddy.Services.SSMS
{
    public static class JoinGraphBuilder
    {
        public static Dictionary<string, List<string>> BuildGraph(List<ForeignKeyInfo> fks)
        {
            var graph = new Dictionary<string, List<string>>(System.StringComparer.OrdinalIgnoreCase);

            foreach (var fk in fks)
            {
                if (!graph.ContainsKey(fk.Table))
                    graph[fk.Table] = new List<string>();

                graph[fk.Table].Add(fk.ReferencedTable);
            }

            return graph;
        }

        public static HashSet<string> Expand(HashSet<string> seeds, Dictionary<string, List<string>> graph)
        {
            var result = new HashSet<string>(seeds, System.StringComparer.OrdinalIgnoreCase);
            var queue = new Queue<string>(seeds);

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();

                if (!graph.ContainsKey(current))
                    continue;

                foreach (var next in graph[current])
                {
                    if (result.Add(next))
                        queue.Enqueue(next);
                }
            }

            return result;
        }
    }
}
