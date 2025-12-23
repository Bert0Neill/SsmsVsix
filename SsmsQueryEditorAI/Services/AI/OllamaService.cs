using Newtonsoft.Json;
using SSMS_AI_Buddy.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace SSMS_AI_Buddy.Services.AI
{
    public static class OllamaSqlSuggestionService
    {
        private static readonly HttpClient _http = new HttpClient
        {
            BaseAddress = new Uri("http://localhost:11434")
        };

        //private static readonly HttpClient _http = new HttpClient();

        public static async Task<List<SqlSuggestion>> GetSqlSuggestionsAsync(string schemaJson, string currentSql)
        {
            Debug.WriteLine($"Prompt Schema:{schemaJson} - {currentSql}");
            // var prompt = AI_Resource.Full_Ai_Prompt.Replace("{{SCHEMA}}", schemaJson).Replace("{{SQL}}", currentSql);

            string prompt = $@"You are a SQL assistant.
Schema (only tables directly joinable to current SQL tables): {{{{SCHEMA}}}}
Current SQL: {{{{SQL}}}}
Suggest next valid SQL tokens/snippets: JOIN, WHERE, GROUP BY, HAVING, ORDER BY.
Respect table aliases if present. Only suggest columns/tables from schema.
Return JSON array:
[
  {{ ""type"": ""..."", ""suggestion"": ""..."", ""description"": ""..."" }}
]
Do not include extra text.
".Replace("{{SCHEMA}}", schemaJson).Replace("{{SQL}}", currentSql);


            //prompt = "Answer directly. Do not explain. Do not reason. What is 4 + 4";

            //-----------------------------
            var request = new
            {
                //model = "qwen2.5-coder:7b",
                //model = "deepseek-coder:1.3b",
                //model = "deepseek-r1:8b",
                //model = "qwen2.5-coder:7b",
                model = "phi3:mini",

            prompt = prompt,
                stream = false,
                options = new
                {
                    temperature = 0.0,
                    num_predict = 32
                }

            };


            var content = new StringContent(JsonConvert.SerializeObject(request), Encoding.UTF8, "application/json");

            //var response = await _http.PostAsync("/api/generate", content);
            var response = await _http.PostAsync("/api/generate", content).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            //var json = await response.Content.ReadAsStringAsync();

            var ollamaResponse = JsonConvert.DeserializeObject<OllamaResponse>(json);
            if (string.IsNullOrWhiteSpace(ollamaResponse?.Response)) return new List<SqlSuggestion>();
            return JsonConvert.DeserializeObject<List<SqlSuggestion>>(ollamaResponse.Response);

        }
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


