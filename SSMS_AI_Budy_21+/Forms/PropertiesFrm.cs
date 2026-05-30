using SSMS_AI_Buddy.Models;
using SSMS_AI_Buddy.Services;
using System;
using System.IO;
using System.Windows.Forms;

namespace SSMS_AI_Buddy.Forms
{
    public partial class PropertiesFrm : Form
    {
        AIProperties _aiProperties;
        FileService _fileService;
        bool _suppressProviderChange;

        public PropertiesFrm()
        {
            InitializeComponent();

            _fileService = new FileService();
            _aiProperties = new AIProperties();
        }

        private void BindAIProviderDropdown()
        {
            _suppressProviderChange = true;
            try
            {
                string provider = _aiProperties.AIProvider;
                int index = Array.IndexOf(AIProviderOptions.All, provider);
                cboAIProvider.SelectedIndex = index >= 0 ? index : 1;
            }
            finally
            {
                _suppressProviderChange = false;
            }
        }

        //private void ApplyProviderSelectionToProperties()
        //{
        //    if (cboAIProvider.SelectedItem == null)
        //        return;

        //    _aiProperties.AIProvider = cboAIProvider.SelectedItem.ToString();
        //    ApplyProviderDefaults(_aiProperties.AIProvider);
        //    propertiesAIPrompt.Refresh();
        //}

        //private void ApplyProviderDefaults(string provider)
        //{
        //    if (provider == AIProviderOptions.NvidiaRemote)
        //    {
        //        _aiProperties.AIPromptURL = "https://integrate.api.nvidia.com/v1/chat/completions";
        //        _aiProperties.PromptLLMName = "deepseek-ai/deepseek-r1";
        //    }
        //    else if (provider == AIProviderOptions.OllamaLocal)
        //    {
        //        _aiProperties.AIPromptURL = "http://localhost:11434/api/generate";
        //        _aiProperties.PromptLLMName = "deepseek-r1:1.5b";
        //    }
        //}

        //private void cboAIProvider_SelectedIndexChanged(object sender, EventArgs e)
        //{
        //    if (_suppressProviderChange)
        //        return;

        //    ApplyProviderSelectionToProperties();
        //}

        private void PropertiesFrm_Load(object sender, EventArgs e)
        {
            _aiProperties.DateLastModified = DateTime.Now;

            string basePath = AppDomain.CurrentDomain.BaseDirectory;
            string resetFilePath = Path.Combine(basePath, _aiProperties.ResetFilename);
            string settingsFilePath = Path.Combine(basePath, _aiProperties.SettingsFilename);
            this.btnReset.Enabled = File.Exists(resetFilePath); // enable reset based on file existing to reset to

            if (File.Exists(settingsFilePath))
            {
                _aiProperties = _fileService.LoadFromJson<AIProperties>(settingsFilePath);
            }

            // if no reset file exists, create based on existing props
            if (!File.Exists(settingsFilePath))
            {
                _fileService.SaveToJson(_aiProperties, resetFilePath);
            }

            BindAIProviderDropdown();
            this.propertiesAIPrompt.SelectedObject = _aiProperties;
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            //ApplyProviderSelectionToProperties();
            _aiProperties.DateLastModified = DateTime.Now;

            string basePath = AppDomain.CurrentDomain.BaseDirectory;
            string resetFilePath = Path.Combine(basePath, _aiProperties.ResetFilename);
            string settingsFilePath = Path.Combine(basePath, _aiProperties.SettingsFilename);

            try
            {
                // 1. Delete existing reset file, if current file exist
                if (File.Exists(settingsFilePath))
                {
                    File.Delete(resetFilePath);
                }

                // 2. Rename current settings file to reset file (if it exists)
                if (File.Exists(settingsFilePath))
                {
                    File.Move(settingsFilePath, resetFilePath);
                }

                // 3. Save new settings file
                _fileService.SaveToJson(_aiProperties, settingsFilePath);
                this.btnReset.Enabled = File.Exists(resetFilePath); // enable reset based on file existing to reset to

                MessageBox.Show("File has been saved successfully.",
                    "Save Successful",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving file: {ex.Message}",
                    "Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        private void btnReset_Click(object sender, EventArgs e)
        {
            string basePath = AppDomain.CurrentDomain.BaseDirectory;
            string resetFilePath = Path.Combine(basePath, _aiProperties.ResetFilename);
            string settingsFilePath = Path.Combine(basePath, _aiProperties.SettingsFilename);

            try
            {
                // 1. Delete current settings file (if it exists)
                if (File.Exists(settingsFilePath))
                {
                    File.Delete(settingsFilePath);
                }

                // 2. Rename reset file → settings file (restore)
                if (File.Exists(resetFilePath))
                {
                    File.Move(resetFilePath, settingsFilePath);
                }

                // 3. Delete reset file if it still exists (cleanup)
                if (File.Exists(resetFilePath))
                {
                    File.Delete(resetFilePath);
                }

                // reload settings
                if (File.Exists(settingsFilePath))
                {
                    _aiProperties = _fileService.LoadFromJson<AIProperties>(settingsFilePath);
                }

                BindAIProviderDropdown();
                this.propertiesAIPrompt.SelectedObject = _aiProperties;
                this.btnReset.Enabled = File.Exists(resetFilePath); // enable reset based on file existing to reset to

                MessageBox.Show("Settings have been reset successfully.",
                    "Reset Successful",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error resetting settings: {ex.Message}",
                    "Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }
    }
}
