using Azure;
using Microsoft.SqlServer.Management.Smo;
using Newtonsoft.Json;
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

namespace SSMS_AI_Buddy.Services.AI
{
    public static class OllamaSqlSuggestionService
    {
        //private static readonly HttpClient _http = new HttpClient
        //{
        //    BaseAddress = new Uri("http://localhost:11434")            
        //};

        private static readonly HttpClient _http = new HttpClient
        {
            BaseAddress = new Uri("http://127.0.0.1:8080")
        };


        //private static readonly HttpClient _http = new HttpClient();

        public static async Task<List<SqlSuggestion>> GetSqlSuggestionsAsync(string schemaJson, string currentSql)
        {
            Debug.WriteLine($"Prompt Schema:{schemaJson} - {currentSql}");
            var prompt = AI_Resource.Full_Ai_Prompt.Replace("{{SCHEMA}}", schemaJson).Replace("{{SQL}}", currentSql);

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


            //        var url = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-2.0-flash:generateContent?key=" + "AIzaSyBBsZvWBKGQQae08ACDNIJx9hnlslDuadk";



            //        var payload = new
            //        {
            //            contents = new[]
            //            {
            //    new
            //    {
            //        role = "user",
            //        parts = new[]
            //        {
            //            new { text = prompt }
            //        }
            //    }
            //}
            //        };

            //        var client = new HttpClient();
            //        var json = JsonConvert.SerializeObject(payload);

            //        var response = await client.PostAsync(
            //            url,
            //            new StringContent(json, Encoding.UTF8, "application/json")
            //        );

            //        var result = await response.Content.ReadAsStringAsync();



            //        if (string.IsNullOrWhiteSpace(result))
            //            return new List<SqlSuggestion>();

            //        return JsonConvert.DeserializeObject<List<SqlSuggestion>>(result);

            //-----------------------------
            //var request = new
            //{
            //    //model = "qwen2.5-coder:7b",
            //    //model = "deepseek-coder:1.3b",
            //    model = "alibayram/smollm3:latest",
            //    prompt = prompt,
            //    system = "You are a backend SQL completion engine. Strict rules: Do NOT include <think> blocks, reasoning, or internal thoughts. Do NOT include markdown, code fences, or comments. Output ONLY valid, machine-parseable JSON. Output MUST be a single JSON array. Do NOT include trailing commas. Do NOT include any text outside the JSON array. If no valid suggestions exist, return an empty array []. Invalid JSON is considered a system failure.\r\n",
            //    stream = false,
            //    options = new
            //    {
            //        temperature = 0.7,
            //        num_predict = 256
            //    }
            //};


            //var content = new StringContent(JsonConvert.SerializeObject(request), Encoding.UTF8, "application/json");
            //var response = await _http.PostAsync("/api/generate", content); // Ollama
            //response.EnsureSuccessStatusCode();
            //var json = await response.Content.ReadAsStringAsync();
            //var ollamaResponse = JsonConvert.DeserializeObject<OllamaResponse>(json);

            var payload = new
            {
                prompt = prompt,
                n_predict = 256,
                temperature = 0.2,
                stop = new[] { "<|im_end|>" },
                stream = false
            };

            var json = JsonConvert.SerializeObject(payload);

            var content = new StringContent(
                json,
                Encoding.UTF8,
                "application/json");

            var response = await _http.PostAsync("/completion", content);

            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadAsStringAsync();
            Console.WriteLine(result);

            if (string.IsNullOrWhiteSpace(result)) return new List<SqlSuggestion>();
            return JsonConvert.DeserializeObject<List<SqlSuggestion>>(result);
            
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


