using System.Windows.Documents;
using System.Windows.Media;
using System.Collections.Generic;

namespace SSMS_AI_Buddy.Services.Helper
{
    public static class MarkdownRenderer
    {
        public static Paragraph[] RenderMarkdown(string markdown)
        {
            var paragraphs = new List<Paragraph>();
            var lines = markdown.Split(new[] { "\r\n", "\n" }, System.StringSplitOptions.None);

            bool inCodeBlock = false;
            Paragraph codeParagraph = null;

            foreach (var line in lines)
            {
                if (line.StartsWith("```"))
                {
                    if (!inCodeBlock)
                    {
                        // Enter code block
                        inCodeBlock = true;
                        codeParagraph = new Paragraph
                        {
                            Background = new SolidColorBrush(Color.FromRgb(30, 30, 30)),
                            Foreground = new SolidColorBrush(Colors.LightGreen),
                            FontFamily = new FontFamily("Consolas"),
                            Margin = new System.Windows.Thickness(4),
                            Padding = new System.Windows.Thickness(4)
                        };
                    }
                    else
                    {
                        // Exit code block
                        inCodeBlock = false;
                        paragraphs.Add(codeParagraph);
                        codeParagraph = null;
                    }
                    continue;
                }

                if (inCodeBlock)
                {
                    codeParagraph.Inlines.Add(new Run(line + "\n"));
                }
                else
                {
                    paragraphs.Add(new Paragraph(new Run(line)));
                }
            }

            return paragraphs.ToArray();
        }
    }
}