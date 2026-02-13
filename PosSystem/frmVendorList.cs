using System;
using System.Data;
using System.Data.SQLite;
using System.Runtime.InteropServices; // Required for Draggable Logic
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PosSystem
{
    public partial class frm_VendorList : Form
    {
        // --- DRAGGABLE LOGIC (WinAPI) ---
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
        // -------------------------------

        public frm_VendorList()
        {
            InitializeComponent();
        }

        // 🔁 BACKWARD-COMPATIBILITY METHOD
        // Old code can still call LoadRecords()
        public void LoadRecords()
        {
            _ = LoadRecordsAsync();
        }

        // 🚀 ASYNC + SAFE LOAD
        public async Task LoadRecordsAsync()
        {
            dataGridView1.Rows.Clear();

            try
            {
                using (SQLiteConnection cn = new SQLiteConnection(DBConnection.MyConnection()))
                {
                    await cn.OpenAsync();

                    using (SQLiteCommand cm = new SQLiteCommand(
                        "SELECT * FROM tblVendor ORDER BY vendor ASC", cn))
                    using (var dr = await cm.ExecuteReaderAsync()) // ✅ FIXED
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
                                dr["fax"].ToString()
                            );
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async void frm_VendorList_Load(object sender, EventArgs e)
        {
            await LoadRecordsAsync();
        }

        private void pictureBox2_Click(object sender, EventArgs e)
        {
            frmVendor frm = new frmVendor(this);
            frm.btnSave.Enabled = true;
            frm.btnUpdate.Enabled = false;
            frm.ShowDialog();
        }

        private void pictureBox1_Click(object sender, EventArgs e)
        {
            this.Dispose();
        }

        private async void dataGridView1_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;

            string colname = dataGridView1.Columns[e.ColumnIndex].Name;

            if (colname == "Edit")
            {
                frmVendor f = new frmVendor(this);

                // ✅ COLUMN-NAME SAFE ACCESS
                f.lblD.Text = dataGridView1.Rows[e.RowIndex].Cells["id"].Value.ToString();
                f.txtVendor.Text = dataGridView1.Rows[e.RowIndex].Cells["vendor"].Value.ToString();
                f.txtAddress.Text = dataGridView1.Rows[e.RowIndex].Cells["address"].Value.ToString();
                f.txtContactPreson.Text = dataGridView1.Rows[e.RowIndex].Cells["contactperson"].Value.ToString();
                f.txtTelephone.Text = dataGridView1.Rows[e.RowIndex].Cells["telephone"].Value.ToString();
                f.txtEmail.Text = dataGridView1.Rows[e.RowIndex].Cells["email"].Value.ToString();
                f.txtFax.Text = dataGridView1.Rows[e.RowIndex].Cells["fax"].Value.ToString();

                f.btnSave.Enabled = false;
                f.btnUpdate.Enabled = true;
                f.ShowDialog();
            }
            else if (colname == "Delete")
            {
                if (MessageBox.Show("Delete this record?", "Delete",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    try
                    {
                        using (SQLiteConnection cn = new SQLiteConnection(DBConnection.MyConnection()))
                        {
                            await cn.OpenAsync();

                            using (SQLiteCommand cm = new SQLiteCommand(
                                "DELETE FROM tblVendor WHERE id = @id", cn))
                            {
                                cm.Parameters.AddWithValue("@id",
                                    dataGridView1.Rows[e.RowIndex].Cells["id"].Value.ToString());

                                await cm.ExecuteNonQueryAsync();
                            }
                        }

                        MessageBox.Show("Record deleted!", "POS",
                            MessageBoxButtons.OK, MessageBoxIcon.Information);

                        await LoadRecordsAsync();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message, "Error",
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }
    }
}