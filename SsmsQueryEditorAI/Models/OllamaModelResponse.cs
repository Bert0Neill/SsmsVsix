using Newtonsoft.Json;
using System;

namespace SSMS_AI_Buddy.Models
{
    public class OllamaModelResponse
    {
        [JsonProperty("model")]
        public string Model { get; set; }

        [JsonProperty("created_at")]
        public DateTime CreatedAt { get; set; }

        [JsonProperty("response")]
        public string Response { get; set; }

    }
}
