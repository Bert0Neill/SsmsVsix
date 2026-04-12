using System;
using System.ComponentModel;

namespace SSMS_AI_Buddy.Models
{
    public class AIProperties
    {
        [Category("AI Provider")]
        [DisplayName("Name of AI provider")]
        [Description("For your reference, the name of the prompt provider.")]
        public string AIProvider { get; set; } = "DeepSeek";

        [Category("AI Provider")]
        [DisplayName("Prompt API URL")]
        [Description("The Prompt URL that will get called with your prompt.")]
        public string AIPromptURL { get; set; } = "http://localhost:11434/api/generate";

        [Category("AI Provider")]
        [DisplayName("API Key")]
        [Description("The key may not be required for internal server prompts.")]
        public string AIPromptKey { get; set; }

        [Category("AI Provider")]
        [DisplayName("LLM Name")]
        [Description("For your reference, what LLM model your Hoster is using (for e.g deepseek-r1:5b)")]
        public string PromptLLMName { get; set; } = "deepseek-r1:1.5b";

        [Category("AI Provider")]
        [DisplayName("Streaming Response")]
        [Description("Do you want to stream the prompt response in real-time.")]
        public bool IsPromptResponseStreaming { get; set; } = false;

        [Category("Coding")]
        [DisplayName("SSMS Version")]
        [Description("What version of SSMS are you requesting answers from AI prompt to execute on")]
        public string SsmsVersion { get; set; } = "SSMS 19+";

        [Category("Coding")]
        [DisplayName("SQL Language")]
        [Description("What SQL based procedural language are you requesting answers from AI prompt to execute against")]
        public string SqlVersion { get; set; } = "TSQL";

        [Category("General Info")]
        [DisplayName("Last Modified")]
        [Description("Whan you last updated the properties.")]
        [ReadOnly(true)]
        public DateTime DateLastModified { get; set; } = DateTime.Now;

        [Category("General Info")]
        [DisplayName("Last Prompt")]
        [Description("The last prompt you made.")]
        [ReadOnly(true)]
        public string LastPrompt { get; set; }

        [Category("General Info")]
        [DisplayName("Settings Filename")]
        [Description("The file where these settings are saved to (readonly).")]
        [ReadOnly(true)]
        public string SettingsFilename { get; set; } = "AI_Buddy_SSMS_Settings.json";

        /******************************************
         * Hidden Properties
         ******************************************/
        [Browsable(false)]
        [Category("General Info")]
        [DisplayName("Reset Filename")]
        [Description("The file holds the previous settings (readonly).")]
        [ReadOnly(true)]
        public string ResetFilename { get; set; } = "AI_Buddy_SSMS_Settings_Reset.json";

        [Browsable(false)]
        [Category("Coding")]
        [DisplayName("Realtime SQL Suggestion (Joins)")]
        [Description("Use existing schema instead of AI to generate suggestions for JOINs when typing in editor")]
        public bool IsRealTime { get; set; } = false;

        /******************************************
         * ReadOnly System Prompts
         ******************************************/

        [Browsable(true)]
        [Category("System Prompt")]
        [DisplayName("Where")]
        [Description("The 'Where' system prompt, that indicates to AI what rules you want AI to adhere too when generating the response. Edit in JSON file or delete JSON file to restart with defaults again")]
        [ReadOnly(true)]
        public string WherePrompt { get; set; } = @"No thinking. You are a SQL Server WHERE clause completion engine.
                                                    OUTPUT REQUIREMENTS:
                                                    - Output ONLY valid JSON.
                                                    - Output MUST be a JSON array with EXACTLY one object.
                                                    - No markdown.
                                                    - No explanations.
                                                    - No comments.
                                                    - No trailing commas.

                                                    JSON FORMAT:
                                                    [
                                                      { ""suggestion"": "" WHERE ..."" }
                                                    ]

                                                    RULES:
                                                    - Return ONLY the SQL fragment to append.
                                                    - NEVER return a full query.
                                                    - NEVER output SELECT, FROM, or JOIN.
                                                    - The fragment MUST start with WHERE.
                                                    - The fragment must be valid SQL Server syntax.
                                                    - Return between 4 and 8 suggestions.

                                                    COLUMN SAFETY:
                                                    - Use only tables, aliases, and columns present in the provided SQL and database Schema only.
                                                    - Do NOT invent schema objects.

                                                    GOOD OUTPUT:
                                                     [
                                                      {
                                                        ""suggestion"": ""WHERE SalesLT.Address.City = 'London'"",
                                                        ""description"": ""Filter addresses by city""
                                                      },
                                                      {
                                                        ""suggestion"": ""WHERE SalesLT.Address.CountryRegion = 'United States'"",
                                                        ""description"": ""Filter addresses by country or region""
                                                      },
                                                      {
                                                        ""suggestion"": ""WHERE SalesLT.Address.PostalCode = '98052'"",
                                                        ""description"": ""Filter addresses by postal code""
                                                      }
                                                    ]";

        [Browsable(true)]
        [Category("System Prompt")]
        [DisplayName(@"Group\Having")]
        [Description(@"The 'Group\Having' system prompt, that indicates to AI what rules you want AI to adhere too when generating the response. Edit in JSON file or delete JSON file to restart with defaults again")]
        [ReadOnly(true)]
        public string GroupHavingPrompt { get; set; } = @"No thinking. You are a SQL Server Group By\Having clause completion engine.
                                                            STRICT RULES:
                                                            - Output ONLY valid JSON.
                                                            - Output MUST be a COMPLETE JSON array.
                                                            - Do NOT include markdown, code fences, comments, or extra text.
                                                            - Do NOT include trailing commas.
                                                            - Do NOT explain reasoning.
                                                            - Do NOT output thoughts.
                                                            - Return between 4 and 8 suggestions.

                                                            COMPLETION RULES:
                                                            - You are completing an EXISTING query; the user keeps their SQL. Output ONLY the next fragment to append.
                                                            - DO NOT rewrite the full query.
                                                            - DO NOT duplicate SELECT ... FROM ... for the whole statement.
                                                            - The JSON ""suggestion"" field MUST NOT start with SELECT.
                                                            - Output fragments from GROUP BY, HAVING, ORDER BY, WHERE, or a comma + column.
                                                            - Prefer short fragments that can be appended directly after the cursor position.

                                                            GROUPING RULES:
                                                            - Suggest GROUP BY clauses using likely grouping columns.
                                                            - Prefer columns typically used for grouping (IDs, names, categories, dates).
                                                            - Suggest multiple grouping combinations when sensible.

                                                            HAVING RULES:
                                                            - Suggest HAVING clauses using aggregate functions.
                                                            - Assume HAVING is used after a GROUP BY.
                                                            - Use common aggregates: COUNT, SUM, AVG, MIN, MAX.
                                                            - Provide several comparison variations (> , < , >= , <=).

                                                            GOOD examples:
                                                            - ""GROUP BY CustomerID""
                                                            - ""GROUP BY CustomerID, OrderDate""
                                                            - ""GROUP BY CategoryID""
                                                            - ""HAVING COUNT(*) > 1""
                                                            - ""HAVING SUM(TotalAmount) > 1000""
                                                            - ""HAVING AVG(Price) >= 50""
                                                            - ""HAVING COUNT(DISTINCT CustomerID) > 5""
                                                            - "", CustomerID""

                                                            BAD examples:
                                                            - Anything starting with SELECT, UPDATE, DELETE, INSERT.
                                                            - Full standalone queries.";

        [Browsable(true)]
        [Category("System Prompt")]
        [DisplayName(@"Join")]
        [Description("The 'Join' system prompt, that indicates to AI what rules you want AI to adhere too when generating the response. Edit in JSON file or delete JSON file to restart with defaults again")]
        [ReadOnly(true)]
        public string JoinPrompt { get; set; } = @"You are a SQL JOIN autocomplete engine.
                                                    Return ONLY valid JSON.

                                                    OUTPUT FORMAT
                                                    [
                                                      {""type"":""Join"",""suggestion"":""JOIN table t ON condition"",""description"":""short explanation""},
                                                      {""type"":""Join"",""suggestion"":""LEFT JOIN table t ON condition"",""description"":""short explanation""},
                                                      {""type"":""Join"",""suggestion"":""Right JOIN table t ON condition"",""description"":""short explanation""},
                                                      {""type"":""Join"",""suggestion"":""Cross JOIN table t ON condition"",""description"":""short explanation""}
                                                    ]

                                                    RULES
                                                    - Output MUST be a valid JSON array.
                                                    - Output MUST contain between 4 and 8 json array items.
                                                    - NEVER output fewer than 4 items.
                                                    - Suggestions MUST start with JOIN, INNER JOIN, LEFT JOIN, or CROSS JOIN.
                                                    - Only output JOIN fragments, never full SELECT statements.
                                                    - Do not rewrite the user query.
                                                    - Do not output explanations or markdown.

                                                    JOIN GENERATION
                                                    - Make a suggestion for every table relationship supplied in prompt.
                                                    - Use foreign key relationships from the schema.
                                                    - If multiple joins are possible, return suggestions for each.
                                                    - If fewer than 4 joins exist, create variants using JOIN, INNER JOIN, LEFT JOIN, CROSS JOIN.
                                                    - Reuse the same ON condition when only the join type differs.

                                                    ALIAS RULE
                                                    - If the base table has an alias, use that alias ";

        //public string JoinPrompt { get; set; } = @"No thinking. You are a backend SQL JOIN completion engine.
        //                                            STRICT RULES:
        //                                            - Output ONLY valid JSON.
        //                                            - Output MUST be a COMPLETE JSON array.
        //                                            - Do NOT include markdown, code fences, comments, or extra text.
        //                                            - Do NOT include trailing commas.
        //                                            - Do NOT explain reasoning.
        //                                            - Do NOT output thoughts.
        //                                            - Return between 4 and 8 suggestions.

        //                                            COMPLETION RULES:
        //                                            - You are completing an EXISTING SQL query; the user keeps their SQL.
        //                                            - Output ONLY the next fragment to append at the cursor position.
        //                                            - DO NOT rewrite the full query.
        //                                            - DO NOT duplicate SELECT ... FROM ... for the whole statement.
        //                                            - The JSON ""suggestion"" field MUST NOT start with SELECT.
        //                                            - Suggestions MUST begin with a JOIN clause.

        //                                            JOIN RULES:
        //                                            - Suggest JOIN clauses only.
        //                                            - When a relationship is likely, include several join kinds:
        //                                              INNER JOIN, LEFT JOIN, and when sensible RIGHT JOIN or FULL OUTER JOIN.
        //                                            - Use the same table alias and ON condition when only the join type differs.
        //                                            - CROSS JOIN should only be suggested if a cross product is appropriate.
        //                                            - Prefer short fragments that can be appended directly after the current query.

        //                                            GOOD examples:
        //                                            - ""INNER JOIN SalesLT.CustomerAddress AS ca ON a.AddressID = ca.AddressID""
        //                                            - ""LEFT JOIN SalesLT.CustomerAddress AS ca ON a.AddressID = ca.AddressID""
        //                                            - ""RIGHT JOIN SalesLT.CustomerAddress AS ca ON a.AddressID = ca.AddressID""
        //                                            - ""FULL OUTER JOIN SalesLT.CustomerAddress AS ca ON a.AddressID = ca.AddressID""

        //                                            BAD examples:
        //                                            - Anything starting with SELECT, UPDATE, DELETE, INSERT.
        //                                            - WHERE, GROUP BY, HAVING, ORDER BY clauses.
        //                                            - Full standalone queries.";

        [Browsable(true)]
        [Category("System Prompt")]
        [DisplayName(@"Continuation")]
        [Description("The 'Continuation' system prompt, that indicates to AI what rules you want AI to adhere too when generating the response. Edit in JSON file or delete JSON file to restart with defaults again")]
        [ReadOnly(true)]
        public string ContinuationPrompt { get; set; } = @"You are a backend SQL autocomplete engine.
                                                            STRICT RULES:
                                                            - Output ONLY valid JSON.
                                                            - Output MUST be a COMPLETE JSON array.
                                                            - Each item MUST contain a ""suggestion"" property.
                                                            - Do NOT include markdown, code fences, comments, or explanations.
                                                            - Do NOT include trailing commas.
                                                            - Do NOT output reasoning or thoughts.
                                                            - Return between 4 and 8 suggestions.

                                                            JSON FORMAT:
                                                            [
                                                              { ""suggestion"": ""SQL fragment"" }
                                                            ]

                                                            COMPLETION RULES:
                                                            - The user already has a SQL query.
                                                            - You are ONLY generating the next fragment to append at the cursor.
                                                            - NEVER rewrite the full query.
                                                            - NEVER repeat SELECT ... FROM.
                                                            - The ""suggestion"" value MUST NOT start with SELECT, INSERT, UPDATE, DELETE, or MERGE.
                                                            - Suggestions must be short SQL fragments.

                                                            VALID SUGGESTION TYPES:
                                                            - JOIN clauses
                                                            - WHERE conditions
                                                            - GROUP BY clauses
                                                            - HAVING conditions
                                                            - ORDER BY clauses
                                                            - additional column selections starting with a comma

                                                            SQL CLAUSE PRIORITY:
                                                            Prefer suggestions in this logical SQL order when applicable:
                                                            1. JOIN
                                                            2. WHERE
                                                            3. GROUP BY
                                                            4. HAVING
                                                            5. ORDER BY

                                                            JOIN RULES:
                                                            - When joins apply, include multiple join variants:
                                                              INNER JOIN
                                                              LEFT JOIN
                                                              RIGHT JOIN (when sensible)
                                                              FULL OUTER JOIN (when sensible)
                                                            - Use the same alias and ON condition when only the join type changes.
                                                            - Suggest joins only when they logically extend the existing tables.
                                                            - CROSS JOIN only when a cross product makes sense.

                                                            SUGGESTION QUALITY:
                                                            - Prefer realistic SQL Server style syntax.
                                                            - Prefer short fragments that can be appended directly.
                                                            - Avoid duplicate suggestions.
                                                            - Avoid repeating tables already joined.

                                                            GOOD examples:
                                                            - ""INNER JOIN SalesLT.CustomerAddress AS ca ON a.AddressID = ca.AddressID""
                                                            - ""LEFT JOIN SalesLT.CustomerAddress AS ca ON a.AddressID = ca.AddressID""
                                                            - ""WHERE a.City = N'London'""
                                                            - ""WHERE a.ModifiedDate >= '2026-01-01'""
                                                            - ""GROUP BY a.CustomerID""
                                                            - ""HAVING COUNT(*) > 1""
                                                            - ""ORDER BY a.CustomerID""
                                                            - "", PostalCode""

                                                            BAD examples:
                                                            - ""SELECT * FROM ...""
                                                            - ""UPDATE ...""
                                                            - ""DELETE ...""
                                                            - full standalone queries.""";

    }
}
