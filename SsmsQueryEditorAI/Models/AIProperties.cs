using Microsoft.Build.Framework;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SSMS_AI_Buddy.Models
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
        [DisplayName("SSMS Version")]
        [Description("What version of SSMS are you requesting answers from AI prompt to execute on")]
        public string SsmsVersion { get; set; } = "SSMS 19+";

        [Category("Coding")]
        [DisplayName("SQL Language")]
        [Description("What SQL based procedural language are you requesting answers from AI prompt to execute against")]
        public string SqlVersion { get; set; } = "TSQL";

        [Category("Coding")]
        [DisplayName("Realtime SQL Suggestion (Joins)")]
        [Description("Use existing schema instead of AI to generate suggestions for JOINs when typing in editor")]
        public bool IsRealTime { get; set; } = false;

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
        public string SettingsFilename { get; set; } = "AI_Buddy_SSMS_Settings.json";

        [Browsable(false)]
        [Category("General Info")]
        [DisplayName("Reset Filename")]
        [Description("The file holds the previous settings (readonly).")]
        [ReadOnly(true)]
        public string ResetFilename { get; set; } = "AI_Buddy_SSMS_Settings_Reset.json";

    }
}
