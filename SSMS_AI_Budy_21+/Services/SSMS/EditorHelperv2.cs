using Microsoft.VisualStudio.Text.Editor;

namespace SSMS_AI_Buddy.Services.SSMS
{
    public static class EditorHelperv2
    {
        public static IWpfTextView ActiveView { get; set; }
        public static void InsertText(string text)
        {
            var view = ActiveView;
            if (view == null) return;

            using (var edit = view.TextBuffer.CreateEdit())
            {
                edit.Insert(view.Caret.Position.BufferPosition, text);
                edit.Apply();
            }
        }

        public static string GetCurrentEditorText()
        {
            return ActiveView?.TextSnapshot.GetText() ?? string.Empty;
        }
    }
}
