using System;
using System.ComponentModel;

namespace SSMS_AI_Buddy.Models
{
    public class AIProperties
    {
        [Category("AI Provider")]
        [DisplayName("AI Hoster")]
        [Description("Select the hoster you wish to call with your prompt.")]
        [TypeConverter(typeof(AIProviderTypeConverter))]
        public string AIProvider { get; set; } = AIProviderOptions.NvidiaRemote;

        [Category("AI Provider")]
        [DisplayName("Prompt API URL")]
        [Description("The Prompt URL that will get called with your prompt.For e.g. NVidia 'https://integrate.api.nvidia.com/v1/chat/completions' or Ollama (local) 'http://localhost:11434/api/generate'")]
        public string AIPromptURL { get; set; } = "https://integrate.api.nvidia.com/v1/chat/completions"; // Nvidia remote | "http://localhost:11434/api/generate"; // Ollama local default

        [Category("AI Provider")]
        [DisplayName("API Key (Nvidia Only)")]
        [Description("The key may not be required for internal server prompts.")]
        public string AIPromptKey { get; set; } = "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"; // Bert's own NVidia key!

        [Category("AI Provider")]
        [DisplayName("LLM Name")]
        [Description("For your reference, what LLM model your Hoster is using (for e.g qwen/qwen3-coder-480b-a35b-instruct)")]
        public string PromptLLMName { get; set; } = "openai/gpt-oss-120b"; //NVIDIA - qwen/qwen3-coder-480b-a35b-instruct NVidia

        [Browsable(false)]
        [Category("AI Provider")]
        [DisplayName("Streaming Response")]
        [Description("Currently all prompts are returned as a block and not streamed in real-time, so that the user can select the appropiate item from a full list.")]
        public bool IsPromptResponseStreaming { get; set; } = false;

        [Category("AI Provider")]
        [DisplayName("Temperature")]
        [Description("Controls the randomness of the AI's responses.")]
        public double Temperature { get; set; } = 0.5; // default value for creativity in AI response, can be adjusted by user

        [Category("AI Provider")]
        [DisplayName("Max Tokens")]
        [Description("Maximum number of text tokens the AI can generate in the response. Higher values allow longer answers but may increase response time and API usage.")]
        public int Token { get; set; } = 1000;

        [Category("AI Provider")]
        [DisplayName("Randomness")]
        [Description("Limits the responses to an upper percentage accuracy. Used with NVida.")]
        public double Randomness { get; set; } = 0.8; // defaults to 80%


        [Category("Coding")]
        [DisplayName("SSMS Version")]
        [Description("What version of SSMS are you requesting answers from AI prompt to execute on")]
        public string SsmsVersion { get; set; } = "SSMS 18-20";

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
        public string GroupHavingPrompt { get; set; } = @"No thinking. You are a SQL Server GROUP BY and HAVING clause completion engine.
                                                        STRICT RULES:
                                                        - Output ONLY valid JSON.
                                                        - Output MUST be a COMPLETE JSON array.
                                                        - Each item MUST contain a ""suggestion"" property.
                                                        - Do NOT include markdown, code fences, comments, explanations, or extra text.
                                                        - Do NOT include trailing commas.
                                                        - Do NOT explain reasoning.
                                                        - Do NOT output thoughts.
                                                        - Return between 4 and 8 suggestions.

                                                        COMPLETION RULES:
                                                        - You are completing an EXISTING SQL query.
                                                        - Output ONLY the next fragment to append.
                                                        - DO NOT rewrite the full query.
                                                        - DO NOT duplicate the existing SELECT or FROM clause.
                                                        - The JSON ""suggestion"" field MUST NOT start with SELECT.
                                                        - Prefer COMPLETE logical SQL continuations over isolated keywords.
                                                        - Prefer GROUP BY + HAVING combinations when aggregate functions are likely.
                                                        - When suggesting HAVING, include the GROUP BY clause in the SAME suggestion.
                                                        - Standalone HAVING suggestions are NOT allowed.
                                                        - Suggestions must be directly appendable at the cursor position.
                                                        - Avoid incomplete fragments such as:
                                                          ""GROUP BY""
                                                          ""HAVING""
                                                          ""ORDER BY""

                                                        GROUPING RULES:
                                                        - Suggest GROUP BY clauses using likely grouping columns.
                                                        - Prefer grouping columns commonly used in SQL Server:
                                                          IDs, names, categories, dates, statuses, regions, types.
                                                        - Suggest multiple grouping combinations when sensible.

                                                        HAVING RULES:
                                                        - Suggest HAVING clauses using aggregate functions.
                                                        - Use common aggregates:
                                                          COUNT, SUM, AVG, MIN, MAX.
                                                        - Use realistic comparison conditions:
                                                          >, <, >=, <=, =.
                                                        - Prefer meaningful thresholds:
                                                          COUNT(*) > 1
                                                          SUM(TotalAmount) > 1000
                                                          AVG(Price) >= 50

                                                        OUTPUT FORMAT:
                                                        [
                                                          {
                                                            ""suggestion"": ""GROUP BY CustomerID HAVING COUNT(*) > 1""
                                                          }
                                                        ]

                                                        GOOD examples:
                                                        - ""GROUP BY CustomerID HAVING COUNT(*) > 1""
                                                        - ""GROUP BY CustomerID HAVING COUNT(*) >= 5""
                                                        - ""GROUP BY CustomerID HAVING SUM(TotalAmount) > 1000""
                                                        - ""GROUP BY ProductID HAVING AVG(Price) >= 50""
                                                        - ""GROUP BY Region HAVING COUNT(DISTINCT CustomerID) > 5""
                                                        - ""GROUP BY OrderDate HAVING SUM(Quantity) > 10""
                                                        - ""GROUP BY CustomerID, OrderDate HAVING COUNT(*) > 1""

                                                        BAD examples:
                                                        - ""GROUP BY""
                                                        - ""HAVING""
                                                        - ""ORDER BY""
                                                        - ""SELECT * FROM Orders""
                                                        - Anything starting with:
                                                          SELECT
                                                          INSERT
                                                          UPDATE
                                                          DELETE
                                                          MERGE
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
                                                    - Output MUST contain between 4 and 10 json array items.
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
        [DisplayName(@"Performance")]
        [Description("The 'Performance' system prompt, that indicates to AI what rules you want AI to adhere too when generating the response. Edit in JSON file or delete JSON file to restart with defaults again")]
        [ReadOnly(true)]
        public string PerformancePrompt { get; set; } = @"STRICT RULES:
                                                        - Output ONLY valid JSON.
                                                        - Output MUST be a COMPLETE JSON array.
                                                        - Each item MUST contain a ""suggestion"" property.
                                                        - Do NOT include markdown, code fences, comments, or explanations.
                                                        - Do NOT include trailing commas.
                                                        - Do NOT output reasoning or thoughts.
                                                        - Return between 4 and 8 suggestions.

                                                        JSON FORMAT:
                                                        [
                                                          { ""suggestion"": ""optimization recommendation"" }
                                                        ]

                                                        PERFORMANCE OPTIMIZATION RULES:
                                                        - The user already has a SQL query.
                                                        - You are ONLY generating SQL performance improvements.
                                                        - Focus on improving execution speed, reducing IO, lowering CPU usage, and improving index usage.
                                                        - Suggestions may rewrite parts of the query for performance purposes.
                                                        - Suggestions MUST be concise and actionable.
                                                        - Suggestions MAY start with SELECT when rewriting a query improves performance.
                                                        - Prefer SQL Server optimization techniques.
                                                        - Make the suggestion a comment, start suggestion with --

                                                        VALID SUGGESTION TYPES:
                                                        - Index recommendations
                                                        - Query rewrites
                                                        - EXISTS instead of IN
                                                        - JOIN optimization
                                                        - SARGable WHERE conditions
                                                        - Reducing SELECT *
                                                        - Covering indexes
                                                        - Filtered indexes
                                                        - Removing unnecessary DISTINCT
                                                        - Replacing subqueries with joins
                                                        - Using TOP when appropriate
                                                        - Predicate pushdown improvements
                                                        - Avoiding functions on indexed columns
                                                        - Partition filtering
                                                        - Temp table or CTE optimization
                                                        - Aggregation optimization

                                                        SUGGESTION QUALITY:
                                                        - Prefer realistic SQL Server syntax.
                                                        - Prefer short but useful optimization recommendations.
                                                        - Avoid duplicate suggestions.
                                                        - Avoid generic advice like ""improve indexes"".
                                                        - Suggestions should be directly applicable to the provided query.
                                                        - Prefer recommendations that commonly improve execution plans.

                                                        GOOD examples:
                                                        - ""-- CREATE INDEX IX_Orders_CustomerID ON Orders(CustomerID)""
                                                        - ""-- Replace IN with EXISTS for better correlated subquery performance""
                                                        - ""-- Avoid SELECT * and return only required columns""
                                                        - ""-- Move functions off indexed columns in the WHERE clause""
                                                        - ""-- Use INNER JOIN instead of LEFT JOIN when nulls are not required""
                                                        - ""-- Add a covering index including OrderDate, CustomerID INCLUDE (TotalAmount)""
                                                        - ""-- Filter rows before JOIN operations to reduce dataset size""
                                                        - ""-- Replace DISTINCT with GROUP BY when aggregation already exists""
                                                        - ""-- Use WHERE OrderDate >= '2025-01-01' to improve index seek usage""

                                                        BAD examples:
                                                        - ""The query could be faster""
                                                        - ""Maybe add indexes""
                                                        - ""Use better SQL""
                                                        - Long explanations
                                                        - Markdown output
                                                        - Full essays";

    }
}
