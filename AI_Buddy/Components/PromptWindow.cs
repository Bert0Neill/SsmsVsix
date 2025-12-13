using Microsoft.VisualStudio.Shell;
using Newtonsoft.Json.Linq;
using System;
using System.Runtime.InteropServices;
using System.Windows.Documents;

namespace AI_Buddy.Components
{
    /// <summary>
    /// This class implements the tool window exposed by this package and hosts a user control.
    /// </summary>
    /// <remarks>
    /// In Visual Studio tool windows are composed of a frame (implemented by the shell) and a pane,
    /// usually implemented by the package implementer.
    /// <para>
    /// This class derives from the ToolWindowPane class provided from the MPF in order to use its
    /// implementation of the IVsUIElementPane interface.
    /// </para>
    /// </remarks>
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

        /// <summary>
        /// Initializes a new instance of the <see cref="PromptWindow"/> class.
        /// </summary>
        public PromptWindow() : base(null)
        {            
            this.Caption = "AI Buddy Prompt Pane";

            // This is the user control hosted by the tool window; Note that, even if this class implements IDisposable,
            // we are not calling Dispose on this object. This is because ToolWindowPane calls Dispose on
            // the object returned by the Content property.
            this.Content = new PromptWindowControl();
        }
    }
}
