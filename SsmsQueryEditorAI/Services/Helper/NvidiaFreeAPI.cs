using System;
using System.Text;
using System.Threading.Tasks;
using System.Net.Http;
using Newtonsoft.Json;

namespace SSMS_AI_Buddy.Services.Helper
{
    
    public class NvidiaFreeAPI
    {
        private static readonly HttpClient _httpClient = new HttpClient();

        public static async Task CallDeepSeekAsync()
        {
            // API key
            string apiKey = "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";

            // NVIDIA hosted DeepSeek model
            string endpoint = "https://integrate.api.nvidia.com/v1/chat/completions";

            var requestBody = new
            {
                model = "deepseek-ai/deepseek-r1",
                messages = new[]
                {
                new
                {
                    role = "user",
                    content = "Generate a SQL INNER JOIN between Customers and Orders tables."
                }
            },
                temperature = 0.2,
                max_tokens = 500
            };

            string json = JsonConvert.SerializeObject(requestBody);

            using (var request = new HttpRequestMessage(HttpMethod.Post, endpoint))
            {
                request.Headers.Add("Authorization", $"Bearer {apiKey}");

                request.Content = new StringContent(
                    json,
                    Encoding.UTF8,
                    "application/json");

                HttpResponseMessage response =
                    await _httpClient.SendAsync(request);

                string responseContent =
                    await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine("Error:");
                    Console.WriteLine(responseContent);
                    return;
                }

                dynamic result = JsonConvert.DeserializeObject(responseContent);

                string aiResponse =
                    result.choices[0].message.content.ToString();

                Console.WriteLine("AI Response:");
                Console.WriteLine(aiResponse);
            }
        }
    }
}
