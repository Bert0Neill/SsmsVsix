using Newtonsoft.Json;
using SSMS_AI_Buddy.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
namespace SSMS_AI_Buddy.Services.SSMS
{
    public static class RealtimeJoinSuggestionService
    {
        public static List<SqlSuggestion> BuildRealTimeJoinSuggestions(string schemaJson, string sql)
        {
            var suggestions = new List<SqlSuggestion>();

            if (string.IsNullOrWhiteSpace(schemaJson) || string.IsNullOrWhiteSpace(sql))
                return suggestions;

            // var relations = JsonSerializer.Deserialize<List<SchemaRelation>>(schemaJson);
            var relations = JsonConvert.DeserializeObject<List<SchemaRelation>>(schemaJson);

            if (relations == null || relations.Count == 0)
                return suggestions;

            var tables = ExtractTables(sql);

            foreach (var table in tables)
            {
                foreach (var rel in relations)
                {
                    if (string.Equals(rel.pkTable, table, StringComparison.OrdinalIgnoreCase))
                    {
                        suggestions.Add(CreateJoin(rel.fkTable, rel.fkColumn, rel.pkTable, rel.pkColumn));
                    }

                    if (string.Equals(rel.fkTable, table, StringComparison.OrdinalIgnoreCase))
                    {
                        suggestions.Add(CreateJoin(rel.pkTable, rel.pkColumn, rel.fkTable, rel.fkColumn));
                    }
                }
            }

            suggestions = suggestions
                .GroupBy(x => x.Suggestion)
                .Select(g => g.First())
                .ToList();

            return EnsureMinimumSuggestions(suggestions);
        }

        private static SqlSuggestion CreateJoin(string joinTable, string joinColumn, string baseTable, string baseColumn)
        {
            return new SqlSuggestion
            {
                Type = "Join",
                Suggestion = $"JOIN {joinTable} ON {joinTable}.{joinColumn} = {baseTable}.{baseColumn}",
                Description = $"Join {joinTable}"
            };
        }

        private static List<string> ExtractTables(string sql)
        {
            var tables = new List<string>();

            var matches = Regex.Matches(sql, @"\bfrom\s+([\w\.]+)", RegexOptions.IgnoreCase);

            foreach (Match match in matches)
                tables.Add(match.Groups[1].Value);

            return tables;
        }

        //private static List<SqlSuggestion> EnsureMinimumSuggestions(List<SqlSuggestion> suggestions)
        //{
        //    var variants = new[] { "JOIN", "INNER JOIN", "LEFT JOIN", "RIGHT JOIN" };

        //    if (suggestions.Count >= 4)
        //        return suggestions;

        //    var expanded = new List<SqlSuggestion>();

        //    foreach (var s in suggestions)
        //    {
        //        foreach (var v in variants)
        //        {
        //            expanded.Add(new SqlSuggestion
        //            {
        //                Type = "Join",
        //                Suggestion = s.Suggestion.Replace("JOIN", v),
        //                Description = s.Description
        //            });

        //            if (expanded.Count >= 4)
        //                return expanded;
        //        }
        //    }

        //    return expanded;
        //}

        private static List<SqlSuggestion> EnsureMinimumSuggestions(List<SqlSuggestion> suggestions)
        {
            return suggestions
                .Select(s => new SqlSuggestion
                {
                    Type = "Join",
                    Suggestion = s.Suggestion.Replace("JOIN", "INNER JOIN"),
                    Description = s.Description
                })
                .ToList();
        }
    }
}

public class SchemaRelation
{
    public string fkTable { get; set; }
    public string fkColumn { get; set; }
    public string pkTable { get; set; }
    public string pkColumn { get; set; }
}