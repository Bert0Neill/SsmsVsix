using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.ComponentModel.Design;
using System.Globalization;
using System.Threading.Tasks;
using Task = System.Threading.Tasks.Task;

namespace SSMS_AI_Buddy.Commands
{
    /// <summary>
    /// SQL editor context menu: AI Buddy submenu commands.
    /// </summary>
    internal static class AIBuddySqlEditorCommands
    {
        private static AsyncPackage _package;

        public const int SqlContinuationRulesId = 0x0101;
        public const int SqlContinuationAiId = 0x0102;
        public const int CorrectSqlErrorsId = 0x0103;
        public const int DisplayPromptId = 0x0104;
        public const int SuggestGroupHavingId = 0x0105;
        public const int SuggestSqlContinuationId = 0x0106;

        public static readonly Guid CommandSet = new Guid("d5cfa9e8-a7de-4eb4-b34f-0ab39f9e31a3");

        public static async Task InitializeAsync(AsyncPackage package)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(package.DisposalToken);
            _package = package;

            var commandService = await package.GetServiceAsync(typeof(IMenuCommandService)) as OleMenuCommandService;
            if (commandService == null)
                return;

            Add(package, commandService, SqlContinuationRulesId, ExecuteSqlContinuationRules);
            Add(package, commandService, SqlContinuationAiId, ExecuteSqlContinuationAi);
            Add(package, commandService, CorrectSqlErrorsId, ExecuteCorrectSqlErrors);
            Add(package, commandService, DisplayPromptId, ExecuteDisplayPrompt);
            Add(package, commandService, SuggestGroupHavingId, ExecuteSuggestGroupHaving);
            Add(package, commandService, SuggestSqlContinuationId, ExecuteSuggestSqlContinuation);
        }

        private static void Add(AsyncPackage package, OleMenuCommandService commandService, int commandId, EventHandler execute)
        {
            var id = new CommandID(CommandSet, commandId);
            commandService.AddCommand(new MenuCommand(execute, id));
        }

        private static void ExecuteSuggestSqlContinuation(object sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            ShowStub(nameof(ExecuteSqlContinuationRules), "SQL suggestion continuation Using AI");
        }


        private static void ExecuteSuggestGroupHaving(object sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            ShowStub(nameof(ExecuteSqlContinuationRules), "SQL suggestion Group Having clause Using AI");
        }

        private static void ExecuteSqlContinuationRules(object sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            ShowStub(nameof(ExecuteSqlContinuationRules), "SQL Continuation Using Rules");
        }

        private static void ExecuteSqlContinuationAi(object sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            ShowStub(nameof(ExecuteSqlContinuationAi), "SQL Continuation Using AI");
        }

        private static void ExecuteCorrectSqlErrors(object sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            ShowStub(nameof(ExecuteCorrectSqlErrors), "Correct SQL errors");
        }

        private static void ExecuteDisplayPrompt(object sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            ShowStub(nameof(ExecuteDisplayPrompt), "Display Prompt");
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
    }
}
