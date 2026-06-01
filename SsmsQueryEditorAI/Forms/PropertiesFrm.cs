using SSMS_AI_Buddy.Models;
using SSMS_AI_Buddy.Services;
using System;
using System.IO;
using System.Reflection;
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
            try
            {
                _aiProperties.DateLastModified = DateTime.Now;

                string resetFilePath = SettingsPathService.GetSettingsFilePath(_aiProperties.ResetFilename);
                string settingsFilePath = SettingsPathService.GetSettingsFilePath(_aiProperties.SettingsFilename);
                this.btnReset.Enabled = File.Exists(resetFilePath);

                if (!File.Exists(settingsFilePath))
                {
                    SettingsPathService.TrySeedFromExtension(_aiProperties.SettingsFilename);
                }

                if (!File.Exists(resetFilePath))
                {
                    SettingsPathService.TrySeedFromExtension(_aiProperties.ResetFilename);
                }

                if (File.Exists(settingsFilePath))
                {
                    _aiProperties = _fileService.LoadFromJson<AIProperties>(settingsFilePath);
                }
                else
                {
                    _fileService.SaveToJson(_aiProperties, settingsFilePath);
                }

                this.propertiesAIPrompt.SelectedObject = _aiProperties;
                SetPropertyGridLabelWidth(propertiesAIPrompt, 200);
            }
            catch (Exception ex)
            {
                LoggerService.Error($"PropertiesFrm_Load failed: {ex}");
                MessageBox.Show($"Could not load AI settings: {ex.Message}",
                    "AI Settings Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            //ApplyProviderSelectionToProperties();
            _aiProperties.DateLastModified = DateTime.Now;

            string resetFilePath = SettingsPathService.GetSettingsFilePath(_aiProperties.ResetFilename);
            string settingsFilePath = SettingsPathService.GetSettingsFilePath(_aiProperties.SettingsFilename);

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
            string resetFilePath = SettingsPathService.GetSettingsFilePath(_aiProperties.ResetFilename);
            string settingsFilePath = SettingsPathService.GetSettingsFilePath(_aiProperties.SettingsFilename);

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

        private static void SetPropertyGridLabelWidth(PropertyGrid propertyGrid, int width)
        {
            if (propertyGrid == null || propertyGrid.Controls.Count < 3)
            {
                return;
            }

            var gridView = propertyGrid.Controls[2];
            var moveSplitterMethod = gridView?.GetType().GetMethod("MoveSplitterTo", BindingFlags.Instance | BindingFlags.NonPublic);
            moveSplitterMethod?.Invoke(gridView, new object[] { width });
        }
    }
}
