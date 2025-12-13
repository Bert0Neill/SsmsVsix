using AI_Buddy.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Input;
using static AI_Buddy.Components.PromptWindowControl;

namespace AI_Buddy.Services
{
    internal class AIService
    {
        private readonly AIProperties _aiProperties;
        private readonly FileService _fileService;

        public AIService()
        {
            _fileService = new FileService();
            _aiProperties = new AIProperties();

            // read file settings
            string filePath = Path.Combine(Directory.GetCurrentDirectory(), _aiProperties.SettingsFilename);

            if (File.Exists(filePath))
            {
                _aiProperties = _fileService.LoadFromJson<AIProperties>(filePath);
            }
        }

        public async Task GetOllamaResponseStreamAsync(string prompt, Action<string> onResponseChunk)
        {
            try
            {
                Mouse.OverrideCursor = Cursors.Wait;

                using (HttpClient client = new HttpClient())
                {
                    var requestBody = new
                    {
                        model = _aiProperties.PromptLLMName,
                        prompt = prompt,
                        stream = _aiProperties.IsPromptResponseStreaming // Enable streaming response
                    };

                    // append api key to request
                    client.DefaultRequestHeaders.Clear();
                    client.DefaultRequestHeaders.Add("Authorization", $"Bearer {_aiProperties.AIPromptKey}");

                    string jsonContent = JsonConvert.SerializeObject(requestBody);
                    HttpContent httpContent = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                    using (var request = new HttpRequestMessage(HttpMethod.Post, _aiProperties.AIPromptURL) { Content = httpContent })
                    using (var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead))
                    {
                        if (!response.IsSuccessStatusCode)
                        {
                            onResponseChunk($"Error: {response.StatusCode}");
                            return;
                        }

                        using (var stream = await response.Content.ReadAsStreamAsync())
                        using (var reader = new StreamReader(stream))
                        {
                            while (!reader.EndOfStream)
                            {
                                var line = await reader.ReadLineAsync();
                                if (!string.IsNullOrWhiteSpace(line))
                                {
                                    var jsonResponse = JsonConvert.DeserializeObject<AIHosterResponse>(line);
                                    jsonResponse.response = Regex.Replace(jsonResponse.response, @"<\/?think>", "") // remove DeepSeek's <think> elements
                                        .TrimStart('\n', '\r')
                                        .TrimStart();

                                    onResponseChunk(jsonResponse.response);
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                onResponseChunk(ex.Message);
            }
            finally
            {
                Mouse.OverrideCursor = null;
            }
        }
    }
}
