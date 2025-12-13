using AI_Buddy.Components;
using AI_Buddy.Models;
using AI_Buddy.Resources;
using AI_Buddy.Services;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.ComponentModel.Design;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Task = System.Threading.Tasks.Task;

namespace AI_Buddy.Commands
{
    /// <summary>
    /// Command handler
    /// </summary>
    internal sealed class GenerateCodeFromHintCmd
    {
        /// <summary>
        /// Command ID.
        /// </summary>
        public const int CommandId = 4136;

        /// <summary>
        /// Command menu group (command set GUID).
        /// </summary>
        public static readonly Guid CommandSet = new Guid("f3ae7c69-4f62-438d-a386-a11549044a8c");

        /// <summary>
        /// VS Package that provides this command, not null.
        /// </summary>
        private readonly AsyncPackage _package;
        private readonly EditorService _editorService;
        private readonly AIService _aiService;
        private readonly AIProperties _aiProperties;
        private readonly FileService _fileService;
        private readonly RichTextBoxParagraphGenerator _richTextBoxParagraphGenerator;

        /// <summary>
        /// Initializes a new instance of the <see cref="GenerateCodeFromHintCmd"/> class.
        /// Adds our command handlers for menu (commands must exist in the command table file)
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        /// <param name="commandService">Command service to add command to, not null.</param>
        private GenerateCodeFromHintCmd(AsyncPackage package, OleMenuCommandService commandService)
        {
            this._package = package ?? throw new ArgumentNullException(nameof(package));
            commandService = commandService ?? throw new ArgumentNullException(nameof(commandService));

            var menuCommandID = new CommandID(CommandSet, CommandId);
            var menuItem = new MenuCommand(this.Execute, menuCommandID);
            commandService.AddCommand(menuItem);

            _editorService = new EditorService();
            _aiService = new AIService();
            _aiProperties = new AIProperties();
            _fileService = new FileService();
            _richTextBoxParagraphGenerator = new RichTextBoxParagraphGenerator();

            // read file settings
            string filePath = Path.Combine(Directory.GetCurrentDirectory(), _aiProperties.SettingsFilename);

            if (File.Exists(filePath))
            {
                _aiProperties = _fileService.LoadFromJson<AIProperties>(filePath);
            }
        }

        /// <summary>
        /// Gets the instance of the command.
        /// </summary>
        public static GenerateCodeFromHintCmd Instance
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the service provider from the owner package.
        /// </summary>
        private Microsoft.VisualStudio.Shell.IAsyncServiceProvider ServiceProvider
        {
            get
            {
                return this._package;
            }
        }

        /// <summary>
        /// Initializes the singleton instance of the command.
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        public static async Task InitializeAsync(AsyncPackage package)
        {
            // Switch to the main thread - the call to AddCommand in GenerateCodeFromTextCmd's constructor requires
            // the UI thread.
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(package.DisposalToken);

            OleMenuCommandService commandService = await package.GetServiceAsync(typeof(IMenuCommandService)) as OleMenuCommandService;
            Instance = new GenerateCodeFromHintCmd(package, commandService);
        }

        /// <summary>
        /// This function is the callback used to execute the command when the menu item is clicked.
        /// See the constructor to see how the menu item is associated with this function using
        /// OleMenuCommandService service and MenuCommand class.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event args.</param>
        private async void Execute(object sender, EventArgs e)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            string text = await _editorService.GetSelectedTextAsync(this._package);

            if (!string.IsNullOrEmpty(text))
            {
                // generate unit test for prompt
                string prompt = String.Format(PromptStrings.GenerateCodeFromHighlightedTextPrompt, _aiProperties.CodingLanguage, text);

                // Initialize and show the window once before streaming
                var promptWindow = this._package.FindToolWindow(typeof(PromptWindow), 0, true) as PromptWindow;
                if (promptWindow?.Frame == null)
                {
                    throw new NotSupportedException("Cannot create Prompt Window.");
                }

                promptWindow.FormattedPrompt = _richTextBoxParagraphGenerator.GenerateCodeFromHintParagraph(prompt, text, _aiProperties); // update window panel control

                var windowFrame = (IVsWindowFrame)promptWindow.Frame;
                Microsoft.VisualStudio.ErrorHandler.ThrowOnFailure(windowFrame.Show());

                // Start streaming and update the window's control for each chunk
                await _aiService.GetOllamaResponseStreamAsync(prompt, chunk =>
                {
                    // Append or update the response without re-showing the window
                    promptWindow.PromptResponse = chunk;
                });
            }
            else
            {
                VsShellUtilities.ShowMessageBox
                   (
                       this._package,
                       $"No text highlighed, to generate code from hints.",
                       "No Text Selected",
                       OLEMSGICON.OLEMSGICON_WARNING,
                       OLEMSGBUTTON.OLEMSGBUTTON_OK,
                       OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST
                   );
            }
        }
    }
}
