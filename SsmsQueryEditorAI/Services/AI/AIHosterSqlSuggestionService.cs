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
using System.Net.Http.Headers;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using static SSMS_AI_Buddy.Models.AppEnums;

namespace SSMS_AI_Buddy.Services.AI
{
    public static class AIHosterSqlSuggestionService
    {
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
        /// Parses Ollama <c>/api/generate</c> JSON body: read only <see cref="OllamaModelResponse.Response"/>,
        /// never the outer <c>context</c> array (regex from first '[' to last ']' would include it).
        /// </summary>
        private static List<SqlSuggestion> ParseSuggestionsFromOllamaHttpBody(string httpBody)
        {
            if (string.IsNullOrWhiteSpace(httpBody))
                return new List<SqlSuggestion>();

            try
            {
                var outer = JsonConvert.DeserializeObject<OllamaModelResponse>(httpBody);
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

        private static List<SqlSuggestion> ParseSuggestionsFromNVidiaHttpBody(string httpBody)
        {
            if (string.IsNullOrWhiteSpace(httpBody))
                return new List<SqlSuggestion>();

            try
            {
                var outer = JsonConvert.DeserializeObject<NVidiaModelResponse>(httpBody);

                var content = outer?
                    .Choices?
                    .FirstOrDefault()?
                    .Message?
                    .Content;

                if (string.IsNullOrWhiteSpace(content))
                    return new List<SqlSuggestion>();

                Debug.WriteLine(content);

                var list = DeserializeSuggestionArray(content);

                NormalizeContinuationSuggestions(list);

                return list;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"NVIDIA suggestion parse failed: {ex}");

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
                    return _aiProperties.PerformancePrompt;

                case AIActionType.AIPerformance:
                    return _aiProperties.PerformancePrompt;

                default:
                    throw new NotSupportedException(
                        string.Format("Unsupported AI action type: {0}", actionType));
            }
        }

        public static async Task<List<SqlSuggestion>> RetrieveAIResponseAsync(string schemaJson, string currentSql, AIActionType aIActionType)
        {
            Debug.WriteLine($"Prompt Schema:{schemaJson} - {currentSql}");
            var prompt = AI_Resource.AI_Prompt.Replace("{{SCHEMA}}", schemaJson).Replace("{{SQL}}", currentSql);
            string system = GetSystemPrompt(aIActionType);


            using (HttpClient client = new HttpClient())
            {
                object requestBody = null;

                requestBody = GenerateAIRequest(prompt, system, client);
                string jsonContent = JsonConvert.SerializeObject(requestBody);
                HttpContent httpContent = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                using (var request = new HttpRequestMessage(HttpMethod.Post, _aiProperties.AIPromptURL) { Content = httpContent })
                using (var res = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead))
                {
                    res.EnsureSuccessStatusCode();
                    var json = await res.Content.ReadAsStringAsync();
                    DisplayResponse(json);

                    if (_aiProperties.AIProvider == AIProviderOptions.NvidiaRemote)
                    {
                        return ParseSuggestionsFromNVidiaHttpBody(json); // NVidia                        
                    }
                    else if (_aiProperties.AIProvider == AIProviderOptions.OllamaLocal)
                    {
                        return ParseSuggestionsFromOllamaHttpBody(json); // Ollama
                    }
                    else
                    {
                        throw new NotSupportedException(string.Format("Unsupported provider: {0}", _aiProperties.AIProvider));
                    }
                }
            }
        }

        private static object GenerateAIRequest(string prompt, string system, HttpClient client)
        {
            object requestBody;
            switch (_aiProperties.AIProvider)
            {
                case AIProviderOptions.OllamaLocal:

                    requestBody = new
                    {
                        model = _aiProperties.PromptLLMName,
                        prompt = prompt,
                        system = system,
                        stream = _aiProperties.IsPromptResponseStreaming
                    };

                    break;

                case AIProviderOptions.NvidiaRemote:

                    requestBody = new
                    {
                        model = _aiProperties.PromptLLMName,
                        messages = new[]
                        {
                                new
                                {
                                    role = "user",
                                    content = prompt
                                },
                                new
                                {
                                    role = "system",
                                    content = system
                                }
                            },

                        temperature = _aiProperties.Temperature,
                        top_p = _aiProperties.Randomness,
                        max_tokens = _aiProperties.Token,
                        stream = _aiProperties.IsPromptResponseStreaming
                    };

                    client.DefaultRequestHeaders.Clear();
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _aiProperties.AIPromptKey);

                    break;

                default:
                    throw new NotSupportedException(string.Format("Unsupported provider: {0}", _aiProperties.AIProvider));
            }

            return requestBody;
        }

        private static void DisplayResponse(string json)
        {
            Debug.WriteLine($"=== {_aiProperties.AIProvider.ToString()} raw JSON response ===");
            Debug.WriteLine(json);
            Debug.WriteLine("=== End of response ===");
        }
    }

    
}


