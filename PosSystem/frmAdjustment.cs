using System;
using System.Data.SQLite;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Runtime.InteropServices;

namespace PosSystem
{
    public partial class frmAdjustment : Form
    {
        Form1 f;
        public string suser;
        private const string STATUS_SOLD = "sold";

        private Label lblTotal1;

        #region Drag Form Logic
        [DllImport("user32.dll")]
        public static extern bool ReleaseCapture();

        [DllImport("user32.dll")]
        public static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);

        private const int WM_NCLBUTTONDOWN = 0xA1;
        private const int HT_CAPTION = 0x2;
        #endregion

        public frmAdjustment()
        {
            InitializeComponent();

            // Initialize missing label
            lblTotal1 = new Label { Visible = false };
            this.Controls.Add(lblTotal1);

            txtSearch.TextChanged += async (s, e) => await LoadRecordsAsync();
            panel1.MouseDown += panel1_MouseDown;
        }

        public frmAdjustment(Form1 frm) : this()
        {
            this.f = frm;
            this.suser = frm._user;
        }

        private void frmAdjustment_Load(object sender, EventArgs e)
        {
            txtUser.Text = suser;
            referenceNo();
            _ = LoadRecordsAsync();
        }

        public void referenceNo()
        {
            Random r = new Random();
            int num = r.Next(100000, 999999);
            txtRef.Text = "ADJ-" + num.ToString();
        }

        public void LoadRecords()
        {
            _ = LoadRecordsAsync();
        }

        public async Task LoadRecordsAsync()
        {
            dataGridView1.Rows.Clear();
            int i = 0;
            string searchText = txtSearch.Text.Trim();

            try
            {
                using (var cn = new SQLiteConnection(DBConnection.MyConnection()))
                {
                    await cn.OpenAsync();

                    string query = "SELECT pcode, pdesc, qty FROM tblProduct WHERE pdesc LIKE @search";

                    using (var cmd = new SQLiteCommand(query, cn))
                    {
                        cmd.Parameters.AddWithValue("@search", $"%{searchText}%");

                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                i++;
                                dataGridView1.Rows.Add(
                                    i,
                                    reader["pcode"].ToString(),
                                    reader["pdesc"].ToString(),
                                    reader["qty"].ToString()
                                );
                            }
                        }
                    }
                }

                lblTotal1.Text = i.ToString();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message, "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void txtSearch_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == (char)Keys.Enter)
            {
                e.Handled = true;
                _ = LoadRecordsAsync();
            }
        }

        private async void btnSave_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(cbCommands.Text) || string.IsNullOrEmpty(txtQty.Text))
            {
                MessageBox.Show("Please fill in the Command and Quantity.", "Warning",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (!int.TryParse(txtQty.Text.Trim(), out int adjQty))
            {
                MessageBox.Show("Quantity must be a valid number.", "Warning",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string command = cbCommands.Text.Trim();
            string pcode = txtPcode.Text.Trim();

            if (string.IsNullOrEmpty(pcode))
            {
                MessageBox.Show("Please select a product from the grid.", "Warning",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                using (var cn = new SQLiteConnection(DBConnection.MyConnection()))
                {
                    await cn.OpenAsync();
                    using (var tran = cn.BeginTransaction())
                    {
                        // 1️⃣ Update tblProduct quantity
                        string updateSql = command.ToLower() == "add"
                            ? "UPDATE tblProduct SET qty = qty + @adjQty WHERE pcode = @pcode"
                            : "UPDATE tblProduct SET qty = qty - @adjQty WHERE pcode = @pcode";

                        using (var cmd = new SQLiteCommand(updateSql, cn, tran))
                        {
                            cmd.Parameters.AddWithValue("@adjQty", adjQty);
                            cmd.Parameters.AddWithValue("@pcode", pcode);
                            int rows = await cmd.ExecuteNonQueryAsync();
                            if (rows == 0)
                                throw new Exception("Product not found or adjustment failed.");
                        }

                        // 2️⃣ Insert into tblAdjustmentHistory
                        string insertSql = @"INSERT INTO tblAdjustmentHistory 
                                             (pcode, command, qty, adjustedBy, refNo, adjDate) 
                                             VALUES (@pcode, @command, @qty, @user, @refNo, @date)";

                        using (var cmd2 = new SQLiteCommand(insertSql, cn, tran))
                        {
                            cmd2.Parameters.AddWithValue("@pcode", pcode);
                            cmd2.Parameters.AddWithValue("@command", command);
                            cmd2.Parameters.AddWithValue("@qty", adjQty);
                            cmd2.Parameters.AddWithValue("@user", suser);
                            cmd2.Parameters.AddWithValue("@refNo", txtRef.Text);
                            cmd2.Parameters.AddWithValue("@date", DateTime.Now);
                            await cmd2.ExecuteNonQueryAsync();
                        }

                        tran.Commit();
                    }
                }

                MessageBox.Show("Stock has been successfully adjusted.", "Process Completed",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);

                referenceNo();
                _ = LoadRecordsAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Adjustment failed: " + ex.Message, "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void dataGridView1_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;

            if (e.ColumnIndex == dataGridView1.Columns["ActionSelect"].Index)
            {
                txtPcode.Text = dataGridView1.Rows[e.RowIndex].Cells[1].Value.ToString();
                txtdesc.Text = dataGridView1.Rows[e.RowIndex].Cells[2].Value.ToString();
            }
        }

        private void panel1_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                ReleaseCapture();
                SendMessage(Handle, WM_NCLBUTTONDOWN, HT_CAPTION, 0);
            }
        }

        private void pictureBox2_Click(object sender, EventArgs e)
        {
            this.Dispose();
        }
    }
}