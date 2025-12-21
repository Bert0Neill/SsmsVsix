using SSMS_AI_Buddy.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
namespace SSMS_AI_Buddy.Services.SSMS
{
    public static class SqlAnalyzer
    {
        // Pass in the schema info so we can fully qualify table names
        public static HashSet<string> ExtractTables(string sql, List<TableInfo> schemaTables)
        {
            var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            var regex = new Regex(@"\b(from|join)\s+([\[\]\w\.]+)",
                RegexOptions.IgnoreCase);

            foreach (Match m in regex.Matches(sql))
            {
                var raw = m.Groups[2].Value.Trim('[', ']');

                string schemaPart = null;
                string tablePart = null;

                var parts = raw.Split('.');
                if (parts.Length == 2)
                {
                    schemaPart = parts[0];
                    tablePart = parts[1];
                }
                else
                {
                    tablePart = parts[0];

                    // Try to look up schema from cached table info
                    var tableInfo = schemaTables.FirstOrDefault(t =>
                        string.Equals(t.Name, tablePart, StringComparison.OrdinalIgnoreCase));
                    if (tableInfo != null)
                        schemaPart = tableInfo.Schema;
                }

                if (!string.IsNullOrEmpty(schemaPart) && !string.IsNullOrEmpty(tablePart))
                    set.Add($"{schemaPart}.{tablePart}");
            }

            return set;
        }

        public static bool ShouldTrigger(string sql)
        {
            return sql.Length > 10 &&
                   Regex.IsMatch(sql, @"\b(from|join)\b", RegexOptions.IgnoreCase);
        }
    }
}
