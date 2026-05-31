using Newtonsoft.Json;

namespace SSMS_AI_Buddy.Models
{
    public class SqlSuggestion
    {
        [JsonProperty("type")]
        public string Type { get; set; }   // join, where, group by, having, orderby

        [JsonProperty("suggestion")]
        public string Suggestion { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }
    }

}
