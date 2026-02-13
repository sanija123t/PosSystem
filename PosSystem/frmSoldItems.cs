using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PosSystem
{
    public partial class frmSoldItems : Form
    {
        #region Private Fields & Constants

        public string suser;
        private const string STATUS_SOLD = "Sold"; // Standardized to match tblCart1 status
        private readonly string _dbPath = DBConnection.MyConnection();
        private readonly string _appTitle = "POS System";

        [DllImport("user32.dll")]
        private static extern bool ReleaseCapture();
        [DllImport("user32.dll")]
        private static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);

        private const int WM_NCLBUTTONDOWN = 0xA1;
        private const int HT_CAPTION = 0x2;

        #endregion

        public frmSoldItems()
        {
            InitializeComponent();
            InitializeDefaults();
        }

        private void InitializeDefaults()
        {
            dateTimePicker1.Value = DateTime.Now;
            dateTimePicker2.Value = DateTime.Now;
            lblTotal1.Text = "0.00";
            lblTotal1.TextAlign = ContentAlignment.MiddleRight;
        }

        private async void frmSoldItems_Load(object sender, EventArgs e)
        {
            try
            {
                // Load UI components first, then data
                await LoadCashierAsync();
                await LoadSoldItemsAsync();
            }
            catch (Exception ex)
            {
                HandleError("Initialization Error", ex);
            }
        }

        #region Core Data Logic

        public async Task LoadSoldItemsAsync()
        {
            dataGridView1.Rows.Clear();
            decimal runningTotal = 0; // Fixed: Use decimal for money
            int recordCount = 0;

            // Database format usually expects yyyy-MM-dd
            string dateFrom = dateTimePicker1.Value.ToString("yyyy-MM-dd");
            string dateTo = dateTimePicker2.Value.ToString("yyyy-MM-dd");

            try
            {
                using (var cn = new SQLiteConnection(_dbPath))
                {
                    await cn.OpenAsync();

                    // Fixed SQL: Added net_total calculation and ensured parameter consistency
                    string sql = @"
                        SELECT c.id, c.transno, c.pcode, p.pdesc, 
                               c.price, c.qty, c.disc, 
                               c.total AS net_total
                        FROM tblCart1 AS c
                        INNER JOIN TblProduct1 AS p ON c.pcode = p.pcode
                        WHERE c.status = @status 
                        AND c.sdate BETWEEN @date1 AND @date2";

                    if (cbCashier.Text != "All Cashier" && !string.IsNullOrWhiteSpace(cbCashier.Text))
                    {
                        sql += " AND c.cashier = @cashier";
                    }

                    using (var cmd = new SQLiteCommand(sql, cn))
                    {
                        cmd.Parameters.AddWithValue("@status", STATUS_SOLD);
                        cmd.Parameters.AddWithValue("@date1", dateFrom);
                        cmd.Parameters.AddWithValue("@date2", dateTo);

                        if (cbCashier.Text != "All Cashier" && !string.IsNullOrWhiteSpace(cbCashier.Text))
                            cmd.Parameters.AddWithValue("@cashier", cbCashier.Text);

                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                recordCount++;
                                decimal netTotal = Convert.ToDecimal(reader["net_total"]);
                                runningTotal += netTotal;

                                dataGridView1.Rows.Add(
                                    recordCount,
                                    reader["id"],
                                    reader["transno"],
                                    reader["pcode"],
                                    reader["pdesc"],
                                    Convert.ToDecimal(reader["price"]).ToString("#,##0.00"),
                                    reader["qty"],
                                    Convert.ToDecimal(reader["disc"]).ToString("#,##0.00"),
                                    netTotal.ToString("#,##0.00")
                                );
                            }
                        }
                    }
                }
                lblTotal1.Text = runningTotal.ToString("#,##0.00");
            }
            catch (Exception ex)
            {
                HandleError("Data Load Error", ex);
            }
        }

        private async Task LoadCashierAsync()
        {
            try
            {
                cbCashier.Items.Clear();
                cbCashier.Items.Add("All Cashier");

                using (var cn = new SQLiteConnection(_dbPath))
                {
                    await cn.OpenAsync();
                    // Database Reference: tblUser column names
                    string sql = "SELECT username FROM tblUser ORDER BY username ASC";

                    using (var cmd = new SQLiteCommand(sql, cn))
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            cbCashier.Items.Add(reader["username"].ToString());
                        }
                    }
                }
                cbCashier.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                HandleError("Cashier List Error", ex);
            }
        }

        #endregion

        #region Event Handlers

        private async void FilterCriteria_Changed(object sender, EventArgs e)
        {
            await LoadSoldItemsAsync();
        }

        private void dataGridView1_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;

            string colName = dataGridView1.Columns[e.ColumnIndex].Name;
            if (colName == "ColCancel")
            {
                OpenCancelDetails(e.RowIndex);
            }
        }

        private void OpenCancelDetails(int rowIndex)
        {
            var row = dataGridView1.Rows[rowIndex];
            // Passing 'this' so frmCancelDetails can call LoadSoldItemsAsync() back
            frmCancelDetails f = new frmCancelDetails(this);

            f.txtID.Text = row.Cells[1].Value.ToString();
            f.txtTransno.Text = row.Cells[2].Value.ToString();
            f.txtPcode.Text = row.Cells[3].Value.ToString();
            f.txtDesc.Text = row.Cells[4].Value.ToString();
            f.txtPrice.Text = row.Cells[5].Value.ToString();
            f.txtQty.Text = row.Cells[6].Value.ToString();
            f.txtDiscount.Text = row.Cells[7].Value.ToString();
            f.txtTotal.Text = row.Cells[8].Value.ToString();
            f.txtCancelled.Text = suser; // Current user session
            f.ShowDialog();
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            // Implementation for report printing
            frmReportSold frm = new frmReportSold(this);
            frm.LoadReport();
            frm.ShowDialog();
        }

        private void pictureBox2_Click(object sender, EventArgs e) => this.Dispose();

        private void cbCashier_KeyPress(object sender, KeyPressEventArgs e) => e.Handled = true;

        private void panel1_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                ReleaseCapture();
                SendMessage(Handle, WM_NCLBUTTONDOWN, HT_CAPTION, 0);
            }
        }

        #endregion

        #region Helper Methods

        private void HandleError(string context, Exception ex)
        {
            MessageBox.Show($"{context}: {ex.Message}", _appTitle, MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        #endregion

        #region Designer Compatibility Stubs

        private void dateTimePicker1_ValueChanged(object sender, EventArgs e) => FilterCriteria_Changed(sender, e);
        private void dateTimePicker2_ValueChanged(object sender, EventArgs e) => FilterCriteria_Changed(sender, e);
        private void cbCashier_SelectedIndexChanged(object sender, EventArgs e) => FilterCriteria_Changed(sender, e);
        private void panel1_Paint(object sender, PaintEventArgs e) { }
        private void lblTotal1_Click(object sender, EventArgs e) { }

        #endregion
    }
}