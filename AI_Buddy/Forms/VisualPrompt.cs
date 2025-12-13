using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace AI_Buddy.Forms
{
    public partial class VisualPrompt : Form
    {
        private readonly HttpClient _httpClient = new HttpClient();

        public VisualPrompt()
        {
            InitializeComponent();
        }

        private async void btnSend_Click(object sender, EventArgs e)
        {
            string userPrompt = txtPrompt.Text;
            if (string.IsNullOrWhiteSpace(userPrompt))
            {
                MessageBox.Show("Please enter a prompt!", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            //string responseText = await GetOllamaResponseAsync(userPrompt);
            //txtResponse.Text = responseText;

            await GetOllamaResponseStreamAsync(userPrompt, chunk =>
            {
                txtResponse.Text += chunk; // Display each chunk as it arrives
            });

        }

        //private async Task<string> GetOllamaResponseAsync(string prompt)
        //{
        //    string ollamaUrl = "http://localhost:11434/api/generate";
        //    string model = "deepseek-r1:1.5b"; // Ensure the model is running locally

        //    using (HttpClient client = new HttpClient())
        //    {
        //        var requestBody = new
        //        {
        //            model = model,
        //            prompt = prompt,
        //            stream = false // Set to `true` if you want streamed responses
        //        };

        //        string jsonContent = JsonConvert.SerializeObject(requestBody);
        //        HttpContent httpContent = new StringContent(jsonContent, Encoding.UTF8, "application/json");

        //        HttpResponseMessage response = await client.PostAsync(ollamaUrl, httpContent);

        //        if (!response.IsSuccessStatusCode)
        //        {
        //            return $"Error: {response.StatusCode}";
        //        }

        //        string responseContent = await response.Content.ReadAsStringAsync();
        //        var jsonResponse = JsonConvert.DeserializeObject<OllamaResponse>(responseContent);
        //        return jsonResponse.response;
        //    }
        //}

        private async Task GetOllamaResponseStreamAsync(string prompt, Action<string> onResponseChunk)
        {
            string ollamaUrl = "http://localhost:11434/api/generate";
            string model = "deepseek-r1:1.5b"; // Ensure the model is running locally

            using (HttpClient client = new HttpClient())
            {
                var requestBody = new
                {
                    model = model,
                    prompt = prompt,
                    stream = true // Enable streaming response
                };

                string jsonContent = JsonConvert.SerializeObject(requestBody);
                HttpContent httpContent = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                using (var request = new HttpRequestMessage(HttpMethod.Post, ollamaUrl) { Content = httpContent })
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
                                var jsonResponse = JsonConvert.DeserializeObject<OllamaResponse>(line);
                                onResponseChunk(jsonResponse?.response);
                            }
                        }
                    }
                }
            }
        }






        public class OllamaResponse
        {
            public string response { get; set; }
        }
    }
}
