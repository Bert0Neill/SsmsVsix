using AI_Buddy.Models;
using System;
using System.Windows.Controls;
using System.Windows.Documents;

namespace AI_Buddy.Services
{
    internal class RichTextBoxParagraphGenerator
    {
        public Paragraph[] GenerateUnitTestParagraph(string prompt, string code, AIProperties aiProperties, bool isRtbEmpty = false)
        {
            Paragraph paragraph = new Paragraph();

            var promptDetails = new Paragraph[2];
            Run promptLabel = new Run($"{(!isRtbEmpty ? Environment.NewLine : string.Empty)} Prompt: ")
            {
                Foreground = System.Windows.Media.Brushes.Black,
            };

            Run promptDescription = new Run($"Generating a Unit Test ({aiProperties.TestFramework}) in {aiProperties.CodingLanguage} for your code: {Environment.NewLine}")
            {
                Foreground = System.Windows.Media.Brushes.Blue
            };
            paragraph.Inlines.Add(promptLabel);
            paragraph.Inlines.Add(promptDescription);

            promptDetails[0] = paragraph;

            // create a Run for the code (blue & italic)
            paragraph = new Paragraph();
            Run promptCode = new Run($"{code} {Environment.NewLine + Environment.NewLine}")
            {
                Foreground = System.Windows.Media.Brushes.Black,
                FontStyle = System.Windows.FontStyles.Italic
            };
            paragraph.Inlines.Add(promptCode);
            promptDetails[1] = paragraph;

            return promptDetails;
        }

        public Paragraph[] GenerateCommentsFromCodeParagraph(string prompt, string code, AIProperties aiProperties, bool isRtbEmpty = false)
        {
            Paragraph paragraph = new Paragraph();

            var promptDetails = new Paragraph[2];
            Run promptLabel = new Run($"{(!isRtbEmpty ? Environment.NewLine : string.Empty)} Prompt: ")
            {
                Foreground = System.Windows.Media.Brushes.Black,
            };

            Run promptDescription = new Run($"Generating comments for your code: {Environment.NewLine}")
            {
                Foreground = System.Windows.Media.Brushes.Blue
            };
            paragraph.Inlines.Add(promptLabel);
            paragraph.Inlines.Add(promptDescription);

            promptDetails[0] = paragraph;

            // create a Run for the code (blue & italic)
            paragraph = new Paragraph();
            Run promptCode = new Run($"{code} {Environment.NewLine + Environment.NewLine}")
            {
                Foreground = System.Windows.Media.Brushes.Black,
                FontStyle = System.Windows.FontStyles.Italic
            };
            paragraph.Inlines.Add(promptCode);
            promptDetails[1] = paragraph;

            return promptDetails;
        }

        public Paragraph[] GenerateCodeFromHintParagraph(string prompt, string hints, AIProperties aiProperties, bool isRtbEmpty = false)
        {
            Paragraph paragraph = new Paragraph();

            var promptDetails = new Paragraph[2];
            Run promptLabel = new Run($"{(!isRtbEmpty ? Environment.NewLine : string.Empty)} Prompt: ")
            {
                Foreground = System.Windows.Media.Brushes.Black,
            };

            Run promptDescription = new Run($"Generating {aiProperties.CodingLanguage} code based on your suggestions: {Environment.NewLine}")
            {
                Foreground = System.Windows.Media.Brushes.Blue
            };
            paragraph.Inlines.Add(promptLabel);
            paragraph.Inlines.Add(promptDescription);

            promptDetails[0] = paragraph;

            // create a Run for the code (blue & italic)
            paragraph = new Paragraph();
            Run promptCode = new Run($"{hints} {Environment.NewLine + Environment.NewLine}")
            {
                Foreground = System.Windows.Media.Brushes.Black,
                FontStyle = System.Windows.FontStyles.Italic
            };
            paragraph.Inlines.Add(promptCode);
            promptDetails[1] = paragraph;

            return promptDetails;
        }

        public Paragraph GenerateSubmitPromptParagraph(string prompt, bool isRtbEmpty = false)
        {
            //Create a new Paragraph
            Paragraph paragraph = new Paragraph();

            // Create a Run for "Prompt:" in black
            Run promptLabel = new Run($"{(!isRtbEmpty ? Environment.NewLine : string.Empty)} Prompt: ")
            {
                Foreground = System.Windows.Media.Brushes.Black
            };

            // Create a Run for the actual prompt text in blue
            Run promptRun = new Run($"{prompt}{Environment.NewLine + Environment.NewLine}") // add blank line after prompt
            {
                Foreground = System.Windows.Media.Brushes.Blue
            };

            // Add both Runs to the Paragraph
            paragraph.Inlines.Add(promptLabel);
            paragraph.Inlines.Add(promptRun);

            return paragraph;
        }

        public bool IsRichTextBoxEmpty(RichTextBox rtb)
        {
            TextRange textRange = new TextRange(rtb.Document.ContentStart, rtb.Document.ContentEnd);
            return string.IsNullOrWhiteSpace(textRange.Text);
        }

        internal Paragraph[] SuggestCodeImprovements(string prompt, string code, AIProperties aiProperties, bool isRtbEmpty = false)
        {
            Paragraph paragraph = new Paragraph();

            var promptDetails = new Paragraph[2];
            Run promptLabel = new Run($"{(!isRtbEmpty ? Environment.NewLine : string.Empty)} Prompt: ")
            {
                Foreground = System.Windows.Media.Brushes.Black,
            };

            Run promptDescription = new Run($"Generating coding imrpovements for your highlighted code: {Environment.NewLine}")
            {
                Foreground = System.Windows.Media.Brushes.Blue
            };
            paragraph.Inlines.Add(promptLabel);
            paragraph.Inlines.Add(promptDescription);

            promptDetails[0] = paragraph;

            // create a Run for the code (blue & italic)
            paragraph = new Paragraph();
            Run promptCode = new Run($"{code} {Environment.NewLine + Environment.NewLine}")
            {
                Foreground = System.Windows.Media.Brushes.Black,
                FontStyle = System.Windows.FontStyles.Italic
            };
            paragraph.Inlines.Add(promptCode);
            promptDetails[1] = paragraph;

            return promptDetails;
        }
    }
}
