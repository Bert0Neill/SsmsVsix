using Newtonsoft.Json;
using SSMS_AI_Buddy.Models;
using SSMS_AI_Buddy.Services;
using SSMS_AI_Buddy.Services.Helper;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using static Microsoft.VisualStudio.Debugger.DkmThread;

namespace SSMS_AI_Buddy.Components
{
    public partial class PromptWindowControl : UserControl
    {
        private readonly string _placeholderTextPrompt = "Enter AI prompt...";
        private AIProperties _aiProperties;
        private readonly RichTextBoxParagraphGenerator _richTextBoxParagraphGenerator;
        private StringBuilder _responseBuffer = new StringBuilder();

        public PromptWindowControl()
        {
            InitializeComponent();
            Unloaded += PromptWindowControl_Unloaded;
            rtbResults.Document.Blocks.Clear();
            SetPlaceholderIfNeeded();
            _richTextBoxParagraphGenerator = new RichTextBoxParagraphGenerator();
        }

        private void PromptWindowControl_Unloaded(object sender, RoutedEventArgs e)
        {
            // Clear existing text
            rtbPrompt.Document.Blocks.Clear();

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

                    //// debug - extract text content from blocks
                    //var texts = blocks
                    //    .OfType<Paragraph>()
                    //    .Select(p => new TextRange(
                    //        p.ContentStart,
                    //        p.ContentEnd
                    //    ).Text)
                    //    .ToList();

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
                await RetrieveAIResponseAsync(prompt, chunk => _responseBuffer.Append(chunk));
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
        #endregion

        #region HTTP Streaming
        private async Task RetrieveAIResponseAsync(string prompt, Action<string> onResponseChunk)
        {
            using (HttpClient client = new HttpClient())
            {
                object requestBody = null;

                switch (_aiProperties.AIProvider)
                {
                    case AIProviderOptions.OllamaLocal:

                        requestBody = new
                        {
                            model = _aiProperties.PromptLLMName,
                            prompt = prompt,
                            stream = _aiProperties.IsPromptResponseStreaming
                        };

                        break;

                    case AIProviderOptions.NvidiaRemote:

                        requestBody = new
                        {
                            model = _aiProperties.PromptLLMName,
                            messages = new[]
                            {
                                new
                                {
                                    role = "user",
                                    content = prompt
                                }
                            },

                            temperature = _aiProperties.Temperature,
                            top_p = _aiProperties.Randomness,
                            max_tokens = _aiProperties.Token,
                            stream = _aiProperties.IsPromptResponseStreaming
                        };

                        client.DefaultRequestHeaders.Clear();
                        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _aiProperties.AIPromptKey);

                        break;

                    default:
                        throw new NotSupportedException(string.Format("Unsupported provider: {0}", _aiProperties.AIProvider));
                }

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

                            if (string.IsNullOrWhiteSpace(line))
                                continue;

                            try
                            {
                                if (_aiProperties.AIProvider == AIProviderOptions.OllamaLocal)
                                {
                                    // Ollama
                                    var jsonResponse = JsonConvert.DeserializeObject<AIHosterResponse>(line);
                                    jsonResponse.response = Regex.Replace(jsonResponse.response, @"<\/?think>", "").TrimStart('\n', '\r', ' ');
                                    onResponseChunk?.Invoke(jsonResponse.response);
                                }
                                else if (_aiProperties.AIProvider == AIProviderOptions.NvidiaRemote)
                                {
                                    // NVidia
                                    var jsonResponse = JsonConvert.DeserializeObject<NVidiaModelResponse>(line);
                                    var content = jsonResponse?.Choices?.FirstOrDefault()?.Message?.Content;

                                    if (!string.IsNullOrWhiteSpace(content))
                                    {                                        
                                        content = CleanResponse(content);
                                        onResponseChunk?.Invoke(content);
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                onResponseChunk?.Invoke($"Parse error: {ex.Message}");
                            }
                        }
                    }
                }
            }
        }

        private string CleanResponse(string content)
        {
            if (string.IsNullOrWhiteSpace(content))
                return content;

            return content
                .Replace("<think>", "")
                .Replace("</think>", "")
                .TrimStart('\n', '\r', ' ');
        }

        #endregion

        public void SetPromptText(string text)
        {
            // Clear existing text
            rtbPrompt.Document.Blocks.Clear();

            // Add a blank paragraph first
            var blankParagraph = new Paragraph();
            rtbPrompt.Document.Blocks.Add(blankParagraph);

            // Add the highlighted SQL below
            var sqlParagraph = new Paragraph();
            sqlParagraph.Inlines.Add(new Run(text));
            rtbPrompt.Document.Blocks.Add(sqlParagraph);

            // Move caret to start of the blank line
            rtbPrompt.CaretPosition = blankParagraph.ContentStart;

            rtbPrompt.Focus();
        }

        public void SetSettingsAI(AIProperties aiProperties)
        {
            _aiProperties = aiProperties;
        }
    }
}