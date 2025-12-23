using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using SSMS_AI_Buddy.Models;
using SSMS_AI_Buddy.Resources;
using System.Diagnostics;
using System.Net.Http;
namespace SSMS_AI_Buddy.Services.AI
{
    public static class OllamaSqlSuggestionService
    {
        private static readonly HttpClient _http = new HttpClient
        {
            BaseAddress = new Uri("http://localhost:11434")
        };

        public static async Task<List<SqlSuggestion>> GetSqlSuggestionsAsync(string schemaJson, string currentSql)
        {
            Debug.WriteLine($"Prompt Schema:{ schemaJson} - {currentSql}");
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

            var request = new
            {
                model = "qwen2.5-coder:7b",
                //model = "deepseek-coder:1.3b",
                prompt = prompt,
                stream = false,
                options = new
                {
                    temperature = 0.1,
                    num_predict = 256
                }
            };


            var content = new StringContent(JsonConvert.SerializeObject(request), Encoding.UTF8, "application/json");

            var response = await _http.PostAsync("/api/generate", content);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();

            var ollamaResponse = JsonConvert.DeserializeObject<OllamaResponse>(json);

            if (string.IsNullOrWhiteSpace(ollamaResponse?.Response))
                return new List<SqlSuggestion>();

            // 🔹 This is the important line
            return JsonConvert.DeserializeObject<List<SqlSuggestion>>(
                ollamaResponse.Response);
        }
    }

}
