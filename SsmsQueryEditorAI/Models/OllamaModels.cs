using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SSMS_AI_Buddy.Models
{
    public class OllamaRequest
    {
        [JsonProperty("model")]
        public string Model { get; set; }

        [JsonProperty("prompt")]
        public string Prompt { get; set; }

        [JsonProperty("stream")]
        public bool Stream { get; set; } = false;
    }

    public class OllamaResponse
    {
        [JsonProperty("response")]
        public string Response { get; set; }
    }

}
