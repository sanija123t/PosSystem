using System;
using System.Data;
using System.Data.SQLite;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PosSystem
{
    public partial class frm_VendorList : Form
    {
        #region DRAGGABLE LOGIC
        [DllImport("user32.dll")]
        public static extern bool ReleaseCapture();
        [DllImport("user32.dll")]
        public static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);
        public const int WM_NCLBUTTONDOWN = 0xA1;
        public const int HT_CAPTION = 0x2;

        private void panelTitle_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                ReleaseCapture();
                SendMessage(Handle, WM_NCLBUTTONDOWN, HT_CAPTION, 0);
            }
        }
        #endregion

        public frm_VendorList()
        {
            InitializeComponent();
            ConfigureGrid();
        }

        private void ConfigureGrid()
        {
            // This call will now use the Global extension method
            dataGridView1.DoubleBuffered(true);
            dataGridView1.ReadOnly = true;
            dataGridView1.AllowUserToAddRows = false;
            dataGridView1.AllowUserToResizeRows = false;
            dataGridView1.SelectionMode = DataGridViewSelectionMode.FullRowSelect;

            foreach (DataGridViewColumn col in dataGridView1.Columns)
            {
                if (col.Name != "Delete") col.ReadOnly = true;
            }
        }

        public void LoadRecords()
        {
            _ = LoadRecordsAsync();
        }

        public async Task LoadRecordsAsync()
        {
            try
            {
                dataGridView1.SuspendLayout();
                dataGridView1.Rows.Clear();

                using (SQLiteConnection cn = new SQLiteConnection(DBConnection.MyConnection()))
                {
                    await cn.OpenAsync();
                    using (SQLiteCommand cm = new SQLiteCommand("SELECT * FROM tblVendor ORDER BY vendor ASC", cn))
                    using (SQLiteDataReader dr = (SQLiteDataReader)await cm.ExecuteReaderAsync())
                    {
                        int i = 0;
                        while (await dr.ReadAsync())
                        {
                            i++;
                            dataGridView1.Rows.Add(
                                i,
                                dr["id"].ToString(),
                                dr["vendor"].ToString(),
                                dr["address"].ToString(),
                                dr["contactperson"].ToString(),
                                dr["telephone"].ToString(),
                                dr["email"].ToString(),
                                dr["fax"].ToString(),
                                "Delete"
                            );
                        }
                    }
                }
            }
            catch (SQLiteException sqlEx)
            {
                MessageBox.Show("Database error: " + sqlEx.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Unexpected error: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                dataGridView1.ResumeLayout();
            }
        }

        private async void frm_VendorList_Load(object sender, EventArgs e)
        {
            await LoadRecordsAsync();
        }

        private void pictureBox2_Click(object sender, EventArgs e)
        {
            frmVendor frm = new frmVendor(this);
            frm.Clear();
            frm.btnSave.Enabled = true;
            frm.btnUpdate.Enabled = false;
            frm.ShowDialog();
        }

        private void pictureBox1_Click(object sender, EventArgs e) => this.Dispose();

        private async void dataGridView1_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;

            string colname = dataGridView1.Columns[e.ColumnIndex].Name;

            try
            {
                if (colname == "Column2" || colname == "Column3")
                {
                    frmVendor f = new frmVendor(this);
                    var row = dataGridView1.Rows[e.RowIndex];

                    f.txtVendorID.Text = row.Cells["Column2"].Value?.ToString();
                    f.txtVendor.Text = row.Cells["Column3"].Value?.ToString();
                    f.txtAddress.Text = row.Cells["Column4"].Value?.ToString();
                    f.txtContactPreson.Text = row.Cells["Column5"].Value?.ToString();
                    f.txtTelephone.Text = row.Cells["Column6"].Value?.ToString();
                    f.txtEmail.Text = row.Cells["Column7"].Value?.ToString();
                    f.txtFax.Text = row.Cells["Column8"].Value?.ToString();

                    f.btnSave.Enabled = false;
                    f.btnUpdate.Enabled = true;
                    f.txtVendorID.ReadOnly = true;
                    f.ShowDialog();
                }
                else if (colname == "Delete")
                {
                    if (MessageBox.Show("Delete this vendor record?", "Confirm Delete",
                        MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
                    {
                        using (SQLiteConnection cn = new SQLiteConnection(DBConnection.MyConnection()))
                        {
                            await cn.OpenAsync();
                            using (SQLiteCommand cm = new SQLiteCommand("DELETE FROM tblVendor WHERE id = @id", cn))
                            {
                                cm.Parameters.AddWithValue("@id", dataGridView1.Rows[e.RowIndex].Cells["Column2"].Value.ToString());
                                await cm.ExecuteNonQueryAsync();
                            }
                        }
                        MessageBox.Show("Record deleted successfully!", "POS System", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        await LoadRecordsAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Operation failed: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}