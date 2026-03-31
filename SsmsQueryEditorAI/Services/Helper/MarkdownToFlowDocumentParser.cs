using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
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
            Paragraph codeParagraph = null;

            var lines = markdown.Split('\n');

            foreach (var raw in lines)
            {
                var line = raw.TrimEnd('\r');
                var trimmed = line.TrimStart();

                // CODE BLOCK START / END
                if (trimmed.StartsWith("```"))
                {
                    if (!inCodeBlock)
                    {
                        inCodeBlock = true;
                        codeLanguage = trimmed.Replace("```", "").Trim().ToLower();

                        codeParagraph = new Paragraph
                        {
                            Margin = new Thickness(6),
                            Padding = new Thickness(10),
                            Background = CodeBackground,
                            FontFamily = new FontFamily("Consolas"),
                            Foreground = CodeForeground
                        };
                    }
                    else
                    {
                        inCodeBlock = false;

                        // Create FlowDocument to control wrapping
                        var doc = new FlowDocument(codeParagraph)
                        {
                            PagePadding = new Thickness(0),
                            TextAlignment = TextAlignment.Left,
                            PageWidth = 2000 // Prevent wrapping
                        };

                        var viewer = new ScrollViewer
                        {
                            HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
                            VerticalScrollBarVisibility = ScrollBarVisibility.Disabled,
                            Content = new RichTextBox
                            {
                                Document = doc,
                                Background = CodeBackground,
                                Foreground = CodeForeground,
                                BorderThickness = new Thickness(0),
                                IsReadOnly = true,
                                FontFamily = new FontFamily("Consolas")
                            }
                        };

                        blocks.Add(new BlockUIContainer(viewer));
                        codeParagraph = null;
                    }

                    continue;
                }

                // CODE BLOCK CONTENT
                if (inCodeBlock)
                {
                    AddHighlightedLine(codeParagraph, line, codeLanguage);
                    codeParagraph.Inlines.Add(new LineBreak());
                    continue;
                }

                // HEADINGS (# ## ### ####)
                var headingMatch = Regex.Match(trimmed, @"^(#{1,6})\s+(.*)");
                if (headingMatch.Success)
                {
                    int level = headingMatch.Groups[1].Value.Length;
                    string content = headingMatch.Groups[2].Value;

                    blocks.Add(new Paragraph(new Bold(new Run(content)))
                    {
                        FontSize = 20 - (level * 2),
                        Margin = new Thickness(4, 10, 4, 4)
                    });

                    continue;
                }

                // BULLET LIST
                if (trimmed.StartsWith("- "))
                {
                    blocks.Add(new Paragraph(new Run("• " + trimmed.Substring(2)))
                    {
                        Margin = new Thickness(20, 2, 2, 2)
                    });

                    continue;
                }

                // NORMAL TEXT
                var paragraph = new Paragraph
                {
                    Margin = new Thickness(4)
                };

                ParseInline(paragraph, trimmed);

                blocks.Add(paragraph);
            }

            return blocks;
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
    }
}