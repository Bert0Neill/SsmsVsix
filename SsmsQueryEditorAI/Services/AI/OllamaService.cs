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



            var request = new
            {
                model = "qwen2.5-coder:0.5b",
                //model = "deepseek-coder:1.3b",
                //model = "alibayram/smollm3:latest",
                prompt = prompt,
                system = "You are a backend SQL completion engine. Strict rules: Do NOT include <think> blocks, reasoning, or internal thoughts. Do NOT include markdown, code fences, or comments. Output ONLY valid, machine-parseable JSON. Output MUST be a single JSON array. Do NOT include trailing commas. Do NOT include any text outside the JSON array. If no valid suggestions exist, return an empty array []. Invalid JSON is considered a system failure.\r\n",
                stream = false,
                options = new
                {
                    temperature = 0.7,
                    num_predict = 256
                }
            };


            var content = new StringContent(JsonConvert.SerializeObject(request), Encoding.UTF8, "application/json");
            var response = await _http.PostAsync("/api/generate", content); // Ollama
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            json = Regex.Replace(json, @"^```json\s*|\s*```$", "", RegexOptions.IgnoreCase);
            
            // DEBUG: print the cleaned JSON to the console
            Debug.WriteLine("=== Ollama raw JSON response ===");
            Debug.WriteLine(json);
            Debug.WriteLine("=== End of response ===");

            var outer = JsonConvert.DeserializeObject<ModelResponse>(json);
            string innerJson = Regex.Replace(outer.Response, @"^```json\s*|\s*```$", "", RegexOptions.IgnoreCase);

            // DEBUG: print the inner JSON too
            Debug.WriteLine("=== Inner JSON ===");
            Debug.WriteLine(innerJson);
            Debug.WriteLine("=== End of inner JSON ===");

            var suggestions = JsonConvert.DeserializeObject<List<SqlSuggestion>>(innerJson);
            return suggestions;

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


