using System;
using System.Data;
using System.Data.SQLite;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PosSystem
{
    public partial class frmSoldItems : Form
    {
        public string suser;
        private const string STATUS_SOLD = "sold";

        #region Drag Form
        [DllImport("user32.dll")]
        public static extern bool ReleaseCapture();

        [DllImport("user32.dll")]
        public static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);

        private const int WM_NCLBUTTONDOWN = 0xA1;
        private const int HT_CAPTION = 0x2;
        #endregion

        public frmSoldItems()
        {
            InitializeComponent();
            dateTimePicker1.Value = DateTime.Now;
            dateTimePicker2.Value = DateTime.Now;
        }

        private async void frmSoldItems_Load(object sender, EventArgs e)
        {
            await LoadCashierAsync();
            await LoadSoldItemsAsync();
        }

        #region Sold Items Logic

        public async Task LoadSoldItemsAsync()
        {
            dataGridView1.Rows.Clear();
            double totalAmount = 0;
            int i = 0;

            DateTime startDate = dateTimePicker1.Value.Date;
            DateTime endDate = dateTimePicker2.Value.Date.AddDays(1).AddSeconds(-1);

            try
            {
                using (var cn = new SQLiteConnection(DBConnection.MyConnection()))
                {
                    await cn.OpenAsync();

                    string query =
                        @"SELECT c.id, c.transno, c.pcode, p.pdesc,
                                 c.price, c.qty, c.disc,
                                 (c.qty * c.price) - c.disc AS total
                          FROM tblCart1 c
                          INNER JOIN TblProduct1 p ON c.pcode = p.pcode
                          WHERE c.status = @status
                          AND c.sdate BETWEEN @date1 AND @date2";

                    if (cbCashier.Text != "All Cashier")
                        query += " AND c.cashier = @cashier";

                    using (var cmd = new SQLiteCommand(query, cn))
                    {
                        cmd.Parameters.AddWithValue("@status", STATUS_SOLD);
                        cmd.Parameters.AddWithValue("@date1", startDate);
                        cmd.Parameters.AddWithValue("@date2", endDate);

                        if (cbCashier.Text != "All Cashier")
                            cmd.Parameters.AddWithValue("@cashier", cbCashier.Text);

                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                i++;
                                double rowTotal = Convert.ToDouble(reader["total"]);
                                totalAmount += rowTotal;

                                dataGridView1.Rows.Add(
                                    i,
                                    reader["id"].ToString(),
                                    reader["transno"].ToString(),
                                    reader["pcode"].ToString(),
                                    reader["pdesc"].ToString(),
                                    reader["price"].ToString(),
                                    reader["qty"].ToString(),
                                    reader["disc"].ToString(),
                                    rowTotal.ToString("#,##0.00")
                                );
                            }
                        }
                    }
                }

                lblTotal1.Text = totalAmount.ToString("#,##0.00");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Load Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        #endregion

        #region Cashier Logic

        private async Task LoadCashierAsync()
        {
            try
            {
                cbCashier.Items.Clear();
                cbCashier.Items.Add("All Cashier");

                using (var cn = new SQLiteConnection(DBConnection.MyConnection()))
                {
                    await cn.OpenAsync();
                    using (var cmd = new SQLiteCommand(
                        "SELECT username FROM tblUser WHERE role = 'Cashier'", cn))
                    {
                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                cbCashier.Items.Add(reader["username"].ToString());
                            }
                        }
                    }
                }

                cbCashier.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Cashier Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        #endregion

        #region Events

        private async void cbCashier_SelectedIndexChanged(object sender, EventArgs e)
        {
            await LoadSoldItemsAsync();
        }

        private async void dateTimePicker1_ValueChanged(object sender, EventArgs e)
        {
            await LoadSoldItemsAsync();
        }

        private async void dateTimePicker2_ValueChanged(object sender, EventArgs e)
        {
            await LoadSoldItemsAsync();
        }

        private void cbCashier_KeyPress(object sender, KeyPressEventArgs e)
        {
            e.Handled = true;
        }

        private void pictureBox2_Click(object sender, EventArgs e)
        {
            Dispose();
        }

        private void panel1_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                ReleaseCapture();
                SendMessage(Handle, WM_NCLBUTTONDOWN, HT_CAPTION, 0);
            }
        }

        #endregion

        #region Cancel Item

        private void dataGridView1_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;

            if (dataGridView1.Columns[e.ColumnIndex].Name == "ColCancel")
            {
                frmCancelDetails f = new frmCancelDetails(this);
                f.txtID.Text = dataGridView1.Rows[e.RowIndex].Cells[1].Value.ToString();
                f.txtTransno.Text = dataGridView1.Rows[e.RowIndex].Cells[2].Value.ToString();
                f.txtPcode.Text = dataGridView1.Rows[e.RowIndex].Cells[3].Value.ToString();
                f.txtDesc.Text = dataGridView1.Rows[e.RowIndex].Cells[4].Value.ToString();
                f.txtPrice.Text = dataGridView1.Rows[e.RowIndex].Cells[5].Value.ToString();
                f.txtQty.Text = dataGridView1.Rows[e.RowIndex].Cells[6].Value.ToString();
                f.txtDiscount.Text = dataGridView1.Rows[e.RowIndex].Cells[7].Value.ToString();
                f.txtTotal.Text = dataGridView1.Rows[e.RowIndex].Cells[8].Value.ToString();
                f.txtCancelled.Text = suser;
                f.ShowDialog();
            }
        }

        private void panel1_Paint(object sender, PaintEventArgs e)
        {
            // intentionally left empty
        }

        #endregion

        #region Report

        private void btnSave_Click(object sender, EventArgs e)
        {
            frmReportSold frm = new frmReportSold(this);
            frm.LoadReport();
            frm.ShowDialog();
        }

        #endregion
    }
}