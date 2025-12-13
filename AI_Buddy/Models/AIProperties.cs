using Microsoft.Build.Framework;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AI_Buddy.Models
{
    internal class AIProperties
    {
        [Category("AI Provider")]
        [DisplayName("Name of AI provider")]
        [Description("For your reference, the name of the prompt provider.")]
        public string AIProvider { get; set; } = "DeepSeek";

        [Category("AI Provider")]
        [DisplayName("Prompt API URL")]
        [Description("The Prompt URL that will get called with your prompt.")]
        public string AIPromptURL { get; set; } = "http://localhost:11434/api/generate";

        [Category("AI Provider")]
        [DisplayName("API Key")]
        [Description("The key may not be required for internal server prompts.")]
        public string AIPromptKey { get; set; }

        [Category("AI Provider")]
        [DisplayName("LLM Name")]
        [Description("For your reference, what LLM model your Hoster is using (for e.g deepseek-r1:70b)")]
        public string PromptLLMName { get; set; } = "deepseek-r1:1.5b";

        [Category("AI Provider")]
        [DisplayName("Streaming Response")]
        [Description("Do you want to stream the prompt response in realtime.")]
        public bool IsPromptResponseStreaming { get; set; } = false;

        [Category("Coding")]
        [DisplayName("Software language")]
        [Description("Coding Language that you want AI prompt to respond in (for e.g. Java, .Net4.8, .Net9 etc.)")]
        public string CodingLanguage { get; set; } = "C#";

        [Category("Coding")]
        [DisplayName("Testing Framework")]
        [Description("Which testing framework to use in prompts (for e.g. NUnit, MSTest, XUnit etc.)")]
        public string TestFramework { get; set; } = "MSTest";

        [Category("General Info")]
        [DisplayName("Last Modified")]
        [Description("Whan you last updated the properties.")]
        [ReadOnly(true)]
        public DateTime DateLastModified { get; set; } = DateTime.Now;

        [Category("General Info")]
        [DisplayName("Last Prompt")]
        [Description("The last prompt you made.")]
        [ReadOnly(true)]
        public string LastPrompt { get; set; }

        [Category("General Info")]
        [DisplayName("Settings Filename")]
        [Description("The file where these settings are saved to (readonly).")]
        [ReadOnly(true)]
        public string SettingsFilename { get; set; } = "AI_Buddy_Settings.json";

    }
}
