using Newtonsoft.Json;
using System.Collections.Generic;

namespace SSMS_AI_Buddy.Models
{
    public class NVidiaModelResponse
    {
        [JsonProperty("choices")]
        public List<Choice> Choices { get; set; }
    }

    public class Choice
    {
        [JsonProperty("message")]
        public Message Message { get; set; }
    }

    public class Message
    {
        [JsonProperty("content")]
        public string Content { get; set; }
    }
}
