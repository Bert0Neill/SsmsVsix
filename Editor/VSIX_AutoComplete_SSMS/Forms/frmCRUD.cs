using System;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using VSIX_AutoComplete_SSMS.Classes;
using VSIX_AutoComplete_SSMS.Models;
using Action = System.Action;
using SQL_Shortcuts.Resources;

namespace VSIX_AutoComplete_SSMS.Forms
{
    public partial class frmCRUD : Form
    {
        #region Class Variables
        private const string INSERT_TABLE = "<Table>";
        private readonly InlineCSharpHelper _inlineCSharpHelper;
        private readonly bool _isInsert;
        private BindingList<SqlShortcuts> _workingCopy;
        public SqlShortcuts _selectedRecord { get; private set; }
        #endregion

        public frmCRUD(BindingList<SqlShortcuts> workingCopy, SqlShortcuts selectedRecord = null)
        {
            try
            {
                System.Windows.Forms.Cursor.Current = System.Windows.Forms.Cursors.WaitCursor;
                InitializeComponent();

                // clear any previous errors
                errorProviderCrud.SetError(txtSqlShortcut, "");
                errorProviderCrud.Clear();

                // add TextChanged event handlers to update button state
                txtSqlComplete.TextChanged += UpdateButtonState;
                txtSqlShortcut.TextChanged += UpdateButtonState;

                _inlineCSharpHelper = new InlineCSharpHelper();
                _workingCopy = workingCopy;
                _selectedRecord = selectedRecord ?? new SqlShortcuts();

                // determine if this is an update or insert operation
                if (_selectedRecord == null)
                {
                    _isInsert = true; // Set the flag to true for insert operation
                    Text = Resource.Grid_Add_New_Row;
                    txtSqlShortcut.Text = string.Empty;
                    txtSqlComplete.Text = string.Empty;
                }
                else
                {
                    Text = Resource.Grid_Edit_Row;
                    txtSqlShortcut.Text = _selectedRecord.SqlShort;
                    txtSqlComplete.Text = _selectedRecord.SqlComplete;
                }

                // if demo version limit the Sql statement to 100 characters
                if (SQL_Shortcuts.Properties.Settings.Default.IsDemo)
                {
                    txtSqlComplete.MaxLength = 100;
                    toolStripStatusLabelCrud.Text = Resource.Grid_Demo_Limit;
                }

                // initial button state
                UpdateButtonState(null, null);
            }
            catch (Exception ex)
            {
                SimpleLogger.Error(ex.ToString());
            }
            finally
            {
                System.Windows.Forms.Cursor.Current = System.Windows.Forms.Cursors.Default;
            }
        }

        public bool IsSqlShortAlreadyUsed(string sqlShort, SqlShortcuts currentItem = null)
        {
            try
            {
                System.Windows.Forms.Cursor.Current = System.Windows.Forms.Cursors.WaitCursor;

                // case-insensitive check if any other item has the same SqlShort
                return _workingCopy.Any(item =>
                    !ReferenceEquals(item, currentItem) && // Skip the current item (for updates)
                    string.Equals(item.SqlShort, sqlShort, StringComparison.OrdinalIgnoreCase));
            }
            catch (Exception ex)
            {
                SetStatusBarText(ex.Message);
                SimpleLogger.Error(ex.ToString());
                return true; // don't allow to save if error
            }
            finally
            {
                System.Windows.Forms.Cursor.Current = System.Windows.Forms.Cursors.Default;
            }
        }

        private void UpdateButtonState(object sender, EventArgs e)
        {
            // enable\disable button based on field(s) status
            bool sqlShortValid = !string.IsNullOrWhiteSpace(txtSqlShortcut.Text);
            bool sqlCompleteValid = !string.IsNullOrWhiteSpace(txtSqlComplete.Text);
            btnSaveSqlStatement.Enabled = sqlShortValid && sqlCompleteValid;
            btnTestSqlStatement.Enabled = sqlCompleteValid;
        }

        private void btnInsTable_Click(object sender, EventArgs e)
        {
            InsertTableTag(INSERT_TABLE);
        }

        private void InsertTableTag(string insertText)
        {
            try
            {
                System.Windows.Forms.Cursor.Current = System.Windows.Forms.Cursors.WaitCursor;

                // Schedule focus + insert logic to run after current UI thread events
                this.BeginInvoke(new Action(() =>
                {
                    // restore focus to ensure correct cursor position
                    txtSqlComplete.Focus();
                    Application.DoEvents(); // Let WinForms update the focus

                    if (this.ActiveControl == this.txtSqlComplete)
                    {
                        // Insert at cursor position
                        int selectionStart = txtSqlComplete.SelectionStart;
                        txtSqlComplete.Text = txtSqlComplete.Text.Insert(selectionStart, insertText);
                        txtSqlComplete.SelectionStart = selectionStart + insertText.Length;
                    }
                    else
                    {
                        // append at the end
                        txtSqlComplete.Text += insertText;
                        txtSqlComplete.SelectionStart = txtSqlComplete.Text.Length;
                    }
                    
                    txtSqlComplete.ScrollToCaret(); // scroll to caret (for multiline display)
                }));
            }
            catch (Exception ex)
            {
                SetStatusBarText(ex.Message);
                SimpleLogger.Error(ex.ToString());
            }
            finally
            {
                System.Windows.Forms.Cursor.Current = System.Windows.Forms.Cursors.Default;
            }
        }

        private void btnSpltDates_DropDowItemClicked(object sender, ToolStripItemClickedEventArgs e)
        {
            InsertTableTag(e.ClickedItem.Text);
        }

        private void btnSaveSqlStatement_Click(object sender, EventArgs e)
        {
            try
            {
                System.Windows.Forms.Cursor.Current = System.Windows.Forms.Cursors.WaitCursor;

                _selectedRecord.SqlShort = this.txtSqlShortcut.Text.Trim();
                _selectedRecord.SqlComplete = this.txtSqlComplete.Text;

                if (_selectedRecord.IsValid())
                {
                    // verify that shortcut name not already used
                    if (_isInsert)
                    {
                        // when adding a new item
                        if (IsSqlShortAlreadyUsed(_selectedRecord.SqlShort))
                        {
                            errorProviderCrud.SetError(txtSqlShortcut, string.Format(Resource.Grid_Shortcut_Exists, _selectedRecord.SqlShort));
                            return;
                        }
                    }
                    else
                    {
                        // when updating an existing item
                        if (IsSqlShortAlreadyUsed(_selectedRecord.SqlShort, _selectedRecord))
                        {
                            errorProviderCrud.SetError(txtSqlShortcut, string.Format(Resource.Grid_Shortcut_Exists, _selectedRecord.SqlShort));
                            return;
                        }
                    }

                    this.DialogResult = DialogResult.OK;
                    this.Close();
                }               
            }
            catch (Exception ex)
            {
                SetStatusBarText(ex.Message);
                SimpleLogger.Error(ex.ToString());
            }
            finally
            {
                System.Windows.Forms.Cursor.Current = System.Windows.Forms.Cursors.Default;
            }
        }

        private void btnTestSqlStatement_Click(object sender, EventArgs e)
        {
            try
            {
                System.Windows.Forms.Cursor.Current = System.Windows.Forms.Cursors.WaitCursor;

                _selectedRecord.SqlShort = this.txtSqlShortcut.Text;
                _selectedRecord.SqlComplete = this.txtSqlComplete.Text;
                _inlineCSharpHelper.EvaluateSqlStatement(_selectedRecord);
            }
            catch (Exception ex)
            {
                SetStatusBarText(ex.Message);
                SimpleLogger.Error(ex.ToString());
            }
            finally
            {
                System.Windows.Forms.Cursor.Current = System.Windows.Forms.Cursors.Default;
            }
        }

        private void txtSqlShortcut_KeyUp(object sender, KeyEventArgs e)
        {
            if (txtSqlShortcut.Text.Trim() == string.Empty)
            {
                errorProviderCrud.SetError(txtSqlShortcut, "");
                errorProviderCrud.Clear();
                return;
            }
            else if (IsSqlShortAlreadyUsed(txtSqlShortcut.Text.Trim()))
            {
                errorProviderCrud.SetError(txtSqlShortcut, string.Format(Resource.Grid_Shortcut_Exists, txtSqlShortcut.Text.Trim()));
                return;
            }
            else
            {
                errorProviderCrud.SetError(txtSqlShortcut, "");
                errorProviderCrud.Clear();
            }
        }

        private void SetStatusBarText(string message, string prefix = null)
        {
            toolStripStatusLabelCrud.Text = $"{prefix ?? Resource.Error_Disdplay} {message}".Trim();
            toolStripStatusLabelCrud.ForeColor = Color.Black;
        }

        private void frmCRUD_Load(object sender, EventArgs e)
        {

        }
    }
}
