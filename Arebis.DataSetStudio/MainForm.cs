using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Arebis.DataSetStudio
{
    public partial class MainForm : Form
    {
        public MainForm()
        {
            InitializeComponent();
        }

        public string BaseTitle;
        public string Filename { get; set; }
        public bool IsDirty;

        private void MainForm_Load(object sender, EventArgs e)
        {
            this.BaseTitle = this.Text;

            this.dataGridView.AutoGenerateColumns = true;

            var args = Environment.GetCommandLineArgs();
            if (args.Length > 1)
            {
                OpenFile(args[1]);
            }

            // Configure tableView:
            tableListView.Columns.Clear();
            tableListView.Columns.Add(new ColumnHeader() { Text = "Table", Width = 300 });
            tableListView.Columns.Add(new ColumnHeader() { Text = "Rows", Width = 60, TextAlign = HorizontalAlignment.Right });

            // Support drag&drop:
            ActivateDragDropOnControl(this);
            ActivateDragDropOnControl(this.tableListView);
            ActivateDragDropOnControl(this.dataGridView);
        }

        private void ActivateDragDropOnControl(Control control)
        {
            control.AllowDrop = true;
            control.DragEnter += OnDragEnter;
            control.DragDrop += OnDragDrop;
        }

        private void tableListView_SelectedIndexChanged(object sender, EventArgs e)
        {
            var items = this.tableListView.SelectedItems;
            if (items.Count > 0)
            {
                this.bindingSource.DataSource = items[0].Tag;
            }
            else
            {
                this.bindingSource.DataSource = null;
            }
        }

        private void mainSplitContainer_SplitterMoved(object sender, SplitterEventArgs e)
        {
            tableListView.Columns[0].Width = mainSplitContainer.SplitterDistance - 90;
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Environment.Exit(0);
        }

        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (openFileDialog.ShowDialog(this) == System.Windows.Forms.DialogResult.OK)
            {
                OpenFile(openFileDialog.FileName);
            }
        }

        private void saveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.SaveFile();
        }

        private void saveAsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (saveFileDialog.ShowDialog(this) == System.Windows.Forms.DialogResult.OK)
            {
                this.Filename = saveFileDialog.FileName;
                this.SaveFile();
                this.Text = this.BaseTitle + " - " + this.Filename;
                this.saveToolStripMenuItem.Enabled = true;
            }
        }

        private void closeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.CloseFile();
        }

        private void OpenFile(string filename)
        {
            if (!AllowToProceed()) return;

            this.Filename = filename;

            this.dataSet.Reset();
            this.dataSet.ReadXml(filename);

            tableListView.Items.Clear();

            foreach (var table in dataSet.Tables.OfType<DataTable>()/*.OrderBy(t => t.TableName)*/)
            {
                var li = new ListViewItem()
                {
                    Text = table.TableName,
                    ImageIndex = 1,
                    Tag = table
                };
                li.SubItems.Add(table.Rows.Count.ToString());
                tableListView.Items.Add(li);
            }

            saveToolStripMenuItem.Enabled = !new System.IO.FileInfo(filename).IsReadOnly;
            saveAsToolStripMenuItem.Enabled = true;

            this.Text = this.BaseTitle + " - " + filename;

            openFileDialog.FileName = filename;
            saveFileDialog.FileName = filename;

            MarkDirty(false);
        }

        private void SaveFile()
        {
            this.dataSet.WriteXml(this.Filename, XmlWriteMode.WriteSchema);
            MarkDirty(false);
        }

        private void CloseFile()
        {
            if (!AllowToProceed()) return;

            this.Filename = null;
            this.MarkDirty(false);

            this.bindingSource.DataSource = null;
            this.tableListView.Items.Clear();
            this.dataSet.Reset();

            saveToolStripMenuItem.Enabled = false;
            saveAsToolStripMenuItem.Enabled = false;
            this.Text = BaseTitle;
        }

        private void OnDragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effect = DragDropEffects.Move;
            }
            else
            {
                e.Effect = DragDropEffects.None;
            }
        }

        private void OnDragDrop(object sender, DragEventArgs e)
        {
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop, false);
            if (files != null && files.Length > 0)
            {
                this.OpenFile(files[0]);
            }
        }

        private void MainForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            Environment.Exit(0);
        }

        private void dataGridView_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            MarkDirty(true);
        }

        private void dataGridView_RowsRemoved(object sender, DataGridViewRowsRemovedEventArgs e)
        {
            //MarkDirty(true);
        }

        private void MarkDirty(bool value)
        {
            if (value == true && IsDirty == false)
            {
                this.Text = this.Text + " *";
            }

            this.IsDirty = value;
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            CloseFile();

            e.Cancel = (this.Filename != null);
        }

        private bool AllowToProceed()
        {
            if (IsDirty && saveToolStripMenuItem.Enabled)
            {
                var dresult = MessageBox.Show(this, "Save file changes first ?", "Save changes", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question, MessageBoxDefaultButton.Button3);
                if (dresult == System.Windows.Forms.DialogResult.Yes)
                {
                    this.SaveFile();
                    return true;
                }
                else if (dresult == System.Windows.Forms.DialogResult.No)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                return true;
            }
        }
    }
}
