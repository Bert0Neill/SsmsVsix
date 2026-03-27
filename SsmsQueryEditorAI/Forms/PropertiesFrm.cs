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
            // read file settings and populate settings class, if no file display defaults
            string filePath = Path.Combine(Directory.GetCurrentDirectory(), _aiProperties.SettingsFilename);

            if (File.Exists(filePath))
            {
                _aiProperties = _fileService.LoadFromJson<AIProperties>(filePath);
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
            string filePath = Path.Combine(Directory.GetCurrentDirectory(), _aiProperties.SettingsFilename);
            _fileService.SaveToJson(_aiProperties, filePath);

            MessageBox.Show("File has been saved successfully.", "Save Successful", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }
}
