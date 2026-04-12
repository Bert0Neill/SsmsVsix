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
        private static HttpClient _http;

        private static HttpClient Http
        {
            get
            {
                if (_http == null)
                {
                    if (_aiProperties == null || string.IsNullOrWhiteSpace(_aiProperties.AIPromptURL))
                        throw new InvalidOperationException("AIProperties or AIPromptURL not configured.");

                    _http = new HttpClient
                    {
                        BaseAddress = new Uri(_aiProperties.AIPromptURL)
                    };
                }

                return _http;
            }
        }

        public static AIProperties _aiProperties { get; set; }

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


        private static List<SqlSuggestion> DeserializeSuggestionArray(string text)
        {
            text = RemoveCodeFences(text ?? string.Empty).Trim();
            text = Regex.Replace(text, "\u003Cthink\u003E[\\s\\S]*?\u003C/think\u003E", "", RegexOptions.IgnoreCase);

            int startArray = text.IndexOf('[');
            int startObject = text.IndexOf('{');

            int start = startArray >= 0 ? startArray : startObject;
            if (start < 0)
                return new List<SqlSuggestion>();

            using (var sr = new StringReader(text.Substring(start)))
            using (var reader = new JsonTextReader(sr) { CloseInput = true })
            {
                var token = JToken.Load(reader);

                if (token.Type == JTokenType.Array)
                    return token.ToObject<List<SqlSuggestion>>() ?? new List<SqlSuggestion>();

                if (token.Type == JTokenType.Object)
                    return new List<SqlSuggestion> { token.ToObject<SqlSuggestion>() };

                return new List<SqlSuggestion>();
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

        public static async Task<List<SqlSuggestion>> GetSqlSuggestionsAsync(string schemaJson, string currentSql, AIActionType aIActionType)
        {
            Debug.WriteLine($"Prompt Schema:{schemaJson} - {currentSql}");
            var prompt = AI_Resource.AI_Prompt.Replace("{{SCHEMA}}", schemaJson).Replace("{{SQL}}", currentSql);
            //var prompt = AI_Resource.Full_Ai_Prompt.Replace("{{SCHEMA}}", schemaJson).Replace("{{SQL}}", currentSql);
            string system = GetSystemPrompt(aIActionType);

//             string system = @"You are a SQL JOIN autocomplete engine.

//Return ONLY valid JSON.

//OUTPUT FORMAT
//[
//  {""type"":""Join"",""suggestion"":""JOIN table t ON condition"",""description"":""short explanation""},
//  {""type"":""Join"",""suggestion"":""LEFT JOIN table t ON condition"",""description"":""short explanation""},
//  {""type"":""Join"",""suggestion"":""Right JOIN table t ON condition"",""description"":""short explanation""},
//  {""type"":""Join"",""suggestion"":""Cross JOIN table t ON condition"",""description"":""short explanation""}
//]

//RULES
//- Output MUST be a valid JSON array.
//- Output MUST contain between 4 and 8 json array items.
//- NEVER output fewer than 4 items.
//- Suggestions MUST start with JOIN, INNER JOIN, LEFT JOIN, or CROSS JOIN.
//- Only output JOIN fragments, never full SELECT statements.
//- Do not rewrite the user query.
//- Do not output explanations or markdown.

//JOIN GENERATION
//- Make a suggestion for every table relationship supplied in prompt.
//- Use foreign key relationships from the schema.
//- If multiple joins are possible, return suggestions for each.
//- If fewer than 4 joins exist, create variants using JOIN, INNER JOIN, LEFT JOIN, CROSS JOIN.
//- Reuse the same ON condition when only the join type differs.

//ALIAS RULE
//- If the base table has an alias, use that alias ";

        var request = new
            {
                model = "qwen2.5-coder:1.5b", // more accurate but slower
                //model = "deepseek-coder:6.7b", // faster but less accurate
                //model = _aiProperties.PromptLLMName,
                prompt = prompt,
                system = system,
                stream = false,
                format= "json",
                options = new
                {
                    temperature = 0.1,
                    num_predict = 150, // token limit for suggestions - adjust as needed based on typical response length and model limits
                     stop = new[] { "]" } // stop runaway generation
                }
            };

            var content = new StringContent(JsonConvert.SerializeObject(request), Encoding.UTF8, "application/json");
            //var response = await _http.PostAsync("/api/generate", content); // Ollama
            var response = await Http.PostAsync(string.Empty, content);
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();

            Debug.WriteLine("=== Ollama raw JSON response ===");
            Debug.WriteLine(json);
            Debug.WriteLine("=== End of response ===");

            return ParseSuggestionsFromOllamaHttpBody(json);
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


