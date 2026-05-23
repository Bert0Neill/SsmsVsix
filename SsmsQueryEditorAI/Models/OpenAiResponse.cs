using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SSMS_AI_Buddy.Models
{
    public class OpenAiResponse
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
