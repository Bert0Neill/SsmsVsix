using Microsoft.VisualStudio.Shell;
using System;
using System.Runtime.InteropServices;
using System.Windows.Documents;

namespace SSMS_AI_Buddy.Components
{
    [Guid("0713738e-d4a3-4b80-bc3d-529ac3e5212c")]
    public class PromptWindow : ToolWindowPane
    {
        private string _promptResponse;
        public string PromptResponse
        {
            get => _promptResponse;
            set
            {
                _promptResponse = value;
                if (this.Content is PromptWindowControl control)
                {
                    control.UpdatePromptResponse(value);
                }
            }
        }

        private Paragraph[] _formattedPrompt;
        public Paragraph[] FormattedPrompt
        {
            get => _formattedPrompt;
            set
            {
                _formattedPrompt = value;
                if (this.Content is PromptWindowControl control)
                {
                    control.UpdateResultPrompt(value);
                }
            }
        }

        public PromptWindow() : base(null)
        {
            this.Caption = "SQL AI Buddy Prompt Pane";
            this.Content = new PromptWindowControl();
        }
    }
}