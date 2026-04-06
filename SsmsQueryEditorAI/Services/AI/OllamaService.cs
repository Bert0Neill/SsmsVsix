using Azure;
using Microsoft.SqlServer.Management.Smo;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SSMS_AI_Buddy.Models;
using SSMS_AI_Buddy.Resources;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using static SSMS_AI_Buddy.Models.AppEnums;

namespace SSMS_AI_Buddy.Services.AI
{
    

    public static class OllamaSqlSuggestionService
    {
        public static AIProperties _aiProperties { get; set; } // = new AIProperties();

        /// <summary>
        /// Strips markdown code fences (``` or ```json) whether they wrap the whole string or appear inline.
        /// </summary>
        public static string RemoveCodeFences(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return input ?? string.Empty;

            input = input.Trim();

            if (Regex.IsMatch(input, @"^```(?:json)?\s*", RegexOptions.IgnoreCase))
            {
                input = Regex.Replace(input, @"^```(?:json)?\s*", "", RegexOptions.IgnoreCase);
                var close = input.LastIndexOf("```", StringComparison.Ordinal);
                if (close >= 0)
                    input = input.Substring(0, close);
                return input.Trim();
            }

            var m = Regex.Match(input, @"```(?:json)?\s*\r?\n([\s\S]*?)\r?\n```", RegexOptions.IgnoreCase);
            if (m.Success)
                return m.Groups[1].Value.Trim();

            return input;
        }


        /// <summary>
        /// Parses the first complete JSON array from text (handles brackets inside string values).
        /// </summary>
        private static List<SqlSuggestion> DeserializeSuggestionArray(string text)
        {
            text = RemoveCodeFences(text ?? string.Empty).Trim();
            text = Regex.Replace(text, "\u003Cthink\u003E[\\s\\S]*?\u003C/think\u003E", "", RegexOptions.IgnoreCase);
            int start = text.IndexOf('[');
            if (start < 0)
                return new List<SqlSuggestion>();

            using (var sr = new StringReader(text.Substring(start)))
            using (var reader = new JsonTextReader(sr) { CloseInput = true })
            {
                var token = JToken.Load(reader);
                return token.ToObject<List<SqlSuggestion>>() ?? new List<SqlSuggestion>();
            }
        }

        /// <summary>
        /// Parses Ollama <c>/api/generate</c> JSON body: read only <see cref="ModelResponse.Response"/>,
        /// never the outer <c>context</c> array (regex from first '[' to last ']' would include it).
        /// </summary>
        private static List<SqlSuggestion> ParseSuggestionsFromOllamaHttpBody(string httpBody)
        {
            if (string.IsNullOrWhiteSpace(httpBody))
                return new List<SqlSuggestion>();

            try
            {
                var outer = JsonConvert.DeserializeObject<ModelResponse>(httpBody);
                if (outer == null || string.IsNullOrWhiteSpace(outer.Response))
                    return new List<SqlSuggestion>();

                var list = DeserializeSuggestionArray(outer.Response);
                NormalizeContinuationSuggestions(list);
                return list;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ollama suggestion parse failed: {ex.Message}");
                return new List<SqlSuggestion>();
            }
        }

        /// <summary>
        /// Models often emit a full SELECT ? even when asked for continuations; trim to the first JOIN / WHERE / etc.
        /// </summary>
        private static void NormalizeContinuationSuggestions(List<SqlSuggestion> list)
        {
            if (list == null) return;
            foreach (var s in list)
            {
                if (s == null || string.IsNullOrEmpty(s.Suggestion))
                    continue;
                s.Suggestion = TrimContinuationFromFullSelect(s.Suggestion);
            }
        }

        private static string TrimContinuationFromFullSelect(string text)
        {
            text = text.Trim().TrimEnd(';');
            if (text.Length == 0)
                return text;
            if (!text.StartsWith("SELECT", StringComparison.OrdinalIgnoreCase))
                return text;

            var match = Regex.Match(
                text,
                @"\b(?:(?:INNER|LEFT|RIGHT|FULL)\s+(?:OUTER\s+)?JOIN|CROSS\s+JOIN|JOIN|WHERE|GROUP\s+BY|ORDER\s+BY|HAVING)\b",
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
            if (match.Success)
                return text.Substring(match.Index).TrimStart();

            return text;
        }

        private static string GetSystemPrompt(AIActionType actionType)
        {
            switch (actionType)
            {
                case AIActionType.AIJoins:
                    return _aiProperties.JoinPrompt;

                case AIActionType.AIGroupHaving:
                    return _aiProperties.GroupHavingPrompt;

                case AIActionType.AIWhere:
                    return _aiProperties.WherePrompt;

                case AIActionType.AIPrompt:
                    return _aiProperties.ContinuationPrompt;

                default:
                    throw new NotSupportedException(
                        string.Format("Unsupported AI action type: {0}", actionType));
            }
        }

        // Ollama endpoint
        private static readonly HttpClient _http = new HttpClient
        {
            BaseAddress = new Uri("http://localhost:11434")
        };

        //// llama.cpp endpoint
        //private static readonly HttpClient _http = new HttpClient
        //{
        //    BaseAddress = new Uri("http://127.0.0.1:8080")
        //};


        public static async Task<List<SqlSuggestion>> GetSqlSuggestionsAsync(string schemaJson, string currentSql, AIActionType aIActionType)
        {
            Debug.WriteLine($"Prompt Schema:{schemaJson} - {currentSql}");
            var prompt = AI_Resource.Full_Ai_Prompt.Replace("{{SCHEMA}}", schemaJson).Replace("{{SQL}}", currentSql);
            string system = GetSystemPrompt(aIActionType);


            //            string prompt = $@"You are a SQL assistant.
            //Schema (only tables directly joinable to current SQL tables): {{{{SCHEMA}}}}
            //Current SQL: {{{{SQL}}}}
            //Suggest next valid SQL tokens/snippets: JOIN, WHERE, GROUP BY, HAVING, ORDER BY.
            //Respect table aliases if present. Only suggest columns/tables from schema.
            //Return JSON array:
            //[
            //  {{ ""type"": ""..."", ""suggestion"": ""..."", ""description"": ""..."" }}
            //]
            //Do not include extra text.
            //".Replace("{{SCHEMA}}", schemaJson).Replace("{{SQL}}", currentSql);




            //-------- Ollama ---------------------

            //            prompt = @"INPUT DATA:

            //Database schema in JSON (only relevant tables included):
            //{
            //  ""tables"": [
            //    {
            //      ""name"": ""Address"",
            //      ""schema"": ""SalesLT"",
            //      ""columns"": [""AddressID"",""City"",""PostalCode""]
            //    },
            //    {
            //      ""name"": ""CustomerAddress"",
            //      ""schema"": ""SalesLT"",
            //      ""columns"": [""CustomerID"",""AddressID""]
            //    },
            //    {
            //      ""name"": ""Customer"",
            //      ""schema"": ""SalesLT"",
            //      ""columns"": [""CustomerID"",""FirstName"",""LastName"",""Email""]
            //    }
            //  ]
            //}

            //Current SQL query:
            //SELECT AddressID, City FROM SalesLT.Address

            //TASK:

            //Suggest valid NEXT SQL tokens or clauses that can be appended to the current query:
            //- JOIN clauses (INNER, LEFT) using columns already in scope
            //- WHERE clauses filtering by columns in scope
            //- ORDER BY clauses using columns in scope

            //RULES:
            //- Use table aliases if needed
            //- Only use tables and columns present in the schema
            //- Do not invent tables or columns
            //- Return syntactically valid SQL snippets
            //- Always return at least one suggestion if possible

            //OUTPUT FORMAT: JSON array of objects:
            //- ""type"": ""join"" | ""where"" | ""orderby""
            //- ""suggestion"": SQL snippet
            //- ""description"": optional explanation

            //IMPORTANT:
            //- Return ONLY the JSON array
            //- Do NOT include markdown, code fences, or any extra text
            //- Do not include explanations outside of the JSON array
            //- Example of valid output:
            //[
            //  { ""type"": ""join"", ""suggestion"": ""LEFT JOIN CustomerAddress AS ca ON Address.AddressID = ca.AddressID"", ""description"": ""Join Address to CustomerAddress"" },
            //  { ""type"": ""where"", ""suggestion"": ""WHERE City LIKE 'New%'"" }
            //]
            //";

//            var system = @"No thinking. You are a backend SQL completion engine.
//STRICT RULES:
//- Output ONLY valid JSON.
//- Output MUST be a COMPLETE JSON array.
//- Do NOT include markdown, code fences, comments, or extra text.
//- Do NOT include trailing commas.

//COMPLETION RULES:
//- You are completing an EXISTING query; the user keeps their SQL. Output ONLY the next fragment to append.
//- DO NOT rewrite the full query. DO NOT duplicate SELECT ... FROM ... for the whole statement.
//- The JSON ""suggestion"" field MUST NOT start with SELECT. Output from JOIN, WHERE, GROUP BY, ORDER BY, HAVING, or a comma + column.
//- When joins apply: include several join suggestions with different join kinds (INNER JOIN, LEFT JOIN, and when sensible RIGHT JOIN, FULL OUTER JOIN). Same aliases and ON clause where only the join kind differs. CROSS JOIN only if a cross product is appropriate.

//GOOD examples:
//- ""INNER JOIN SalesLT.CustomerAddress AS ca ON a.AddressID = ca.AddressID""
//- ""LEFT JOIN SalesLT.CustomerAddress AS ca ON a.AddressID = ca.AddressID""
//- ""WHERE a.City = N'London'""
//- "", PostalCode""

//BAD examples:
//- Anything starting with SELECT, UPDATE, DELETE, INSERT.
//- Full standalone queries

//If no valid suggestions exist, return [].

//Invalid or incomplete JSON is a system failure.";


            var request = new
            {
                
                //model = "qwen2.5-coder:1.5b", // more accurate but slower
                //model = "qwen2.5-coder:0.5b", // faster but less accurate
                model = "deepseek-r1:1.5b",
                //model = "alibayram/smollm3:latest",
                prompt = prompt,
                system = system,
                //system = "No thinking, you are a backend SQL completion engine. Strict rules: Do NOT include <think> blocks, reasoning, or internal thoughts. Do NOT include markdown, code fences, or comments. Output ONLY valid, machine-parseable JSON. Output MUST be a single JSON array. Do NOT include trailing commas. Do NOT include any text outside the JSON array. If no valid suggestions exist, return an empty array []. Invalid JSON is considered a system failure.\r\n",
                stream = false,
                format= "json",
                options = new
                {
                    temperature = 0.1,
                    num_predict = 500 // token limit for suggestions - adjust as needed based on typical response length and model limits
                }
            };


            var content = new StringContent(JsonConvert.SerializeObject(request), Encoding.UTF8, "application/json");
            var response = await _http.PostAsync("/api/generate", content); // Ollama
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();

            Debug.WriteLine("=== Ollama raw JSON response ===");
            Debug.WriteLine(json);
            Debug.WriteLine("=== End of response ===");

            return ParseSuggestionsFromOllamaHttpBody(json);

            // ------------ LLama.cpp--------
            //            prompt = @"INPUT DATA:

            //Database schema in JSON (only relevant tables included):
            //{
            //  ""tables"": [
            //    {
            //      ""name"": ""Address"",
            //      ""schema"": ""SalesLT"",
            //      ""columns"": [""AddressID"",""City"",""PostalCode""]
            //    },
            //    {
            //      ""name"": ""CustomerAddress"",
            //      ""schema"": ""SalesLT"",
            //      ""columns"": [""CustomerID"",""AddressID""]
            //    }
            //  ]
            //}

            //Current SQL query:
            //select * from SalesLT.Address

            //TASK:

            //Suggest valid NEXT SQL tokens that can be appended:
            //- JOIN clauses (INNER, LEFT) using columns in scope
            //- WHERE clauses on columns in scope
            //- ORDER BY clauses on columns in scope

            //RULES:
            //- Use table aliases if needed
            //- Only use tables/columns in schema
            //- Return syntactically valid snippets

            //OUTPUT FORMAT: JSON array of objects:
            //- ""type"": ""join""|""where""|""orderby""
            //- ""suggestion"": SQL snippet
            //- ""description"": optional explanation

            //Return ONLY the JSON array.
            //";

            //            var payload = new
            //            {
            //                prompt = prompt,
            //                n_predict = 128,
            //                temperature = 0.2,
            //                stop = new[] { "<|im_end|>" },
            //                stream = false
            //            };

            //            var json = JsonConvert.SerializeObject(payload);

            //            var content = new StringContent(
            //                json,
            //                Encoding.UTF8,
            //                "application/json");

            //            var response = await _http.PostAsync("/completion", content);
            //            response.EnsureSuccessStatusCode();

            //            var result = await response.Content.ReadAsStringAsync();
            //            Console.WriteLine(result);

            //            if (string.IsNullOrWhiteSpace(result)) return new List<SqlSuggestion>();
            //            return JsonConvert.DeserializeObject<List<SqlSuggestion>>(result);

        }
    }

    public class ModelResponse
    {
        [JsonProperty("model")]
        public string Model { get; set; }

        [JsonProperty("created_at")]
        public DateTime CreatedAt { get; set; }

        [JsonProperty("response")]
        public string Response { get; set; }

        // other properties if needed...
    }

    public class GeminiRequest
    {
        [JsonProperty("contents")]
        public List<Content> Contents { get; set; }

        [JsonProperty("generationConfig")]
        public GenerationConfig GenerationConfig { get; set; }
    }

    public class GenerationConfig
    {
        [JsonProperty("response_mime_type")]
        public string ResponseMimeType { get; set; }

        [JsonProperty("temperature")]
        public double Temperature { get; set; } = 0.1;
    }



    public class Content
    {
        public string Role { get; set; } = "user";
        public List<Part> Parts { get; set; }
    }

    public class Part
    {
        public string Text { get; set; }
    }


    public class GeminiResponse
    {
        public List<Candidate> Candidates { get; set; }
    }

    public class Candidate
    {
        public Content Content { get; set; }
    }
}


