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
        [Description("Do you want to stream the prompt response in real-time.")]
        public bool IsPromptResponseStreaming { get; set; } = false;

        [Category("Coding")]
        [DisplayName("SSMS Version")]
        [Description("What version of SSMS are you requesting answers from AI prompt to execute on")]
        public string SsmsVersion { get; set; } = "SSMS 19+";

        [Category("Coding")]
        [DisplayName("SQL Language")]
        [Description("What SQL based procedural language are you requesting answers from AI prompt to execute against")]
        public string SqlVersion { get; set; } = "TSQL";

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

        /******************************************
         * Hidden Properties
         ******************************************/
        [Browsable(false)]
        [Category("General Info")]
        [DisplayName("Reset Filename")]
        [Description("The file holds the previous settings (readonly).")]
        [ReadOnly(true)]
        public string ResetFilename { get; set; } = "AI_Buddy_SSMS_Settings_Reset.json";

        [Browsable(false)]
        [Category("Coding")]
        [DisplayName("Realtime SQL Suggestion (Joins)")]
        [Description("Use existing schema instead of AI to generate suggestions for JOINs when typing in editor")]
        public bool IsRealTime { get; set; } = false;

        /******************************************
         * ReadOnly Prompts
         ******************************************/
        [Browsable(true)]
        [Category("Prompts")]
        [DisplayName("System Prompt")]
        [Description("How the AI LLM should react to your prompt, what rules it must follow when generating its answer")]
        [ReadOnly(true)]
        public string SystemPrompt { get; set; } = @"No thinking. You are a backend SQL completion engine.
STRICT RULES:
- Output ONLY valid JSON.
- Output MUST be a COMPLETE JSON array.
- Do NOT include markdown, code fences, comments, or extra text.
- Do NOT include trailing commas.

COMPLETION RULES:
- You are completing an EXISTING query; the user keeps their SQL. Output ONLY the next fragment to append.
- DO NOT rewrite the full query. DO NOT duplicate SELECT ... FROM ... for the whole statement.
- The JSON ""suggestion"" field MUST NOT start with SELECT. Output from JOIN, WHERE, GROUP BY, ORDER BY, HAVING, or a comma + column.
- When joins apply: include several join suggestions with different join kinds (INNER JOIN, LEFT JOIN, and when sensible RIGHT JOIN, FULL OUTER JOIN). Same aliases and ON clause where only the join kind differs. CROSS JOIN only if a cross product is appropriate.

GOOD examples:
- ""INNER JOIN SalesLT.CustomerAddress AS ca ON a.AddressID = ca.AddressID""
- ""LEFT JOIN SalesLT.CustomerAddress AS ca ON a.AddressID = ca.AddressID""
- ""WHERE a.City = N'London'""
- "", PostalCode""

BAD examples:
- Anything starting with SELECT, UPDATE, DELETE, INSERT.
- Full standalone queries";


        [Browsable(true)]
        [Category("Prompts")]
        [DisplayName("Where Prompt")]
        [Description("The 'Where' prompt that will be supplied to AI along with your (highlighted) SQL statement")]
        [ReadOnly(true)]
        public string WherePrompt { get; set; }

        [Browsable(true)]
        [Category("Prompts")]
        [DisplayName(@"Group\Having Prompt")]
        [Description(@"The 'Group\Having' prompt that will be supplied to AI along with your (highlighted) SQL statement")]
        [ReadOnly(true)]
        public string GroupHavingPrompt { get; set; }

        [Browsable(true)]
        [Category("Prompts")]
        [DisplayName(@"Join Prompt")]
        [Description(@"The 'Join' prompt that will be supplied to AI along with your (highlighted) SQL statement")]
        [ReadOnly(true)]
        public string JoinPrompt { get; set; }

        [Browsable(true)]
        [Category("Prompts")]
        [DisplayName(@"Continuation Prompt")]
        [Description(@"The 'Continuation' prompt that will be supplied to AI along with your (highlighted) SQL statement")]
        [ReadOnly(true)]
        public string ContinuationPrompt { get; set; }

        [Browsable(true)]
        [Category("Prompts")]
        [DisplayName(@"RealTime Prompt")]
        [Description(@"The 'RealTime' prompt that will be supplied to AI along with your (highlighted) SQL statement")]
        [ReadOnly(true)]
        public string RealTimePrompt { get; set; } = $@"";
    }
}
