using EnvDTE;
using EnvDTE80;
using Microsoft.Identity.Client;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using SSMS_AI_Buddy.Components;
using SSMS_AI_Buddy.Models;
using SSMS_AI_Buddy.Services;
using SSMS_AI_Buddy.Services.AI;
using System;
using System.ComponentModel.Design;
using System.Globalization;
using System.IO;
using static System.Net.Mime.MediaTypeNames;
using Task = System.Threading.Tasks.Task;

namespace SSMS_AI_Buddy.Commands
{
    /// <summary>
    /// SQL editor context menu: AI Buddy submenu commands.
    /// </summary>
    internal static class AIBuddySqlEditorCommands
    {
        private static AsyncPackage _package;

        public const int SqlJoinsRealtimeId = 0x0101;
        public const int SqlJoinsAiId = 0x0102;
        public const int WhereClauseId = 0x0103;
        public const int SuggestGroupHavingId = 0x0104;
        public const int SuggestSqlContinuationId = 0x0105;
        public const int DisplayPromptId = 0x0106;

        static AIProperties _aiProperties;
        

        public static readonly Guid CommandSet = new Guid("d5cfa9e8-a7de-4eb4-b34f-0ab39f9e31a3");

        public static async Task InitializeAsync(AsyncPackage package)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(package.DisposalToken);
            _package = package;

            var commandService = await package.GetServiceAsync(typeof(IMenuCommandService)) as OleMenuCommandService;
            if (commandService == null)
                return;

            Add(package, commandService, SqlJoinsRealtimeId, ExecuteSqlJoinsRealtimeRules);
            Add(package, commandService, SqlJoinsAiId, ExecuteSqlJoinsAi);
            Add(package, commandService, WhereClauseId, ExecuteWhereClauseAi);
            Add(package, commandService, DisplayPromptId, ExecuteDisplayPrompt);
            Add(package, commandService, SuggestGroupHavingId, ExecuteSuggestGroupHaving);
            Add(package, commandService, SuggestSqlContinuationId, ExecuteSuggestSqlContinuation);

        }

        private static void Add(AsyncPackage package, OleMenuCommandService commandService, int commandId, EventHandler execute)
        {
            var id = new CommandID(CommandSet, commandId);
            commandService.AddCommand(new MenuCommand(execute, id));
        }

        private static void ExecuteSqlJoinsRealtimeRules(object sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            ShowStub(nameof(ExecuteSqlJoinsRealtimeRules), "SQL Joins Realtime...");
        }

        private static void LoadSettingsAI()
        {
            FileService _fileService = new FileService();
            string basePath = string.Empty;
            string settingsFilePath = string.Empty;
            basePath = AppDomain.CurrentDomain.BaseDirectory;
            settingsFilePath = Path.Combine(basePath, _aiProperties.SettingsFilename);

            // if file doesn't exist yet, default AI properties will be used
            if (File.Exists(settingsFilePath))
            {
                _aiProperties = _fileService.LoadFromJson<AIProperties>(settingsFilePath);
            }
            else {
                _aiProperties = new AIProperties(); // default settings
            }

            OllamaSqlSuggestionService._aiProperties = _aiProperties;
        }

        private static void ExecuteSuggestGroupHaving(object sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            LoadSettingsAI();

            ShowStub(nameof(ExecuteSqlJoinsRealtimeRules), "SQL suggestion Group Having clause Using AI");
        }

        private static void ExecuteSqlJoinsAi(object sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            LoadSettingsAI();
            ShowStub(nameof(ExecuteSqlJoinsAi), "SQL Joins AI...");
        }

        private static void ExecuteSuggestSqlContinuation(object sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            LoadSettingsAI();
            ShowStub(nameof(ExecuteSuggestSqlContinuation), "SQL Continuation AI...");
        }

        private static void ExecuteWhereClauseAi(object sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            LoadSettingsAI();
            ShowStub(nameof(ExecuteWhereClauseAi), "SQL Where Clause AI...");
        }

        private static void ExecuteDisplayPrompt(object sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            LoadSettingsAI();

            // Get selected SQL from the editor
            string highlightedSql = GetSelectedSqlText();

            // Find or create the PromptWindow
            var promptWindow = _package.FindToolWindow(typeof(PromptWindow), 0, true) as PromptWindow;
            if (promptWindow?.Frame == null)
            {
                throw new NotSupportedException("Cannot create Prompt Window.");
            }

            // Show the window
            var windowFrame = (IVsWindowFrame)promptWindow.Frame;
            Microsoft.VisualStudio.ErrorHandler.ThrowOnFailure(windowFrame.Show());

            // Pass the highlighted SQL to the prompt
            if (!String.IsNullOrEmpty(highlightedSql))
            {
                if (promptWindow.Content is PromptWindowControl control)
                {
                    control.SetPromptText(highlightedSql);
                    control.SetSettingsAI(_aiProperties);
                }
            }
        }

        private static void ShowStub(string callbackName, string title)
        {
            if (_package == null)
                return;

            string message = string.Format(CultureInfo.CurrentCulture, "Inside {0}.{1}", typeof(AIBuddySqlEditorCommands).FullName, callbackName);
            VsShellUtilities.ShowMessageBox(
                _package,
                message,
                title,
                OLEMSGICON.OLEMSGICON_INFO,
                OLEMSGBUTTON.OLEMSGBUTTON_OK,
                OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
        }

        public static string GetSelectedSqlText()
        {
            var dte = (DTE2)Package.GetGlobalService(typeof(DTE));
            if (dte?.ActiveDocument == null)
                return string.Empty;

            TextSelection selection = (TextSelection)dte.ActiveDocument.Selection;

            return selection?.Text ?? string.Empty;
        }
    }
}
