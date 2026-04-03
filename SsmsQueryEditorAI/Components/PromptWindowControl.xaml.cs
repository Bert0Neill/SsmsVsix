using Newtonsoft.Json;
using SSMS_AI_Buddy.Models;
using SSMS_AI_Buddy.Services;
using SSMS_AI_Buddy.Services.Helper;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace SSMS_AI_Buddy.Components
{
    public partial class PromptWindowControl : UserControl
    {
        private readonly string _placeholderTextPrompt = "Enter AI prompt...";
        private AIProperties _aiProperties;
        private readonly FileService _fileService;
        private readonly RichTextBoxParagraphGenerator _richTextBoxParagraphGenerator;
        private bool _isImagesAttached = false;
        private List<string> _imageFilename = new List<string>();
        private StringBuilder _responseBuffer = new StringBuilder();

        public PromptWindowControl()
        {
            InitializeComponent();
            Unloaded += PromptWindowControl_Unloaded;

            _fileService = new FileService();
            _aiProperties = new AIProperties();
            _richTextBoxParagraphGenerator = new RichTextBoxParagraphGenerator();

            string filePath = Path.Combine(Directory.GetCurrentDirectory(), _aiProperties.SettingsFilename);
            if (File.Exists(filePath))
            {
                _aiProperties = _fileService.LoadFromJson<AIProperties>(filePath);
            }

            rtbResults.Document.Blocks.Clear();
            SetPlaceholderIfNeeded();
        }

        private void PromptWindowControl_Unloaded(object sender, RoutedEventArgs e)
        {
            Mouse.OverrideCursor = null;
        }

        #region Placeholder Methods
        private void rtbPrompt_Loaded(object sender, RoutedEventArgs e) => SetPlaceholderIfNeeded();
        private void rtbPrompt_GotFocus(object sender, RoutedEventArgs e)
        {
            if (IsPlaceholderActive())
            {
                rtbPrompt.Document.Blocks.Clear();
                rtbPrompt.Foreground = Brushes.Black; // Ensure typing is black
            }
        }
        private void rtbPrompt_LostFocus(object sender, RoutedEventArgs e) => SetPlaceholderIfNeeded();
        private void SetPlaceholderIfNeeded()
        {
            var text = new TextRange(rtbPrompt.Document.ContentStart, rtbPrompt.Document.ContentEnd).Text.Trim();
            if (string.IsNullOrEmpty(text))
            {
                rtbPrompt.Document.Blocks.Clear();
                rtbPrompt.Foreground = Brushes.Gray;
                rtbPrompt.Document.Blocks.Add(new Paragraph(new Run(_placeholderTextPrompt)));
            }
        }
        private bool IsPlaceholderActive()
        {
            var text = new TextRange(rtbPrompt.Document.ContentStart, rtbPrompt.Document.ContentEnd).Text.Trim();
            return string.Equals(text, _placeholderTextPrompt, StringComparison.OrdinalIgnoreCase);
        }
        #endregion

        #region Result Append

        public void AppendResult(string text, bool isError = false)
        {
            rtbResults.Dispatcher.Invoke(() =>
            {
                if (isError)
                {
                    var errorParagraph = new Paragraph();
                    errorParagraph.Foreground = Brushes.OrangeRed;
                    errorParagraph.Inlines.Add(new Bold(new Run("Error: ")));
                    errorParagraph.Inlines.Add(new Run(text));
                    rtbResults.Document.Blocks.Add(errorParagraph);
                }
                else
                {
                    var blocks = MarkdownRenderer.Parse(text); // Parse markdown including code blocks
                    foreach (var block in blocks)
                    {
                        rtbResults.Document.Blocks.Add(block);
                    }
                }

                rtbResults.CaretPosition = rtbResults.Document.ContentEnd;
                rtbResults.ScrollToEnd();
            });
        }

        internal void UpdatePromptResponse(string value) => AppendResult(value);
        internal void UpdateResultPrompt(Paragraph[] value)
        {
            foreach (var item in value)
                rtbResults.Document.Blocks.Add(item);
            rtbResults.CaretPosition = rtbResults.Document.ContentEnd;
            rtbResults.ScrollToEnd();
        }
        #endregion

        #region Submit
        private async void SubmitClickAsync(object sender, RoutedEventArgs e)
        {
            try
            {
                Mouse.OverrideCursor = Cursors.Wait;
                _isImagesAttached = false;
                _imageFilename = new List<string>();
                _responseBuffer.Clear();

                // disable submit button to prevent multiple clicks
                btnSubmit.IsEnabled = false;

                var prompt = ExtractRichTextBoxContent();
                if (string.IsNullOrWhiteSpace(prompt) || prompt == _placeholderTextPrompt)
                    return;

                // Add the submitted prompt to the results
                rtbResults.Document.Blocks.Add(
                    _richTextBoxParagraphGenerator.GenerateSubmitPromptParagraph(
                        prompt,
                        _richTextBoxParagraphGenerator.IsRichTextBoxEmpty(rtbResults)
                    )
                );

                // Clear the input box and reset placeholder
                rtbPrompt.Document.Blocks.Clear();
                SetPlaceholderIfNeeded();

                // Get AI response
                await GetOllamaResponseStreamAsync(prompt, chunk => _responseBuffer.Append(chunk));
                AppendResult(_responseBuffer.ToString());
                _responseBuffer.Clear();
                btnSubmit.IsEnabled = true;
            }
            catch (Exception ex)
            {
                btnSubmit.IsEnabled = true;
                AppendResult(ex.Message, isError: true);
            }
            finally
            {
               
                Mouse.OverrideCursor = null;
            }
        }
        #endregion

        #region RichTextBox Helpers
        private void rtbPrompt_Pasting(object sender, DataObjectPastingEventArgs e)
        {
            if (e.DataObject.GetDataPresent(System.Windows.Forms.DataFormats.Bitmap))
            {
                e.CancelCommand();
                var bitmapSource = Clipboard.GetImage();
                if (bitmapSource != null)
                {
                    var image = new System.Windows.Controls.Image { Source = bitmapSource, Width = 100, Height = 100, Stretch = Stretch.Uniform };
                    var container = new InlineUIContainer(image);
                    Paragraph currentParagraph = rtbPrompt.CaretPosition.Paragraph ?? new Paragraph();
                    currentParagraph.Inlines.Add(container);
                    if (rtbPrompt.CaretPosition.Paragraph == null)
                        rtbPrompt.Document.Blocks.Add(currentParagraph);
                    rtbPrompt.Document.Blocks.Add(new Paragraph());
                    rtbPrompt.CaretPosition = rtbPrompt.Document.ContentEnd;
                }
            }
        }

        private string ExtractRichTextBoxContent()
        {
            StringBuilder extractedText = new StringBuilder();
            List<BitmapSource> images = new List<BitmapSource>();

            TextRange range;

            if (!rtbPrompt.Selection.IsEmpty)
            {
                range = new TextRange(rtbPrompt.Selection.Start, rtbPrompt.Selection.End);
            }
            else
            {
                range = new TextRange(rtbPrompt.Document.ContentStart, rtbPrompt.Document.ContentEnd);
            }

            extractedText.Append(range.Text);

            return extractedText.ToString();
        }

        //private string ExtractRichTextBoxContent()
        //{
        //    StringBuilder extractedText = new StringBuilder();
        //    List<BitmapSource> images = new List<BitmapSource>();

        //    foreach (Block block in rtbPrompt.Document.Blocks)
        //    {
        //        if (block is Paragraph paragraph)
        //        {
        //            foreach (Inline inline in paragraph.Inlines)
        //            {
        //                if (inline is Run run)
        //                    extractedText.Append(run.Text);
        //                else if (inline is InlineUIContainer container && container.Child is System.Windows.Controls.Image image)
        //                {
        //                    BitmapSource bitmapSource = image.Source as BitmapSource;
        //                    if (bitmapSource != null)
        //                    {
        //                        _isImagesAttached = true;
        //                        images.Add(bitmapSource);
        //                        extractedText.Append(" (Image attached) ");
        //                        BitmapSource resized = ResizeBitmapSource(bitmapSource, 100, 100);
        //                        Clipboard.SetImage(resized);
        //                        rtbResults.Paste();
        //                    }
        //                }
        //            }
        //        }
        //    }

        //    int count = 1;
        //    foreach (var img in images)
        //    {
        //        _imageFilename.Add($"ExtractedImage_{count}.jpeg");
        //        SaveImage(img, $"ExtractedImage_{count}.jpeg");
        //        count++;
        //    }

        //    return extractedText.ToString();
        //}

        private BitmapSource ResizeBitmapSource(BitmapSource source, int width, int height)
        {
            var scaleX = (double)width / source.PixelWidth;
            var scaleY = (double)height / source.PixelHeight;
            var transform = new TransformedBitmap(source, new ScaleTransform(scaleX, scaleY));
            return transform;
        }

        private void SaveImage(BitmapSource image, string filePath)
        {
            using (FileStream stream = new FileStream(filePath, FileMode.Create))
            {
                PngBitmapEncoder encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(image));
                encoder.Save(stream);
            }
        }
        #endregion

        #region HTTP Streaming
        private async Task GetOllamaResponseStreamAsync(string prompt, Action<string> onResponseChunk)
        {
            using (HttpClient client = new HttpClient())
            {
                var requestBody = new
                {
                    model = _aiProperties.PromptLLMName,
                    prompt = prompt,
                    stream = _aiProperties.IsPromptResponseStreaming
                };

                client.DefaultRequestHeaders.Clear();
                client.DefaultRequestHeaders.Add("Authorization", $"Bearer {_aiProperties.AIPromptKey}");

                string jsonContent = JsonConvert.SerializeObject(requestBody);
                HttpContent httpContent = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                using (var request = new HttpRequestMessage(HttpMethod.Post, _aiProperties.AIPromptURL) { Content = httpContent })
                using (var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead))
                {
                    if (!response.IsSuccessStatusCode)
                    {
                        onResponseChunk?.Invoke($"Error: {response.StatusCode}");
                        return;
                    }

                    using (var stream = await response.Content.ReadAsStreamAsync())
                    using (var reader = new StreamReader(stream))
                    {
                        while (!reader.EndOfStream)
                        {
                            var line = await reader.ReadLineAsync();
                            if (!string.IsNullOrWhiteSpace(line))
                            {
                                var jsonResponse = JsonConvert.DeserializeObject<AIHosterResponse>(line);
                                jsonResponse.response = Regex.Replace(jsonResponse.response, @"<\/?think>", "").TrimStart('\n', '\r', ' ');
                                onResponseChunk?.Invoke(jsonResponse.response);
                            }
                        }
                    }
                }
            }
        }

        #endregion

        public void SetPromptText(string text)
        {
            // Clear existing text
            rtbPrompt.Document.Blocks.Clear();

            if (!string.IsNullOrWhiteSpace(text))
            {
                // Add a blank paragraph first
                var blankParagraph = new Paragraph();
                rtbPrompt.Document.Blocks.Add(blankParagraph);

                // Add the highlighted SQL below
                var sqlParagraph = new Paragraph();
                sqlParagraph.Inlines.Add(new Run(text));
                rtbPrompt.Document.Blocks.Add(sqlParagraph);

                // Move caret to start of the blank line
                rtbPrompt.CaretPosition = blankParagraph.ContentStart;
            }
            else
            {
                // If no text, just keep placeholder behavior
                SetPlaceholderIfNeeded();
            }

            rtbPrompt.Focus();
        }
    }
}