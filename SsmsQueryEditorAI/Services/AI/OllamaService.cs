using Newtonsoft.Json;
using SSMS_AI_Buddy.Models;
using SSMS_AI_Buddy.Resources;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Linq;

namespace SSMS_AI_Buddy.Services.AI
{
    public static class OllamaSqlSuggestionService
    {
        //private static readonly HttpClient _http = new HttpClient
        //{
        //    BaseAddress = new Uri("http://localhost:11434")
        //};

        private static readonly HttpClient _http = new HttpClient();

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




            //        var request = new
            //        {
            //            contents = new[]
            //{
            //    new
            //    {
            //        role = "user",
            //        parts = new[]
            //        {
            //            new { text = prompt }
            //        }
            //    }
            //},
            //            generationConfig = new
            //            {
            //                temperature = 0.1
            //            }
            //        };



            //        var json = JsonConvert.SerializeObject(request);
            //        var content = new StringContent(json, Encoding.UTF8, "application/json");

            //        var url = $"https://generativelanguage.googleapis.com/v1/models/gemini-1.5-flash:generateContent?key={"AIzaSyBBsZvWBKGQQae08ACDNIJx9hnlslDuadk"}";


            //        //var url =                $"https://generativelanguage.googleapis.com/v1beta/models/gemini-1.5-flash:generateContent?key={"AIzaSyBBsZvWBKGQQae08ACDNIJx9hnlslDuadk"}";

            //        var response = await _http.PostAsync(url, content);
            //        if (!response.IsSuccessStatusCode)
            //        {
            //            var error = await response.Content.ReadAsStringAsync();
            //            throw new Exception($"Gemini error: {error}");
            //        }


            //        var responseJson = await response.Content.ReadAsStringAsync();
            //        var result = JsonConvert.DeserializeObject<GeminiResponse>(responseJson);

            //        var text = result?
            //        .Candidates?
            //        .FirstOrDefault()?
            //        .Content?
            //        .Parts?
            //        .FirstOrDefault()?
            //        .Text;

            //        if (string.IsNullOrWhiteSpace(text))
            //            return new List<SqlSuggestion>();

            //        return JsonConvert.DeserializeObject<List<SqlSuggestion>>(text);


            //var url = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-1.5-flash:generateContent?key={"AIzaSyBBsZvWBKGQQae08ACDNIJx9hnlslDuadk"}";


            var url = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-2.0-flash:generateContent?key=" + "AIzaSyBBsZvWBKGQQae08ACDNIJx9hnlslDuadk";



            var payload = new
            {
                contents = new[]
                {
        new
        {
            role = "user",
            parts = new[]
            {
                new { text = prompt }
            }
        }
    }
            };

            var client = new HttpClient();
            var json = JsonConvert.SerializeObject(payload);

            var response = await client.PostAsync(
                url,
                new StringContent(json, Encoding.UTF8, "application/json")
            );

            var result = await response.Content.ReadAsStringAsync();

          

            if (string.IsNullOrWhiteSpace(result))
                return new List<SqlSuggestion>();

            return JsonConvert.DeserializeObject<List<SqlSuggestion>>(result);

            //-----------------------------
            //var request = new
            //{
            //    model = "qwen2.5-coder:7b",
            //    //model = "deepseek-coder:1.3b",
            //    prompt = prompt,
            //    stream = true,
            //    options = new
            //    {
            //        temperature = 0.1,
            //        num_predict = 256
            //    }
            //};


            //var content = new StringContent(JsonConvert.SerializeObject(request), Encoding.UTF8, "application/json");

            //var response = await _http.PostAsync("/api/generate", content);
            //response.EnsureSuccessStatusCode();

            //var json = await response.Content.ReadAsStringAsync();

            //var ollamaResponse = JsonConvert.DeserializeObject<OllamaResponse>(json);

            //if (string.IsNullOrWhiteSpace(ollamaResponse?.Response))
            //    return new List<SqlSuggestion>();

            //// 🔹 This is the important line
            //return JsonConvert.DeserializeObject<List<SqlSuggestion>>(
            //    ollamaResponse.Response);
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


