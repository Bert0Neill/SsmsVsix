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
            var prompt = String.Format(AI_Resource.Full_Ai_Prompt, schemaJson, currentSql);
            Debug.WriteLine("AI Prompt:\n" + prompt);

            var request = new OllamaRequest
            {
                Model = "llama3.1",
                Prompt = prompt
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
