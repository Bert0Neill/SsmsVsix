using AI_Buddy.Models;
using AI_Buddy.Services;
using System;
using System.IO;
using System.Windows.Forms;

namespace AI_Buddy.Forms
{
    public partial class PropertiesFrm : Form
    {
        AIProperties _aiProperties;
        FileService _fileService;

        public PropertiesFrm()
        {
            InitializeComponent();

            _fileService = new FileService();
            _aiProperties = new AIProperties();
        }

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

            this.propertiesAIPrompt.SelectedObject = _aiProperties;
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
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
