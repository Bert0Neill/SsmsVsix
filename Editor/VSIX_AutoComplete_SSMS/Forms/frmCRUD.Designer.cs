namespace VSIX_AutoComplete_SSMS.Forms
{
    partial class frmCRUD
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
            if (disposing)
            {
                // Unsubscribe from events to avoid memory leaks
                txtSqlComplete.TextChanged -= UpdateButtonState;
                txtSqlShortcut.TextChanged -= UpdateButtonState;

                // Clear error providers
                errorProviderCrud?.Clear();

                // Dispose components container (auto-generated)
                if (components != null)
                {
                    components.Dispose();
                }
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
            Syncfusion.Windows.Forms.Tools.SplitButtonRenderer splitButtonRenderer3 = new Syncfusion.Windows.Forms.Tools.SplitButtonRenderer();
            Syncfusion.Windows.Forms.BannerTextInfo bannerTextInfo3 = new Syncfusion.Windows.Forms.BannerTextInfo();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(frmCRUD));
            this.btnSpltDates = new Syncfusion.Windows.Forms.Tools.SplitButton();
            this.tlTipDateTimeNow = new Syncfusion.Windows.Forms.Tools.toolstripitem();
            this.tlTipDateTimeUtcNow = new Syncfusion.Windows.Forms.Tools.toolstripitem();
            this.tlTipAddDay = new Syncfusion.Windows.Forms.Tools.toolstripitem();
            this.tlTipAddMonth = new Syncfusion.Windows.Forms.Tools.toolstripitem();
            this.tlTipAddYear = new Syncfusion.Windows.Forms.Tools.toolstripitem();
            this.tlTipToStringFormat = new Syncfusion.Windows.Forms.Tools.toolstripitem();
            this.tlTipDayOfWeek = new Syncfusion.Windows.Forms.Tools.toolstripitem();
            this.tlTipDayOfYear = new Syncfusion.Windows.Forms.Tools.toolstripitem();
            this.tlTipMidnight = new Syncfusion.Windows.Forms.Tools.toolstripitem();
            this.tpTipTimeOfDay = new Syncfusion.Windows.Forms.Tools.toolstripitem();
            this.tlTipTicks = new Syncfusion.Windows.Forms.Tools.toolstripitem();
            this.tlTipAdd10Minutes = new Syncfusion.Windows.Forms.Tools.toolstripitem();
            this.tlTipShortDate = new Syncfusion.Windows.Forms.Tools.toolstripitem();
            this.tpTipShortTime = new Syncfusion.Windows.Forms.Tools.toolstripitem();
            this.tlTipCastGetdate = new Syncfusion.Windows.Forms.Tools.toolstripitem();
            this.tlTipLastWeek = new Syncfusion.Windows.Forms.Tools.toolstripitem();
            this.lblSqlShortcut = new Syncfusion.Windows.Forms.Tools.AutoLabel();
            this.txtSqlShortcut = new Syncfusion.Windows.Forms.Tools.TextBoxExt();
            this.lblSqlComplete = new Syncfusion.Windows.Forms.Tools.AutoLabel();
            this.txtSqlComplete = new Syncfusion.Windows.Forms.Tools.TextBoxExt();
            this.imgLstCRUD = new System.Windows.Forms.ImageList(this.components);
            this.btnInsTable = new System.Windows.Forms.Button();
            this.bnrTxtPrvShortcut = new Syncfusion.Windows.Forms.BannerTextProvider(this.components);
            this.tlTipExpression = new Syncfusion.Windows.Forms.SfToolTip(this.components);
            this.stsStripCrud = new System.Windows.Forms.StatusStrip();
            this.toolStripStatusLabelCrud = new System.Windows.Forms.ToolStripStatusLabel();
            this.btnTestSqlStatement = new System.Windows.Forms.Button();
            this.btnSaveSqlStatement = new System.Windows.Forms.Button();
            this.errorProviderCrud = new System.Windows.Forms.ErrorProvider(this.components);
            ((System.ComponentModel.ISupportInitialize)(this.txtSqlShortcut)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.txtSqlComplete)).BeginInit();
            this.stsStripCrud.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.errorProviderCrud)).BeginInit();
            this.SuspendLayout();
            // 
            // btnSpltDates
            // 
            this.btnSpltDates.BeforeTouchSize = new System.Drawing.Size(212, 42);
            this.btnSpltDates.DropDownItems.Add(this.tlTipDateTimeNow);
            this.btnSpltDates.DropDownItems.Add(this.tlTipDateTimeUtcNow);
            this.btnSpltDates.DropDownItems.Add(this.tlTipAddDay);
            this.btnSpltDates.DropDownItems.Add(this.tlTipAddMonth);
            this.btnSpltDates.DropDownItems.Add(this.tlTipAddYear);
            this.btnSpltDates.DropDownItems.Add(this.tlTipToStringFormat);
            this.btnSpltDates.DropDownItems.Add(this.tlTipDayOfWeek);
            this.btnSpltDates.DropDownItems.Add(this.tlTipDayOfYear);
            this.btnSpltDates.DropDownItems.Add(this.tlTipMidnight);
            this.btnSpltDates.DropDownItems.Add(this.tpTipTimeOfDay);
            this.btnSpltDates.DropDownItems.Add(this.tlTipTicks);
            this.btnSpltDates.DropDownItems.Add(this.tlTipAdd10Minutes);
            this.btnSpltDates.DropDownItems.Add(this.tlTipShortDate);
            this.btnSpltDates.DropDownItems.Add(this.tpTipShortTime);
            this.btnSpltDates.DropDownItems.Add(this.tlTipCastGetdate);
            this.btnSpltDates.DropDownItems.Add(this.tlTipLastWeek);
            this.btnSpltDates.DropDownPosition = Syncfusion.Windows.Forms.Tools.Position.Top;
            this.btnSpltDates.Font = new System.Drawing.Font("Microsoft Sans Serif", 8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnSpltDates.ForeColor = System.Drawing.Color.Black;
            this.btnSpltDates.Location = new System.Drawing.Point(800, 100);
            this.btnSpltDates.Margin = new System.Windows.Forms.Padding(4);
            this.btnSpltDates.MinimumSize = new System.Drawing.Size(100, 28);
            this.btnSpltDates.Name = "btnSpltDates";
            splitButtonRenderer3.SplitButton = this.btnSpltDates;
            this.btnSpltDates.Renderer = splitButtonRenderer3;
            this.btnSpltDates.ShowDropDownOnButtonClick = false;
            this.btnSpltDates.Size = new System.Drawing.Size(212, 42);
            this.btnSpltDates.TabIndex = 7;
            this.btnSpltDates.Text = global::SQL_Shortcuts.Resources.Resource.Button_Insert_Date_Expression;
            this.tlTipExpression.SetToolTip(this.btnSpltDates, "Expression will be evaluated when rendered");
            this.btnSpltDates.DropDowItemClicked += new System.Windows.Forms.ToolStripItemClickedEventHandler(this.btnSpltDates_DropDowItemClicked);
            // 
            // tlTipDateTimeNow
            // 
            this.tlTipDateTimeNow.Name = "tlTipDateTimeNow";
            this.tlTipDateTimeNow.Size = new System.Drawing.Size(32, 19);
            this.tlTipDateTimeNow.Text = "\'{DateTime.Now}\'";
            this.tlTipDateTimeNow.ToolTipText = "Date and time now";
            // 
            // tlTipDateTimeUtcNow
            // 
            this.tlTipDateTimeUtcNow.Name = "tlTipDateTimeUtcNow";
            this.tlTipDateTimeUtcNow.Size = new System.Drawing.Size(32, 19);
            this.tlTipDateTimeUtcNow.Text = "\'{DateTime.UtcNow}\'";
            this.tlTipDateTimeUtcNow.ToolTipText = "UTC Time";
            // 
            // tlTipAddDay
            // 
            this.tlTipAddDay.Name = "tlTipAddDay";
            this.tlTipAddDay.Size = new System.Drawing.Size(32, 19);
            this.tlTipAddDay.Text = "\'{DateTime.Now.AddDays(1)}\'";
            this.tlTipAddDay.ToolTipText = "Add a day";
            // 
            // tlTipAddMonth
            // 
            this.tlTipAddMonth.Name = "tlTipAddMonth";
            this.tlTipAddMonth.Size = new System.Drawing.Size(32, 19);
            this.tlTipAddMonth.Text = "\'{DateTime.Now.AddMonths(1)}\'";
            this.tlTipAddMonth.ToolTipText = "Add a month";
            // 
            // tlTipAddYear
            // 
            this.tlTipAddYear.Name = "tlTipAddYear";
            this.tlTipAddYear.Size = new System.Drawing.Size(32, 19);
            this.tlTipAddYear.Text = "\'{DateTime.Now.AddYears(1)}\'";
            this.tlTipAddYear.ToolTipText = "Add a year";
            // 
            // tlTipToStringFormat
            // 
            this.tlTipToStringFormat.Name = "tlTipToStringFormat";
            this.tlTipToStringFormat.Size = new System.Drawing.Size(32, 19);
            this.tlTipToStringFormat.Text = "\'{DateTime.Now.ToString(\"yyyy-MM-dd HH:mm:ss\")}\'";
            this.tlTipToStringFormat.ToolTipText = "Format date and time to \"yyyy-MM-dd HH:mm:ss\"";
            // 
            // tlTipDayOfWeek
            // 
            this.tlTipDayOfWeek.Name = "tlTipDayOfWeek";
            this.tlTipDayOfWeek.Size = new System.Drawing.Size(32, 19);
            this.tlTipDayOfWeek.Text = "\'{DateTime.Now.DayOfWeek}\'";
            this.tlTipDayOfWeek.ToolTipText = "The day of the week";
            // 
            // tlTipDayOfYear
            // 
            this.tlTipDayOfYear.Name = "tlTipDayOfYear";
            this.tlTipDayOfYear.Size = new System.Drawing.Size(32, 19);
            this.tlTipDayOfYear.Text = "\'{DateTime.Now.DayOfYear}\'";
            this.tlTipDayOfYear.ToolTipText = "The day of the year";
            // 
            // tlTipMidnight
            // 
            this.tlTipMidnight.Name = "tlTipMidnight";
            this.tlTipMidnight.Size = new System.Drawing.Size(32, 19);
            this.tlTipMidnight.Text = "\'{DateTime.Now.Date}\'";
            this.tlTipMidnight.ToolTipText = "Today\'s date at midnight";
            // 
            // tpTipTimeOfDay
            // 
            this.tpTipTimeOfDay.Name = "tpTipTimeOfDay";
            this.tpTipTimeOfDay.Size = new System.Drawing.Size(32, 19);
            this.tpTipTimeOfDay.Text = "\'{DateTime.Now.TimeOfDay}\'";
            this.tpTipTimeOfDay.ToolTipText = "The time of the day";
            // 
            // tlTipTicks
            // 
            this.tlTipTicks.Name = "tlTipTicks";
            this.tlTipTicks.Size = new System.Drawing.Size(32, 19);
            this.tlTipTicks.Text = "\'{DateTime.Now.Ticks}\'";
            this.tlTipTicks.ToolTipText = "Date and time as ticks";
            // 
            // tlTipAdd10Minutes
            // 
            this.tlTipAdd10Minutes.Name = "tlTipAdd10Minutes";
            this.tlTipAdd10Minutes.Size = new System.Drawing.Size(32, 19);
            this.tlTipAdd10Minutes.Text = "\'{DateTime.Now.AddMinutes(10)}\'";
            this.tlTipAdd10Minutes.ToolTipText = "Add 10 minutes";
            // 
            // tlTipShortDate
            // 
            this.tlTipShortDate.Name = "tlTipShortDate";
            this.tlTipShortDate.Size = new System.Drawing.Size(32, 19);
            this.tlTipShortDate.Text = "\'{DateTime.Now.ToShortDateString() }\'";
            this.tlTipShortDate.ToolTipText = "To short date";
            // 
            // tpTipShortTime
            // 
            this.tpTipShortTime.Name = "tpTipShortTime";
            this.tpTipShortTime.Size = new System.Drawing.Size(32, 19);
            this.tpTipShortTime.Text = "\'{DateTime.Now.ToShortTimeString()}\'";
            this.tpTipShortTime.ToolTipText = "To short time";
            // 
            // tlTipCastGetdate
            // 
            this.tlTipCastGetdate.Name = "tlTipCastGetdate";
            this.tlTipCastGetdate.Size = new System.Drawing.Size(32, 19);
            this.tlTipCastGetdate.Text = " CAST(GETDATE() AS DATE)";
            this.tlTipCastGetdate.ToolTipText = "Cast today\'s datetime as date";
            // 
            // tlTipLastWeek
            // 
            this.tlTipLastWeek.Name = "tlTipLastWeek";
            this.tlTipLastWeek.Size = new System.Drawing.Size(32, 19);
            this.tlTipLastWeek.Tag = "";
            this.tlTipLastWeek.Text = "DATEADD(DAY, -7, GETDATE())";
            this.tlTipLastWeek.ToolTipText = "This date and time last week";
            // 
            // lblSqlShortcut
            // 
            this.lblSqlShortcut.DX = -85;
            this.lblSqlShortcut.DY = 3;
            this.lblSqlShortcut.LabeledControl = this.txtSqlShortcut;
            this.lblSqlShortcut.Location = new System.Drawing.Point(72, 21);
            this.lblSqlShortcut.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lblSqlShortcut.Name = "lblSqlShortcut";
            this.lblSqlShortcut.Size = new System.Drawing.Size(81, 16);
            this.lblSqlShortcut.TabIndex = 0;
            this.lblSqlShortcut.Text = global::SQL_Shortcuts.Resources.Resource.Label_Sql_Shortcut;
            // 
            // txtSqlShortcut
            // 
            bannerTextInfo3.Text = global::SQL_Shortcuts.Resources.Resource.Banner_Maxlength_10;
            bannerTextInfo3.Visible = true;
            this.bnrTxtPrvShortcut.SetBannerText(this.txtSqlShortcut, bannerTextInfo3);
            this.txtSqlShortcut.BeforeTouchSize = new System.Drawing.Size(635, 217);
            this.txtSqlShortcut.Location = new System.Drawing.Point(157, 18);
            this.txtSqlShortcut.Margin = new System.Windows.Forms.Padding(4);
            this.txtSqlShortcut.MaxLength = 10;
            this.txtSqlShortcut.Name = "txtSqlShortcut";
            this.txtSqlShortcut.Size = new System.Drawing.Size(211, 22);
            this.txtSqlShortcut.TabIndex = 3;
            this.txtSqlShortcut.KeyUp += new System.Windows.Forms.KeyEventHandler(this.txtSqlShortcut_KeyUp);
            // 
            // lblSqlComplete
            // 
            this.lblSqlComplete.DX = -97;
            this.lblSqlComplete.DY = 100;
            this.lblSqlComplete.LabeledControl = this.txtSqlComplete;
            this.lblSqlComplete.Location = new System.Drawing.Point(60, 149);
            this.lblSqlComplete.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lblSqlComplete.Name = "lblSqlComplete";
            this.lblSqlComplete.Size = new System.Drawing.Size(93, 16);
            this.lblSqlComplete.TabIndex = 2;
            this.lblSqlComplete.Text = global::SQL_Shortcuts.Resources.Resource.Label_Sql_Statement;
            // 
            // txtSqlComplete
            // 
            this.txtSqlComplete.AcceptsReturn = true;
            this.txtSqlComplete.BeforeTouchSize = new System.Drawing.Size(635, 217);
            this.txtSqlComplete.Location = new System.Drawing.Point(157, 49);
            this.txtSqlComplete.Margin = new System.Windows.Forms.Padding(4);
            this.txtSqlComplete.Multiline = true;
            this.txtSqlComplete.Name = "txtSqlComplete";
            this.txtSqlComplete.ScrollBars = System.Windows.Forms.ScrollBars.Both;
            this.txtSqlComplete.Size = new System.Drawing.Size(635, 217);
            this.txtSqlComplete.TabIndex = 1;
            // 
            // imgLstCRUD
            // 
            this.imgLstCRUD.ImageStream = ((System.Windows.Forms.ImageListStreamer)(resources.GetObject("imgLstCRUD.ImageStream")));
            this.imgLstCRUD.TransparentColor = System.Drawing.Color.Transparent;
            this.imgLstCRUD.Images.SetKeyName(0, "85307_edit_file_icon (1).png");
            this.imgLstCRUD.Images.SetKeyName(1, "85307_edit_file_icon.png");
            this.imgLstCRUD.Images.SetKeyName(2, "285657_floppy_guardar_save_icon (1).png");
            this.imgLstCRUD.Images.SetKeyName(3, "285657_floppy_guardar_save_icon.png");
            this.imgLstCRUD.Images.SetKeyName(4, "728987_experiment_chemistry_lab_laboratory_research_icon.png");
            this.imgLstCRUD.Images.SetKeyName(5, "1490886325-23-flask_82493.png");
            this.imgLstCRUD.Images.SetKeyName(6, "44832_test_icon (1).png");
            this.imgLstCRUD.Images.SetKeyName(7, "44844_aid_first_health_kit_medicine_icon.png");
            this.imgLstCRUD.Images.SetKeyName(8, "beaker.png");
            // 
            // btnInsTable
            // 
            this.btnInsTable.Font = new System.Drawing.Font("Microsoft Sans Serif", 8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnInsTable.Image = ((System.Drawing.Image)(resources.GetObject("btnInsTable.Image")));
            this.btnInsTable.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.btnInsTable.Location = new System.Drawing.Point(800, 50);
            this.btnInsTable.Margin = new System.Windows.Forms.Padding(4);
            this.btnInsTable.Name = "btnInsTable";
            this.btnInsTable.Size = new System.Drawing.Size(212, 42);
            this.btnInsTable.TabIndex = 6;
            this.btnInsTable.Text = global::SQL_Shortcuts.Resources.Resource.Button_Insert_Table_Tag;
            this.tlTipExpression.SetToolTip(this.btnInsTable, "<Table> will be replaced by selected table\\view");
            this.btnInsTable.UseVisualStyleBackColor = true;
            this.btnInsTable.Click += new System.EventHandler(this.btnInsTable_Click);
            // 
            // tlTipExpression
            // 
            this.tlTipExpression.AutoPopDelay = 3000;
            // 
            // stsStripCrud
            // 
            this.stsStripCrud.ImageScalingSize = new System.Drawing.Size(18, 18);
            this.stsStripCrud.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripStatusLabelCrud});
            this.stsStripCrud.Location = new System.Drawing.Point(0, 323);
            this.stsStripCrud.Name = "stsStripCrud";
            this.stsStripCrud.Padding = new System.Windows.Forms.Padding(1, 0, 19, 0);
            this.stsStripCrud.Size = new System.Drawing.Size(1052, 22);
            this.stsStripCrud.TabIndex = 8;
            // 
            // toolStripStatusLabelCrud
            // 
            this.toolStripStatusLabelCrud.Name = "toolStripStatusLabelCrud";
            this.toolStripStatusLabelCrud.Size = new System.Drawing.Size(0, 16);
            // 
            // btnTestSqlStatement
            // 
            this.btnTestSqlStatement.AutoSize = true;
            this.btnTestSqlStatement.Image = ((System.Drawing.Image)(resources.GetObject("btnTestSqlStatement.Image")));
            this.btnTestSqlStatement.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.btnTestSqlStatement.Location = new System.Drawing.Point(525, 277);
            this.btnTestSqlStatement.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.btnTestSqlStatement.Name = "btnTestSqlStatement";
            this.btnTestSqlStatement.Size = new System.Drawing.Size(137, 42);
            this.btnTestSqlStatement.TabIndex = 9;
            this.btnTestSqlStatement.Text = global::SQL_Shortcuts.Resources.Resource.Button_Test_Sql;
            this.btnTestSqlStatement.UseVisualStyleBackColor = true;
            this.btnTestSqlStatement.Click += new System.EventHandler(this.btnTestSqlStatement_Click);
            // 
            // btnSaveSqlStatement
            // 
            this.btnSaveSqlStatement.AutoEllipsis = true;
            this.btnSaveSqlStatement.Image = ((System.Drawing.Image)(resources.GetObject("btnSaveSqlStatement.Image")));
            this.btnSaveSqlStatement.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.btnSaveSqlStatement.Location = new System.Drawing.Point(668, 277);
            this.btnSaveSqlStatement.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.btnSaveSqlStatement.Name = "btnSaveSqlStatement";
            this.btnSaveSqlStatement.Size = new System.Drawing.Size(124, 42);
            this.btnSaveSqlStatement.TabIndex = 10;
            this.btnSaveSqlStatement.Text = global::SQL_Shortcuts.Resources.Resource.Button_Save;
            this.btnSaveSqlStatement.UseVisualStyleBackColor = true;
            this.btnSaveSqlStatement.Click += new System.EventHandler(this.btnSaveSqlStatement_Click);
            // 
            // errorProviderCrud
            // 
            this.errorProviderCrud.ContainerControl = this;
            // 
            // frmCRUD
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1052, 345);
            this.Controls.Add(this.btnSaveSqlStatement);
            this.Controls.Add(this.btnTestSqlStatement);
            this.Controls.Add(this.stsStripCrud);
            this.Controls.Add(this.btnSpltDates);
            this.Controls.Add(this.btnInsTable);
            this.Controls.Add(this.txtSqlComplete);
            this.Controls.Add(this.lblSqlComplete);
            this.Controls.Add(this.txtSqlShortcut);
            this.Controls.Add(this.lblSqlShortcut);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Margin = new System.Windows.Forms.Padding(4);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "frmCRUD";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "SQL Expression Editor";
            this.Load += new System.EventHandler(this.frmCRUD_Load);
            ((System.ComponentModel.ISupportInitialize)(this.txtSqlShortcut)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.txtSqlComplete)).EndInit();
            this.stsStripCrud.ResumeLayout(false);
            this.stsStripCrud.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.errorProviderCrud)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private Syncfusion.Windows.Forms.Tools.AutoLabel lblSqlShortcut;
        private Syncfusion.Windows.Forms.Tools.TextBoxExt txtSqlShortcut;
        private Syncfusion.Windows.Forms.Tools.AutoLabel lblSqlComplete;
        private Syncfusion.Windows.Forms.Tools.TextBoxExt txtSqlComplete;
        private System.Windows.Forms.ImageList imgLstCRUD;
        private System.Windows.Forms.Button btnInsTable;
        private Syncfusion.Windows.Forms.Tools.SplitButton btnSpltDates;
        private Syncfusion.Windows.Forms.Tools.toolstripitem tlTipDateTimeNow;
        private Syncfusion.Windows.Forms.Tools.toolstripitem tlTipDateTimeUtcNow;
        private Syncfusion.Windows.Forms.Tools.toolstripitem tlTipAddDay;
        private Syncfusion.Windows.Forms.Tools.toolstripitem tlTipAddMonth;
        private Syncfusion.Windows.Forms.Tools.toolstripitem tlTipAddYear;
        private Syncfusion.Windows.Forms.Tools.toolstripitem tlTipToStringFormat;
        private Syncfusion.Windows.Forms.Tools.toolstripitem tlTipDayOfWeek;
        private Syncfusion.Windows.Forms.Tools.toolstripitem tlTipDayOfYear;
        private Syncfusion.Windows.Forms.Tools.toolstripitem tlTipMidnight;
        private Syncfusion.Windows.Forms.Tools.toolstripitem tpTipTimeOfDay;
        private Syncfusion.Windows.Forms.Tools.toolstripitem tlTipTicks;
        private Syncfusion.Windows.Forms.Tools.toolstripitem tlTipAdd10Minutes;
        private Syncfusion.Windows.Forms.Tools.toolstripitem tlTipShortDate;
        private Syncfusion.Windows.Forms.Tools.toolstripitem tpTipShortTime;
        private Syncfusion.Windows.Forms.Tools.toolstripitem tlTipCastGetdate;
        private Syncfusion.Windows.Forms.Tools.toolstripitem tlTipLastWeek;
        private Syncfusion.Windows.Forms.BannerTextProvider bnrTxtPrvShortcut;
        private Syncfusion.Windows.Forms.SfToolTip tlTipExpression;
        private System.Windows.Forms.StatusStrip stsStripCrud;
        private System.Windows.Forms.ToolStripStatusLabel toolStripStatusLabelCrud;
        private System.Windows.Forms.Button btnTestSqlStatement;
        private System.Windows.Forms.Button btnSaveSqlStatement;
        private System.Windows.Forms.ErrorProvider errorProviderCrud;
    }
}