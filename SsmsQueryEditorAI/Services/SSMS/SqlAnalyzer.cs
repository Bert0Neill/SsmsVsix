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
            if (string.IsNullOrWhiteSpace(sql) || sql.Length < 10)
                return false;

            // Normalize spaces and remove trailing semicolon
            sql = sql.Trim().TrimEnd(';');

            // Regex to detect FROM or JOIN followed by optional schema.table and optional alias at the end
            // Examples matched:
            // FROM Address
            // FROM SalesLT.Address a
            // JOIN Customer c
            // JOIN SalesLT.Customer
            var pattern = @"\b(FROM|JOIN)\s+([a-zA-Z0-9_]+\.)?[a-zA-Z0-9_]+(\s+[a-zA-Z0-9_]+)?$";

            return Regex.IsMatch(sql, pattern, RegexOptions.IgnoreCase);
        }
    }
}
