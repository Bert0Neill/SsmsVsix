namespace VSIX_AutoComplete_SSMS.Forms
{
    partial class frmShortcuts
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
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(frmShortcuts));
            Syncfusion.WinForms.DataGrid.GridTextColumn gridTextColumn1 = new Syncfusion.WinForms.DataGrid.GridTextColumn();
            Syncfusion.WinForms.DataGrid.GridTextColumn gridTextColumn2 = new Syncfusion.WinForms.DataGrid.GridTextColumn();
            Syncfusion.WinForms.DataGrid.GridButtonColumn gridButtonColumn1 = new Syncfusion.WinForms.DataGrid.GridButtonColumn();
            Syncfusion.WinForms.DataGrid.GridButtonColumn gridButtonColumn2 = new Syncfusion.WinForms.DataGrid.GridButtonColumn();
            Syncfusion.WinForms.DataGrid.GridButtonColumn gridButtonColumn3 = new Syncfusion.WinForms.DataGrid.GridButtonColumn();
            this.imageListShortcut = new System.Windows.Forms.ImageList(this.components);
            this.sfDataGridShortCuts = new Syncfusion.WinForms.DataGrid.SfDataGrid();
            this.shortcutStatusBar = new System.Windows.Forms.StatusStrip();
            this.toolStripStatusLabel = new System.Windows.Forms.ToolStripStatusLabel();
            this.btnAdd = new System.Windows.Forms.Button();
            this.tlTipExpression = new Syncfusion.Windows.Forms.SfToolTip(this.components);
            this.btnExport = new System.Windows.Forms.Button();
            this.toolTip1 = new System.Windows.Forms.ToolTip(this.components);
            ((System.ComponentModel.ISupportInitialize)(this.sfDataGridShortCuts)).BeginInit();
            this.shortcutStatusBar.SuspendLayout();
            this.SuspendLayout();
            // 
            // imageListShortcut
            // 
            this.imageListShortcut.ImageStream = ((System.Windows.Forms.ImageListStreamer)(resources.GetObject("imageListShortcut.ImageStream")));
            this.imageListShortcut.TransparentColor = System.Drawing.Color.Transparent;
            this.imageListShortcut.Images.SetKeyName(0, "Custom-Icon-Design-Pretty-Office-9-Comment-edit.16.png");
            this.imageListShortcut.Images.SetKeyName(1, "Double-J-Design-Origami-Colored-Pencil-Blue-document.16.png");
            this.imageListShortcut.Images.SetKeyName(2, "Double-J-Design-Ravenna-3d-Comments.16.png");
            this.imageListShortcut.Images.SetKeyName(3, "Dtafalonso-Modern-Xp-ModernXP-30-Filetype-Text.16.png");
            this.imageListShortcut.Images.SetKeyName(4, "emergency_health_healthcare.png");
            this.imageListShortcut.Images.SetKeyName(5, "Gartoon-Team-Gartoon-Categories-Applications-science.16.png");
            this.imageListShortcut.Images.SetKeyName(6, "Gartoon-Team-Gartoon-Categories-Preferences-autocompletion.16.png");
            this.imageListShortcut.Images.SetKeyName(7, "Icons8-Ios7-Science-Test-Tube.16.png");
            this.imageListShortcut.Images.SetKeyName(8, "Kyo-Tux-Soft-Chat.16.png");
            this.imageListShortcut.Images.SetKeyName(9, "Rafiqul-Hassan-Blogger-Settings-2.16.png");
            this.imageListShortcut.Images.SetKeyName(10, "Social-balloon-friends.16.png");
            this.imageListShortcut.Images.SetKeyName(11, "Thalita-Torres-Office-Documents.16.png");
            this.imageListShortcut.Images.SetKeyName(12, "51415_disk_guardar_save_icon (1).png");
            this.imageListShortcut.Images.SetKeyName(13, "285657_floppy_guardar_save_icon (1).png");
            this.imageListShortcut.Images.SetKeyName(14, "1041633_close_icon.png");
            this.imageListShortcut.Images.SetKeyName(15, "1398917_circle_close_cross_incorrect_invalid_icon.png");
            this.imageListShortcut.Images.SetKeyName(16, "2142693_arrow_close_door_exit_out_icon.png");
            this.imageListShortcut.Images.SetKeyName(17, "6172083_cancel_close_exit_out_icon.png");
            this.imageListShortcut.Images.SetKeyName(18, "51415_disk_guardar_save_icon.png");
            this.imageListShortcut.Images.SetKeyName(19, "285657_floppy_guardar_save_icon.png");
            this.imageListShortcut.Images.SetKeyName(20, "Fatcow-Farm-Fresh-Db-sql-server.ico");
            this.imageListShortcut.Images.SetKeyName(21, "7172879_floppy_disc_save_storage_disk_icon (1).png");
            this.imageListShortcut.Images.SetKeyName(22, "7172879_floppy_disc_save_storage_disk_icon.png");
            // 
            // sfDataGridShortCuts
            // 
            this.sfDataGridShortCuts.AccessibleName = "Table";
            this.sfDataGridShortCuts.AllowEditing = false;
            this.sfDataGridShortCuts.AllowGrouping = false;
            this.sfDataGridShortCuts.AllowResizingColumns = true;
            gridTextColumn1.AllowBlankFilters = false;
            gridTextColumn1.AllowEditing = false;
            gridTextColumn1.AllowFiltering = true;
            gridTextColumn1.AllowGrouping = false;
            gridTextColumn1.AllowResizing = true;
            gridTextColumn1.HeaderStyle.BackColor = System.Drawing.SystemColors.GradientActiveCaption;
            gridTextColumn1.HeaderStyle.Font.Bold = true;
            gridTextColumn1.HeaderStyle.Font.Facename = "Microsoft Sans Serif";
            gridTextColumn1.HeaderStyle.Font.Size = 10F;
            gridTextColumn1.HeaderText = global::SQL_Shortcuts.Resources.Resource.Grid_Header_Shortcut;
            gridTextColumn1.MappingName = "SqlShort";
            gridTextColumn1.MinimumWidth = 200D;
            gridTextColumn1.ValidationMode = Syncfusion.WinForms.DataGrid.Enums.GridValidationMode.InView;
            gridTextColumn2.AllowEditing = false;
            gridTextColumn2.AllowGrouping = false;
            gridTextColumn2.AllowMultiline = true;
            gridTextColumn2.AllowResizing = true;
            gridTextColumn2.AllowTextWrapping = true;
            gridTextColumn2.AutoSizeColumnsMode = Syncfusion.WinForms.DataGrid.Enums.AutoSizeColumnsMode.LastColumnFill;
            gridTextColumn2.HeaderStyle.BackColor = System.Drawing.SystemColors.GradientActiveCaption;
            gridTextColumn2.HeaderStyle.Font.Bold = true;
            gridTextColumn2.HeaderStyle.Font.Size = 10F;
            gridTextColumn2.HeaderText = global::SQL_Shortcuts.Resources.Resource.Grid_Header_Sql_Expression;
            gridTextColumn2.MappingName = "SqlComplete";
            gridTextColumn2.TextTrimming = System.Drawing.StringTrimming.None;
            gridTextColumn2.ValidationMode = Syncfusion.WinForms.DataGrid.Enums.GridValidationMode.InView;
            gridButtonColumn1.AllowDefaultButtonText = false;
            gridButtonColumn1.AllowEditing = false;
            gridButtonColumn1.AllowGrouping = false;
            gridButtonColumn1.AllowResizing = true;
            gridButtonColumn1.AllowSorting = false;
            gridButtonColumn1.ButtonSize = new System.Drawing.Size(0, 0);
            gridButtonColumn1.CellStyle.HorizontalAlignment = System.Windows.Forms.HorizontalAlignment.Center;
            gridButtonColumn1.DefaultButtonText = "";
            gridButtonColumn1.HeaderStyle.BackColor = System.Drawing.SystemColors.GradientActiveCaption;
            gridButtonColumn1.HeaderStyle.Font.Bold = true;
            gridButtonColumn1.HeaderStyle.Font.Size = 10F;
            gridButtonColumn1.HeaderText = global::SQL_Shortcuts.Resources.Resource.Grid_Header_Delete;
            gridButtonColumn1.Image = ((System.Drawing.Image)(resources.GetObject("gridButtonColumn1.Image")));
            gridButtonColumn1.ImageSize = new System.Drawing.Size(0, 0);
            gridButtonColumn1.MappingName = "colDelete";
            gridButtonColumn1.ValidationMode = Syncfusion.WinForms.DataGrid.Enums.GridValidationMode.InView;
            gridButtonColumn2.AllowDefaultButtonText = false;
            gridButtonColumn2.AllowEditing = false;
            gridButtonColumn2.AllowGrouping = false;
            gridButtonColumn2.AllowResizing = true;
            gridButtonColumn2.AllowSorting = false;
            gridButtonColumn2.ButtonSize = new System.Drawing.Size(0, 0);
            gridButtonColumn2.CellStyle.HorizontalAlignment = System.Windows.Forms.HorizontalAlignment.Center;
            gridButtonColumn2.DefaultButtonText = "";
            gridButtonColumn2.HeaderStyle.BackColor = System.Drawing.SystemColors.GradientActiveCaption;
            gridButtonColumn2.HeaderStyle.Font.Bold = true;
            gridButtonColumn2.HeaderStyle.Font.Size = 10F;
            gridButtonColumn2.HeaderText = global::SQL_Shortcuts.Resources.Resource.Grid_Header_Edit;
            gridButtonColumn2.Image = ((System.Drawing.Image)(resources.GetObject("gridButtonColumn2.Image")));
            gridButtonColumn2.ImageSize = new System.Drawing.Size(0, 0);
            gridButtonColumn2.MappingName = "colEdit";
            gridButtonColumn2.ShowToolTip = true;
            gridButtonColumn2.ValidationMode = Syncfusion.WinForms.DataGrid.Enums.GridValidationMode.InView;
            gridButtonColumn3.AllowDefaultButtonText = true;
            gridButtonColumn3.AllowEditing = false;
            gridButtonColumn3.AllowGrouping = false;
            gridButtonColumn3.AllowResizing = true;
            gridButtonColumn3.ButtonSize = new System.Drawing.Size(0, 0);
            gridButtonColumn3.CellStyle.HorizontalAlignment = System.Windows.Forms.HorizontalAlignment.Center;
            gridButtonColumn3.DefaultButtonText = global::SQL_Shortcuts.Resources.Resource.Button_Test;
            gridButtonColumn3.HeaderStyle.BackColor = System.Drawing.SystemColors.GradientActiveCaption;
            gridButtonColumn3.HeaderStyle.FilterIconColor = System.Drawing.Color.FromArgb(((int)(((byte)(29)))), ((int)(((byte)(29)))), ((int)(((byte)(29)))));
            gridButtonColumn3.HeaderStyle.Font.Bold = true;
            gridButtonColumn3.HeaderStyle.Font.Size = 10F;
            gridButtonColumn3.HeaderText = global::SQL_Shortcuts.Resources.Resource.Grid_Eval_Header;
            gridButtonColumn3.ImageSize = new System.Drawing.Size(0, 0);
            gridButtonColumn3.MappingName = "EvalExpression";
            gridButtonColumn3.ValidationMode = Syncfusion.WinForms.DataGrid.Enums.GridValidationMode.InView;
            this.sfDataGridShortCuts.Columns.Add(gridTextColumn1);
            this.sfDataGridShortCuts.Columns.Add(gridTextColumn2);
            this.sfDataGridShortCuts.Columns.Add(gridButtonColumn1);
            this.sfDataGridShortCuts.Columns.Add(gridButtonColumn2);
            this.sfDataGridShortCuts.Columns.Add(gridButtonColumn3);
            this.sfDataGridShortCuts.Location = new System.Drawing.Point(12, 12);
            this.sfDataGridShortCuts.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.sfDataGridShortCuts.Name = "sfDataGridShortCuts";
            this.sfDataGridShortCuts.PreviewRowHeight = 35;
            this.sfDataGridShortCuts.Size = new System.Drawing.Size(1315, 625);
            this.sfDataGridShortCuts.Style.Border3DStyle = System.Windows.Forms.Border3DStyle.Raised;
            this.sfDataGridShortCuts.Style.BorderColor = System.Drawing.Color.Silver;
            this.sfDataGridShortCuts.TabIndex = 3;
            this.sfDataGridShortCuts.ValidationMode = Syncfusion.WinForms.DataGrid.Enums.GridValidationMode.InView;
            // 
            // shortcutStatusBar
            // 
            this.shortcutStatusBar.ImageScalingSize = new System.Drawing.Size(20, 20);
            this.shortcutStatusBar.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripStatusLabel});
            this.shortcutStatusBar.Location = new System.Drawing.Point(0, 695);
            this.shortcutStatusBar.Name = "shortcutStatusBar";
            this.shortcutStatusBar.Padding = new System.Windows.Forms.Padding(1, 0, 13, 0);
            this.shortcutStatusBar.Size = new System.Drawing.Size(1343, 26);
            this.shortcutStatusBar.TabIndex = 4;
            this.shortcutStatusBar.Text = "statusStrip1";
            // 
            // toolStripStatusLabel
            // 
            this.toolStripStatusLabel.Name = "toolStripStatusLabel";
            this.toolStripStatusLabel.Size = new System.Drawing.Size(670, 20);
            this.toolStripStatusLabel.Text = global::SQL_Shortcuts.Resources.Resource.Status_Auto_Save_Enabled;
            // 
            // btnAdd
            // 
            this.btnAdd.AutoSize = true;
            this.btnAdd.Image = ((System.Drawing.Image)(resources.GetObject("btnAdd.Image")));
            this.btnAdd.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.btnAdd.Location = new System.Drawing.Point(1139, 649);
            this.btnAdd.Margin = new System.Windows.Forms.Padding(4);
            this.btnAdd.Name = "btnAdd";
            this.btnAdd.Size = new System.Drawing.Size(188, 47);
            this.btnAdd.TabIndex = 5;
            this.btnAdd.Text = global::SQL_Shortcuts.Resources.Resource.Button_Add_New_Shortcut;
            this.tlTipExpression.SetToolTip(this.btnAdd, "Add a new SQL shortcut");
            this.btnAdd.UseVisualStyleBackColor = true;
            this.btnAdd.Click += new System.EventHandler(this.btnAdd_Click);
            // 
            // tlTipExpression
            // 
            this.tlTipExpression.AutoPopDelay = 3000;
            // 
            // btnExport
            // 
            this.btnExport.AutoSize = true;
            this.btnExport.Image = ((System.Drawing.Image)(resources.GetObject("btnExport.Image")));
            this.btnExport.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.btnExport.Location = new System.Drawing.Point(924, 649);
            this.btnExport.Margin = new System.Windows.Forms.Padding(4);
            this.btnExport.Name = "btnExport";
            this.btnExport.Size = new System.Drawing.Size(207, 47);
            this.btnExport.TabIndex = 6;
            this.btnExport.Text = global::SQL_Shortcuts.Resources.Resource.Button_Export_Shortcuts;
            this.btnExport.UseVisualStyleBackColor = true;
            this.btnExport.Click += new System.EventHandler(this.btnExport_Click);
            // 
            // frmShortcuts
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1343, 721);
            this.Controls.Add(this.btnExport);
            this.Controls.Add(this.btnAdd);
            this.Controls.Add(this.shortcutStatusBar);
            this.Controls.Add(this.sfDataGridShortCuts);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Margin = new System.Windows.Forms.Padding(4);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "frmShortcuts";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = global::SQL_Shortcuts.Resources.Resource.Form_Sql_Expression_Shortcuts;
            this.Load += new System.EventHandler(this.frmShortcuts_Load);
            ((System.ComponentModel.ISupportInitialize)(this.sfDataGridShortCuts)).EndInit();
            this.shortcutStatusBar.ResumeLayout(false);
            this.shortcutStatusBar.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private Syncfusion.WinForms.DataGrid.SfDataGrid sfDataGridShortCuts;
        private System.Windows.Forms.ImageList imageListShortcut;
        private System.Windows.Forms.StatusStrip shortcutStatusBar;
        private System.Windows.Forms.ToolStripStatusLabel toolStripStatusLabel;
        private System.Windows.Forms.Button btnAdd;
        private Syncfusion.Windows.Forms.SfToolTip tlTipExpression;
        private System.Windows.Forms.Button btnExport;
        private System.Windows.Forms.ToolTip toolTip1;
    }
}