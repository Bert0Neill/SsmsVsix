using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;

namespace SSMS_AI_Buddy.Services.Helper
{
    public static class MarkdownRenderer
    {
        private static readonly Brush CodeBackground = new SolidColorBrush(Color.FromRgb(37, 37, 38));
        private static readonly Brush CodeForeground = new SolidColorBrush(Color.FromRgb(212, 212, 212));

        private static readonly Brush SqlKeywordColor = new SolidColorBrush(Color.FromRgb(86, 156, 214));
        private static readonly Brush CSharpKeywordColor = new SolidColorBrush(Color.FromRgb(86, 156, 214));
        private static readonly Brush StringColor = new SolidColorBrush(Color.FromRgb(214, 157, 133));
        private static readonly Brush CommentColor = new SolidColorBrush(Color.FromRgb(87, 166, 74));

        private static readonly HashSet<string> SqlKeywords = new HashSet<string>
        {
            "SELECT","FROM","WHERE","JOIN","INNER","LEFT","RIGHT","FULL","OUTER",
            "GROUP","BY","ORDER","INSERT","UPDATE","DELETE","INTO","VALUES",
            "TOP","DISTINCT","AND","OR","NOT","NULL","LIKE","IN","AS"
        };

        private static readonly HashSet<string> CSharpKeywords = new HashSet<string>
        {
            "public","private","protected","class","void","var","int","string",
            "return","async","await","new","if","else","foreach","while",
            "using","namespace","static","bool","true","false","null"
        };

        public static IEnumerable<Block> Parse(string markdown)
        {
            var blocks = new List<Block>();

            bool inCodeBlock = false;
            string codeLanguage = "";
            StringBuilder codeBuffer = new StringBuilder();

            var lines = markdown.Split('\n');

            foreach (var raw in lines)
            {
                var line = raw.TrimEnd('\r');
                var trimmed = line.TrimStart();

                // skip blank lines
                if (string.IsNullOrWhiteSpace(trimmed))
                    continue;

                // Horizontal rule
                if (trimmed == "---")
                {
                    blocks.Add(new Paragraph(new Run("────────────────────"))
                    {
                        Margin = new Thickness(2, 6, 2, 6)
                    });

                    continue;
                }

                // CODE BLOCK START / END
                if (trimmed.StartsWith("```"))
                {
                    if (!inCodeBlock)
                    {
                        inCodeBlock = true;
                        //codeLanguage = trimmed.Replace("```", "").Trim().ToLower();
                        codeLanguage = trimmed.Substring(3).Trim().ToLower();
                        codeBuffer.Clear();
                    }
                    else
                    {
                        inCodeBlock = false;
                        // Add the code block as a selectable TextBox
                        blocks.Add(CreateCodeBlock(codeBuffer.ToString(), codeLanguage));
                        codeBuffer.Clear();
                    }
                    continue;
                }

                // CODE BLOCK CONTENT
                if (inCodeBlock)
                {
                    codeBuffer.AppendLine(line);
                    continue;
                }

                // HEADINGS (#, ##, ###, etc.)
                var headingMatch = Regex.Match(trimmed, @"^(#{1,6})\s+(.*)");
                if (headingMatch.Success)
                {
                    int level = headingMatch.Groups[1].Value.Length;
                    string content = headingMatch.Groups[2].Value;

                    var paragraph = new Paragraph
                    {
                        Margin = new Thickness(4, 8, 4, 4)
                    };

                    // Bold for heading text
                    paragraph.Inlines.Add(new Bold(new Run(content)));

                    // Adjust font size based on heading level
                    paragraph.FontSize = 24 - (level * 2);
                    blocks.Add(paragraph);
                    continue;
                }

                // BULLET LIST                
                if (trimmed.StartsWith("- ") || trimmed.StartsWith("• "))
                {
                    var paragraph = new Paragraph
                    {
                        Margin = new Thickness(20, 2, 2, 2)
                    };

                    string bulletText = trimmed.StartsWith("- ") ? trimmed.Substring(2) : trimmed.Substring(1).TrimStart();

                    // Add the bullet symbol
                    paragraph.Inlines.Add(new Run("• "));

                    // Parse inline markdown in the bullet text
                    ParseInline(paragraph, bulletText);

                    blocks.Add(paragraph);
                    continue;
                }

                // NORMAL TEXT (with inline markdown)
                var normalParagraph = new Paragraph
                {
                    Margin = new Thickness(4)
                };
                ParseInline(normalParagraph, trimmed);
                blocks.Add(normalParagraph);
            }

            // flush unclosed code block
            if (inCodeBlock && codeBuffer.Length > 0)
            {
                blocks.Add(CreateCodeBlock(codeBuffer.ToString(), codeLanguage));
            }

            return blocks;
        }

        private static void ParseInline(Paragraph paragraph, string text)
        {
            var tokens = Regex.Split(text, @"(\*\*.*?\*\*|\*.*?\*)");

            foreach (var token in tokens)
            {
                if (token.StartsWith("**") && token.EndsWith("**"))
                {
                    paragraph.Inlines.Add(new Bold(new Run(token.Trim('*'))));
                }
                else if (token.StartsWith("*") && token.EndsWith("*"))
                {
                    paragraph.Inlines.Add(new Italic(new Run(token.Trim('*'))));
                }
                else
                {
                    paragraph.Inlines.Add(new Run(token));
                }
            }
        }

        private static void AddHighlightedLine(Paragraph paragraph, string line, string language)
        {
            var tokens = Regex.Split(line, @"(\s+|""[^""]*""|//.*)");

            foreach (var token in tokens)
            {
                Run run = new Run(token);

                if (token.StartsWith("//"))
                {
                    run.Foreground = CommentColor;
                }
                else if (token.StartsWith("\"") && token.EndsWith("\""))
                {
                    run.Foreground = StringColor;
                }
                else
                {
                    var upper = token.ToUpper();

                    if (language == "sql" && SqlKeywords.Contains(upper))
                    {
                        run.Foreground = SqlKeywordColor;
                        run.FontWeight = FontWeights.Bold;
                    }
                    else if ((language == "csharp" || language == "cs") && CSharpKeywords.Contains(token))
                    {
                        run.Foreground = CSharpKeywordColor;
                        run.FontWeight = FontWeights.Bold;
                    }
                }

                paragraph.Inlines.Add(run);
            }
        }

        public static Block CreateCodeBlock(string code, string language = "sql")
        {
            var paragraph = new Paragraph
            {
                Background = new SolidColorBrush(Color.FromRgb(37, 37, 38)),   // dark background
                Foreground = new SolidColorBrush(Color.FromRgb(212, 212, 212)), // light text
                FontFamily = new FontFamily("Consolas"),
                FontSize = 12,
                Margin = new Thickness(4),
                Padding = new Thickness(6),
                LineHeight = 16,
                LineStackingStrategy = LineStackingStrategy.BlockLineHeight
            };

            // Split the code into lines
            var lines = code.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);

            for (int i = 0; i < lines.Length; i++)
            {
                // Apply syntax highlighting
                AddHighlightedLine(paragraph, lines[i], language);

                // Only add a line break if it's NOT the last line
                if (i < lines.Length - 1)
                {
                    paragraph.Inlines.Add(new LineBreak());
                }
            }

            return paragraph;
        }
    }
}