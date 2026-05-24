namespace SSMS_AI_Buddy.Models
{

    public class AIProviderOptions
    {
        // extend as needed, but for now we have two options: Nvidia (Remote) and Ollama (Locally)
        public const string NvidiaRemote = "Nvidia (Remote)"; // key required and prompt built up differently
        public const string OllamaLocal = "Ollama (Local)";

        public static readonly string[] All = { NvidiaRemote, OllamaLocal };

        public AIProviderOptions()
        {

        }
    }
}
