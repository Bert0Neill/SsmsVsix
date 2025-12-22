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

        /*
         *SqlTriggerHelper.ShouldTrigger("SELECT * FROM SalesLT.Address");        // true
SqlTriggerHelper.ShouldTrigger("SELECT * FROM SalesLT.Address a");      // true
SqlTriggerHelper.ShouldTrigger("SELECT * FROM Address, Customer");      // true
SqlTriggerHelper.ShouldTrigger("SELECT * FROM Address a, Customer c");  // true
SqlTriggerHelper.ShouldTrigger("SELECT * FROM Address a WHERE");        // false
SqlTriggerHelper.ShouldTrigger("SELECT * FROM Address a JOIN Customer"); // true
SqlTriggerHelper.ShouldTrigger("SELECT * FROM Address a JOIN Customer c"); // true
SqlTriggerHelper.ShouldTrigger("SELECT * FROM Address a JOIN Customer c WHERE"); // false

         */
        public static bool ShouldTrigger(string sql)
        {
            if (string.IsNullOrWhiteSpace(sql) || sql.Length < 10)
                return false;

            // Normalize spaces and remove trailing semicolon
            sql = Regex.Replace(sql.Trim().TrimEnd(';'), @"\s+", " ");

            // Pattern matches:
            // - FROM t1
            // - FROM schema.t1
            // - FROM t1 a
            // - FROM t1 a, t2 b
            // - JOIN t2
            var pattern = @"
            \b(FROM|JOIN)\s+                       # Match FROM or JOIN
            (([a-zA-Z0-9_]+\.)?[a-zA-Z0-9_]+      # Optional schema + table
            (\s+[a-zA-Z0-9_]+)?                    # Optional alias
            (\s*,\s*([a-zA-Z0-9_]+\.)?[a-zA-Z0-9_]+(\s+[a-zA-Z0-9_]+)?)*)?$ # Optional comma-separated tables
        ";

            return Regex.IsMatch(sql, pattern, RegexOptions.IgnoreCase | RegexOptions.IgnorePatternWhitespace);
        }
    }
}
