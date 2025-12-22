//using SSMS_AI_Buddy.Services.SSMS;
//using System;
//using System.Collections.Generic;
//using System.Linq;

//namespace SSMS_AI_Buddy.Services.SSMS
//{
//    public static class JoinGraphBuilder
//    {
//        /// <summary>
//        /// Build a bidirectional graph of table relationships using fully qualified names (Schema.Table).
//        /// </summary>
//        public static Dictionary<string, HashSet<string>> BuildGraph(IEnumerable<TableInfo> tables)
//        {
//            var graph = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);

//            // Iterate through all tables and columns
//            foreach (var table in tables)
//            {
//                var tableKey = $"{table.Schema}.{table.Name}";

//                foreach (var column in table.Columns)
//                {
//                    var fk = column.Fk;
//                    if (fk != null)
//                    {
//                        var parentKey = $"{fk.Schema}.{fk.Table}";

//                        // Child → Parent
//                        AddEdge(graph, tableKey, parentKey);

//                        // Parent → Child (bidirectional)
//                        AddEdge(graph, parentKey, tableKey);
//                    }
//                }
//            }

//            return graph;
//        }

//        /// <summary>
//        /// Expand starting tables up to maxDepth to find all reachable tables.
//        /// </summary>
//        public static HashSet<string> Expand(IEnumerable<string> startTables, Dictionary<string, HashSet<string>> graph, int maxDepth = 2)
//        {
//            var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
//            var queue = new Queue<(string Table, int Depth)>();

//            foreach (var table in startTables)
//            {
//                visited.Add(table);
//                queue.Enqueue((table, 0));
//            }

//            while (queue.Count > 0)
//            {
//                var (current, depth) = queue.Dequeue();

//                if (depth >= maxDepth)
//                    continue;

//                if (!graph.TryGetValue(current, out var neighbors))
//                    continue;

//                foreach (var neighbor in neighbors)
//                {
//                    if (visited.Add(neighbor))
//                        queue.Enqueue((neighbor, depth + 1));
//                }
//            }

//            return visited;
//        }

//        /// <summary>
//        /// Adds an edge to the graph.
//        /// </summary>
//        private static void AddEdge(Dictionary<string, HashSet<string>> graph, string from, string to)
//        {
//            if (!graph.TryGetValue(from, out var set))
//            {
//                set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
//                graph[from] = set;
//            }

//            set.Add(to);
//        }
//    }
//}


using SSMS_AI_Buddy.Models;
using System;
using System.Collections.Generic;

namespace SSMS_AI_Buddy.Services.SSMS
{
    public static class JoinGraphBuilder
    {
        // Build a bidirectional graph of table relationships using fully qualified names
        public static Dictionary<string, HashSet<string>> BuildGraph(
            IEnumerable<ForeignKeyInfo> foreignKeys)
        {
            var graph = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);

            foreach (var fk in foreignKeys)
            {
                var child = $"{fk.Schema}.{fk.Table}";
                var parent = $"{fk.ReferencedSchema}.{fk.ReferencedTable}";

                // Child → Parent
                AddEdge(graph, child, parent);

                // Parent → Child (joins are bidirectional)
                AddEdge(graph, parent, child);
            }

            return graph;
        }

        // Expand starting tables up to maxDepth to find all reachable tables
        public static HashSet<string> Expand(IEnumerable<string> startTables, Dictionary<string, HashSet<string>> graph, int maxDepth = 2)
        {
            var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var queue = new Queue<(string Table, int Depth)>();

            foreach (var table in startTables)
            {
                // Make sure start table includes schema
                visited.Add(table);
                queue.Enqueue((table, 0));
            }

            while (queue.Count > 0)
            {
                var (current, depth) = queue.Dequeue();
                if (depth >= maxDepth)
                    continue;

                if (!graph.TryGetValue(current, out var neighbors))
                    continue;

                foreach (var neighbor in neighbors)
                {
                    if (visited.Add(neighbor))
                        queue.Enqueue((neighbor, depth + 1));
                }
            }

            return visited;
        }

        private static void AddEdge(
            Dictionary<string, HashSet<string>> graph,
            string from,
            string to)
        {
            if (!graph.TryGetValue(from, out var set))
            {
                set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                graph[from] = set;
            }

            set.Add(to);
        }
    }
}
