namespace SSMS_AI_Buddy.Forms
{
    partial class PropertiesFrm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(PropertiesFrm));
            this.propertiesAIPrompt = new System.Windows.Forms.PropertyGrid();
            this.btnCancel = new System.Windows.Forms.Button();
            this.btnSave = new System.Windows.Forms.Button();
            this.btnReset = new System.Windows.Forms.Button();
            this.lblAIProvider = new System.Windows.Forms.Label();
            this.cboAIProvider = new System.Windows.Forms.ComboBox();
            this.SuspendLayout();
            // 
            // lblAIProvider
            // 
            this.lblAIProvider.AutoSize = true;
            this.lblAIProvider.Location = new System.Drawing.Point(11, 15);
            this.lblAIProvider.Name = "lblAIProvider";
            this.lblAIProvider.Size = new System.Drawing.Size(63, 13);
            this.lblAIProvider.TabIndex = 4;
            this.lblAIProvider.Text = "AI Provider:";
            // 
            // cboAIProvider
            // 
            this.cboAIProvider.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cboAIProvider.FormattingEnabled = true;
            this.cboAIProvider.Items.AddRange(new object[] {
            "Nvidia (Remote)",
            "Ollama (Locally)"});
            this.cboAIProvider.Location = new System.Drawing.Point(80, 12);
            this.cboAIProvider.Name = "cboAIProvider";
            this.cboAIProvider.Size = new System.Drawing.Size(368, 21);
            this.cboAIProvider.TabIndex = 5;
            //this.cboAIProvider.SelectedIndexChanged += new System.EventHandler(this.cboAIProvider_SelectedIndexChanged);
            // 
            // propertiesAIPrompt
            // 
            this.propertiesAIPrompt.Location = new System.Drawing.Point(4, 40);
            this.propertiesAIPrompt.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.propertiesAIPrompt.Name = "propertiesAIPrompt";
            this.propertiesAIPrompt.Size = new System.Drawing.Size(444, 293);
            this.propertiesAIPrompt.TabIndex = 0;
            // 
            // btnCancel
            // 
            this.btnCancel.Image = ((System.Drawing.Image)(resources.GetObject("btnCancel.Image")));
            this.btnCancel.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.btnCancel.Location = new System.Drawing.Point(348, 337);
            this.btnCancel.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(100, 31);
            this.btnCancel.TabIndex = 1;
            this.btnCancel.Text = "Cancel";
            this.btnCancel.UseVisualStyleBackColor = true;
            this.btnCancel.Click += new System.EventHandler(this.btnCancel_Click);
            // 
            // btnSave
            // 
            this.btnSave.Image = ((System.Drawing.Image)(resources.GetObject("btnSave.Image")));
            this.btnSave.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.btnSave.Location = new System.Drawing.Point(244, 336);
            this.btnSave.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.btnSave.Name = "btnSave";
            this.btnSave.Size = new System.Drawing.Size(100, 31);
            this.btnSave.TabIndex = 2;
            this.btnSave.Text = "Save";
            this.btnSave.UseVisualStyleBackColor = true;
            this.btnSave.Click += new System.EventHandler(this.btnSave_Click);
            // 
            // btnReset
            // 
            this.btnReset.Image = ((System.Drawing.Image)(resources.GetObject("btnReset.Image")));
            this.btnReset.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.btnReset.Location = new System.Drawing.Point(13, 339);
            this.btnReset.Name = "btnReset";
            this.btnReset.Size = new System.Drawing.Size(100, 31);
            this.btnReset.TabIndex = 3;
            this.btnReset.Text = "Reset";
            this.btnReset.UseVisualStyleBackColor = true;
            this.btnReset.Click += new System.EventHandler(this.btnReset_Click);
            // 
            // PropertiesFrm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(459, 382);
            this.Controls.Add(this.cboAIProvider);
            this.Controls.Add(this.lblAIProvider);
            this.Controls.Add(this.btnReset);
            this.Controls.Add(this.btnSave);
            this.Controls.Add(this.btnCancel);
            this.Controls.Add(this.propertiesAIPrompt);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "PropertiesFrm";
            this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Hide;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "AI Settings";
            this.Load += new System.EventHandler(this.PropertiesFrm_Load);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.PropertyGrid propertiesAIPrompt;
        private System.Windows.Forms.Button btnCancel;
        private System.Windows.Forms.Button btnSave;
        private System.Windows.Forms.Button btnReset;
        private System.Windows.Forms.Label lblAIProvider;
        private System.Windows.Forms.ComboBox cboAIProvider;
    }
}