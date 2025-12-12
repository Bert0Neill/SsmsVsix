using Microsoft.Toolkit.Uwp.Notifications;
using Syncfusion.WinForms.DataGrid;
using Syncfusion.WinForms.DataGrid.Events;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using VSIX_AutoComplete_SSMS.Classes;
using VSIX_AutoComplete_SSMS.Models;
using SQL_Shortcuts.Resources;
using VSIX_Demo;
using iTextSharp.text.pdf;
using iTextSharp.text;
using Document = iTextSharp.text.Document;

namespace VSIX_AutoComplete_SSMS.Forms
{
    public partial class frmShortcuts : Form
    {
        #region Class Variables
        private const int AUTO_HEIGHT = 24;
        private readonly InlineCSharpHelper _inlineCSharpHelper;
        private FileService _fileService;
        private string _filePath = string.Empty;
        private BindingList<SqlShortcuts> _workingCopy; // temporary working copy
        private RowAutoFitOptions _autoFitOptions = new RowAutoFitOptions();
        private int _autoHeight;
        #endregion

        public frmShortcuts()
        {
            try
            {
                System.Windows.Forms.Cursor.Current = System.Windows.Forms.Cursors.WaitCursor;

                InitializeComponent();

                string filename = SQL_Shortcuts.Properties.Settings.Default.ConfigFile;
                _inlineCSharpHelper = new InlineCSharpHelper();

                SimpleLogger.Info(Resource.Grid_Loading_Form);

                // create working copy using LINQ
                var sourceData = SQL_Shortcuts.Properties.Settings.Default.IsDemo
                    ? VSIX_DemoPackage._sqlShortcuts.Take(1)  // Only take first record in demo mode
                    : VSIX_DemoPackage._sqlShortcuts;         // Take all records in normal mode

                _workingCopy = new BindingList<SqlShortcuts>(
                    sourceData.Select(item => new SqlShortcuts
                    {
                        SqlShort = item.SqlShort,
                        SqlComplete = item.SqlComplete
                    }).ToList());

                _fileService = new FileService();
                _filePath = Path.Combine(Directory.GetCurrentDirectory(), filename);
            }
            catch (Exception ex)
            {
                SimpleLogger.Error($"{ex.Message}");
            }
            finally
            {
                System.Windows.Forms.Cursor.Current = System.Windows.Forms.Cursors.Default;
            }
        }

        private void frmShortcuts_Load(object sender, EventArgs e)
        {
            try
            {
                System.Windows.Forms.Cursor.Current = System.Windows.Forms.Cursors.WaitCursor;

                this.sfDataGridShortCuts.CellButtonClick += sfDataGridShortCuts_CellButtonClick;
                this.sfDataGridShortCuts.RecordDeleted += sfDataGridShortCuts_RecordDeleted;
                this.sfDataGridShortCuts.QueryRowHeight += sfDataGridShortCuts_QueryRowHeight;

                // Bind to our working copy instead of the main collection
                sfDataGridShortCuts.DataSource = _workingCopy;

                UpdateButtonState(null, null); // Initial button state

                toolStripStatusLabel.Text = string.Format(Resource.Grid_Loaded_Shortcuts, _workingCopy.Count);
            }
            catch (Exception ex)
            {
                SimpleLogger.Error($"{ex.Message}");
            }
            finally
            {
                System.Windows.Forms.Cursor.Current = System.Windows.Forms.Cursors.Default;
            }
        }

        private void SaveChanges()
        {
            try
            {
                System.Windows.Forms.Cursor.Current = System.Windows.Forms.Cursors.WaitCursor;

                // update the main collection with our working copy
                VSIX_DemoPackage._sqlShortcuts.Clear();

                foreach (var item in _workingCopy)
                {
                    VSIX_DemoPackage._sqlShortcuts.Add(new SqlShortcuts
                    {
                        SqlShort = item.SqlShort,
                        SqlComplete = item.SqlComplete
                    });
                }

                _fileService.SaveToJson(VSIX_DemoPackage._sqlShortcuts, _filePath);

                // abbreviated file path for clarity in popup
                string displayedPath = _filePath.Length > 40
                    ? "..." + _filePath.Substring(_filePath.Length - 40)
                    : _filePath;

                toolStripStatusLabel.Text = string.Format(Resource.Grid_Saved_Shortcuts, _workingCopy.Count());

            }
            catch (Exception ex)
            {
                SimpleLogger.Error($"{ex.Message}");
            }
            finally
            {
                System.Windows.Forms.Cursor.Current = System.Windows.Forms.Cursors.Default;
            }
        }

        private void sfDataGridShortCuts_CellButtonClick(object sender, CellButtonClickEventArgs e)
        {
            try
            {
                System.Windows.Forms.Cursor.Current = System.Windows.Forms.Cursors.WaitCursor;

                if (e.Column.MappingName == "colEdit")
                {
                    var rowData = ((Syncfusion.WinForms.DataGrid.DataRowBase)e.Record).RowData as SqlShortcuts;
                    frmCRUD frmCRUD = new frmCRUD(_workingCopy, rowData);
                    frmCRUD.ShowDialog();

                    if (frmCRUD.DialogResult == DialogResult.OK)
                    {
                        if (frmCRUD._selectedRecord != null)
                        {
                            rowData.SqlShort = frmCRUD._selectedRecord.SqlShort;
                            rowData.SqlComplete = frmCRUD._selectedRecord.SqlComplete;
                           
                            SaveChanges();
                        }

                        UpdateButtonState(null, null); // check button state

                        toolStripStatusLabel.Text = string.Format(Resource.Grid_Updated_Shortcut, rowData.SqlShort);
                    }
                }
                else if (e.Column.MappingName == "EvalExpression")
                {
                    var sqlShortcut = ((Syncfusion.WinForms.DataGrid.DataRowBase)e.Record).RowData as SqlShortcuts;
                    _inlineCSharpHelper.EvaluateSqlStatement(sqlShortcut);
                }
                else if (e.Column.MappingName == "colDelete")
                {
                    var rowData = ((Syncfusion.WinForms.DataGrid.DataRowBase)e.Record).RowData as SqlShortcuts;
                    int dgDeletingRowIndex = sfDataGridShortCuts.TableControl.ResolveToRecordIndex(sfDataGridShortCuts.CurrentCell.RowIndex);
                    sfDataGridShortCuts.View.RemoveAt(dgDeletingRowIndex); // Delete record by based on the row index 
                   
                    _workingCopy.Remove(rowData); // Remove from working copy
                    SaveChanges();

                    UpdateButtonState(null, null); // check button state

                    toolStripStatusLabel.Text = string.Format(Resource.Grid_Deleted_Shortcut, rowData.SqlShort);
                }
            }
            catch (Exception ex)
            {
                SimpleLogger.Error($"{ex.Message}");
            }
            finally
            {
                System.Windows.Forms.Cursor.Current = System.Windows.Forms.Cursors.Default;
            }
        }

        private void sfDataGridShortCuts_RecordDeleted(object sender, RecordDeletedEventArgs e)
        {
            SaveChanges();
            toolStripStatusLabel.Text = string.Format(Resource.Grid_Deleted_Success, e.Items[0].ToString());
        }

        private void sfDataGridShortCuts_QueryRowHeight(object sender, QueryRowHeightEventArgs e)
        {
            if (this.sfDataGridShortCuts.AutoSizeController.GetAutoRowHeight(e.RowIndex, _autoFitOptions, out _autoHeight))
            {
                if (_autoHeight > AUTO_HEIGHT)
                {
                    e.Height = _autoHeight;
                    e.Handled = true;
                }
            }
        }

        private void btnAdd_Click(object sender, EventArgs e)
        {
            var newData = new SqlShortcuts();
            frmCRUD frmCRUD = new frmCRUD(_workingCopy, newData);
            frmCRUD.ShowDialog();

            if (frmCRUD.DialogResult == DialogResult.OK)
            {
                try
                {
                    System.Windows.Forms.Cursor.Current = System.Windows.Forms.Cursors.WaitCursor;

                    if (frmCRUD._selectedRecord != null)
                    {
                        // add new record to working copy
                        _workingCopy.Add(frmCRUD._selectedRecord);
                        
                        UpdateButtonState(null, null); // check button state

                        toolStripStatusLabel.Text = string.Format(Resource.Grid_Added_Shortcut, frmCRUD._selectedRecord.SqlShort);
                        SaveChanges();
                    }
                }
                catch (Exception ex)
                {
                    SimpleLogger.Error($"{ex.Message}");
                }
                finally
                {
                    System.Windows.Forms.Cursor.Current = System.Windows.Forms.Cursors.Default;
                }
            }
        }

        private void btnExport_Click(object sender, EventArgs e)
        {
            if (this.sfDataGridShortCuts != null)
            {
                try
                {
                    using (SaveFileDialog saveDialog = new SaveFileDialog())
                    {
                        saveDialog.Title = "Export SQL Shortcuts as Pdf";
                        saveDialog.Filter = "Pdf Files (*.pdf)|*.pdf";
                        saveDialog.DefaultExt = "pdf";
                        saveDialog.AddExtension = true;
                        saveDialog.FileName = SQL_Shortcuts.Properties.Settings.Default.ExportFilename;

                        if (saveDialog.ShowDialog() == DialogResult.OK)
                        {
                            System.Windows.Forms.Cursor.Current = System.Windows.Forms.Cursors.WaitCursor;

                            string filePath = saveDialog.FileName;

                            // Create a document and set margins
                            Document document = new Document(PageSize.A4, 36, 36, 36, 36);
                            PdfWriter.GetInstance(document, new FileStream(filePath, FileMode.Create));
                            document.Open();

                            // Title
                            var titleFont = FontFactory.GetFont("Helvetica", "16", Font.Bold);
                            document.Add(new Paragraph("SQL Shortcuts", titleFont));
                            document.Add(new Paragraph("\n")); // empty line

                            // Table with 2 columns
                            PdfPTable table = new PdfPTable(2)
                            {
                                WidthPercentage = 100
                            };
                            table.SetWidths(new float[] { 1f, 3f });

                            // Header style
                            var headerFont = FontFactory.GetFont("Helvetica", "12", Font.Bold);
                            table.AddCell(new PdfPCell(new Phrase("SQL Shortcut", headerFont)) { BackgroundColor = BaseColor.LightGray });
                            table.AddCell(new PdfPCell(new Phrase("SQL Expression", headerFont)) { BackgroundColor = BaseColor.LightGray });

                            // Data rows
                            var bodyFont = FontFactory.GetFont("Helvetica", 11);
                            foreach (var shortcut in _workingCopy)
                            {
                                table.AddCell(new Phrase(shortcut.SqlShort ?? "", bodyFont));
                                table.AddCell(new Phrase(shortcut.SqlComplete ?? "", bodyFont));
                            }

                            document.Add(table);
                            document.Close();

                            // display popup
                            new ToastContentBuilder()
                                .AddVisualChild(new AdaptiveText()
                                {
                                    Text = "VSIX SQL Autocomplete",
                                    HintStyle = AdaptiveTextStyle.Title
                                })
                                .AddVisualChild(new AdaptiveText()
                                {
                                    Text = "Sql shortcuts exported to file:",
                                    HintStyle = AdaptiveTextStyle.Body
                                })
                                .AddVisualChild(new AdaptiveText()
                                {
                                    Text = filePath,
                                    HintStyle = AdaptiveTextStyle.CaptionSubtle
                                })
                                .Show();

                            if (File.Exists(filePath))
                            {
                                System.Diagnostics.Process.Start(new ProcessStartInfo
                                {
                                    FileName = filePath,
                                    UseShellExecute = true // Required for .NET Framework to use default app/browser
                                });
                            }

                            toolStripStatusLabel.Text = String.Format(Resource.Grid_Shortcuts_Exported_To, filePath);
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("An error occurred exporting shortcuts to PDF - " + ex.Message);
                }
                finally
                {
                    System.Windows.Forms.Cursor.Current = System.Windows.Forms.Cursors.Default;
                }
            }
        }

        private void UpdateButtonState(object sender, EventArgs e)
        {
            try
            {
                System.Windows.Forms.Cursor.Current = System.Windows.Forms.Cursors.WaitCursor;

                if (SQL_Shortcuts.Properties.Settings.Default.IsDemo && _workingCopy?.Count() >= 1)
                {
                    // Disable the button if in demo mode and more than 1 record
                    this.btnAdd.Enabled = false;
                    this.toolStripStatusLabel.Text = "Demo version: Only able to create 1 SQL shortcut";
                }
                else
                {
                    this.btnAdd.Enabled = true;
                    this.toolStripStatusLabel.Text = string.Empty;
                }
                
                this.btnExport.Enabled = _workingCopy.Any(); // enable\disable button based on number of shortcuts
            }
            catch (Exception ex)
            {
                SimpleLogger.Error($"{ex.Message}");
            }
            finally
            {
                System.Windows.Forms.Cursor.Current = System.Windows.Forms.Cursors.Default;
            }
        }
    }
}
