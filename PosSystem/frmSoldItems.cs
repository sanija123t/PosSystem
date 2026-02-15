using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Drawing;
using System.Drawing.Printing;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PosSystem
{
    public partial class frmSoldItems : Form
    {
        #region Win32 API
        [DllImport("user32.dll")]
        private static extern bool ReleaseCapture();
        [DllImport("user32.dll")]
        private static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);

        private const int WM_NCLBUTTONDOWN = 0xA1;
        private const int HT_CAPTION = 0x2;
        #endregion

        #region Fields
        private readonly string _dbPath = DBConnection.MyConnection();
        private readonly string _appTitle = "POS System";
        private const string STATUS_SOLD = "Sold";

        public string _user;

        // Printing Objects
        PrintDocument printDoc = new PrintDocument();
        PrintPreviewDialog previewDlg = new PrintPreviewDialog();
        #endregion

        #region Public Properties (Bridges)
        public string suser { get => _user; set => _user = value; }
        public DateTimePicker dt1 => dateTimePicker1;
        public DateTimePicker dt2 => dateTimePicker2;
        #endregion

        public frmSoldItems()
        {
            InitializeComponent();
            InitializeDefaults();

            // Attach Print Event
            printDoc.PrintPage += new PrintPageEventHandler(printDoc_PrintPage);
        }

        private void InitializeDefaults()
        {
            if (dateTimePicker1 != null) dateTimePicker1.Value = DateTime.Now;
            if (dateTimePicker2 != null) dateTimePicker2.Value = DateTime.Now;
            if (lblTotal1 != null) lblTotal1.Text = "0.00";
        }

        private async void frmSoldItems_Load(object sender, EventArgs e)
        {
            try
            {
                await LoadCashierAsync();
                await LoadSoldItemsAsync();
            }
            catch (Exception ex)
            {
                HandleError("Initialization Error", ex);
            }
        }

        public async Task LoadSoldItemsAsync()
        {
            if (dataGridView1 == null) return;

            dataGridView1.Rows.Clear();
            decimal runningTotal = 0;
            int recordCount = 0;

            string dateFrom = dateTimePicker1.Value.ToString("yyyy-MM-dd");
            string dateTo = dateTimePicker2.Value.ToString("yyyy-MM-dd");

            try
            {
                using (var cn = new SQLiteConnection(_dbPath))
                {
                    await cn.OpenAsync();
                    string sql = @"
                        SELECT c.id, c.transno, c.pcode, p.pdesc, 
                               c.price, c.qty, c.disc, c.total
                        FROM tblCart1 AS c
                        INNER JOIN TblProduct1 AS p ON c.pcode = p.pcode
                        WHERE c.status = @status 
                        AND c.sdate BETWEEN @date1 AND @date2";

                    bool filterByCashier = cbCashier.Text != "All Cashier" && !string.IsNullOrWhiteSpace(cbCashier.Text);
                    if (filterByCashier) sql += " AND c.cashier = @cashier";

                    using (var cmd = new SQLiteCommand(sql, cn))
                    {
                        cmd.Parameters.AddWithValue("@status", STATUS_SOLD);
                        cmd.Parameters.AddWithValue("@date1", dateFrom);
                        cmd.Parameters.AddWithValue("@date2", dateTo);
                        if (filterByCashier) cmd.Parameters.AddWithValue("@cashier", cbCashier.Text);

                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                recordCount++;
                                decimal total = Convert.ToDecimal(reader["total"]);
                                runningTotal += total;

                                dataGridView1.Rows.Add(
                                    recordCount,
                                    reader["id"],
                                    reader["transno"],
                                    reader["pcode"],
                                    reader["pdesc"],
                                    Convert.ToDecimal(reader["price"]).ToString("#,##0.00"),
                                    reader["qty"],
                                    Convert.ToDecimal(reader["disc"]).ToString("#,##0.00"),
                                    total.ToString("#,##0.00")
                                );
                            }
                        }
                    }
                }
                lblTotal1.Text = runningTotal.ToString("#,##0.00");
            }
            catch (Exception ex)
            {
                HandleError("Load Error", ex);
            }
        }

        private async Task LoadCashierAsync()
        {
            if (cbCashier == null) return;
            cbCashier.Items.Clear();
            cbCashier.Items.Add("All Cashier");
            using (var cn = new SQLiteConnection(_dbPath))
            {
                await cn.OpenAsync();
                using (var cmd = new SQLiteCommand("SELECT username FROM tblUser ORDER BY username ASC", cn))
                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync()) cbCashier.Items.Add(reader["username"].ToString());
                }
            }
            if (cbCashier.Items.Count > 0) cbCashier.SelectedIndex = 0;
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            // Trigger Windows Print Preview directly
            previewDlg.Document = printDoc;
            previewDlg.WindowState = FormWindowState.Maximized;
            previewDlg.ShowDialog();
        }

        // The Windows Print Preview Rendering Logic
        private void printDoc_PrintPage(object sender, PrintPageEventArgs e)
        {
            Graphics g = e.Graphics;
            Font fRegular = new Font("Arial", 9);
            Font fBold = new Font("Arial", 9, FontStyle.Bold);
            Font fHeader = new Font("Arial", 14, FontStyle.Bold);
            float y = 50;
            float x = 50;

            // Header
            g.DrawString("DAILY SOLD ITEMS REPORT", fHeader, Brushes.Black, x, y);
            y += 30;
            g.DrawString($"Period: {dateTimePicker1.Value.ToShortDateString()} - {dateTimePicker2.Value.ToShortDateString()}", fRegular, Brushes.Black, x, y);
            y += 20;
            g.DrawString($"Cashier: {cbCashier.Text}", fRegular, Brushes.Black, x, y);
            y += 40;

            // Table Header
            g.DrawString("Description", fBold, Brushes.Black, x, y);
            g.DrawString("Price", fBold, Brushes.Black, x + 250, y);
            g.DrawString("Qty", fBold, Brushes.Black, x + 350, y);
            g.DrawString("Total", fBold, Brushes.Black, x + 450, y);
            y += 20;
            g.DrawLine(Pens.Black, x, y, x + 550, y);
            y += 10;

            // Rows from DataGridView
            foreach (DataGridViewRow row in dataGridView1.Rows)
            {
                if (row.Cells[4].Value == null) continue;
                g.DrawString(row.Cells[4].Value.ToString(), fRegular, Brushes.Black, x, y); // Desc
                g.DrawString(row.Cells[5].Value.ToString(), fRegular, Brushes.Black, x + 250, y); // Price
                g.DrawString(row.Cells[6].Value.ToString(), fRegular, Brushes.Black, x + 350, y); // Qty
                g.DrawString(row.Cells[8].Value.ToString(), fRegular, Brushes.Black, x + 450, y); // Total
                y += 20;

                if (y > e.PageBounds.Height - 100)
                {
                    e.HasMorePages = true;
                    return;
                }
            }

            y += 20;
            g.DrawLine(Pens.Black, x, y, x + 550, y);
            y += 10;
            g.DrawString($"GRAND TOTAL: {lblTotal1.Text}", fHeader, Brushes.Black, x + 300, y);
        }

        private void dataGridView1_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
            string colName = dataGridView1.Columns[e.ColumnIndex].Name;

            if (colName == "ColCancel")
            {
                var row = dataGridView1.Rows[e.RowIndex];
                frmCancelDetails f = new frmCancelDetails(this);
                f.txtID.Text = row.Cells[1].Value.ToString();
                f.txtTransno.Text = row.Cells[2].Value.ToString();
                f.txtPcode.Text = row.Cells[3].Value.ToString();
                f.txtDesc.Text = row.Cells[4].Value.ToString();
                f.txtPrice.Text = row.Cells[5].Value.ToString();
                f.txtQty.Text = row.Cells[6].Value.ToString();
                f.txtDiscount.Text = row.Cells[7].Value.ToString();
                f.txtTotal.Text = row.Cells[8].Value.ToString();
                f.txtCancelled.Text = _user;
                f.ShowDialog();
            }
        }

        private void pictureBox2_Click(object sender, EventArgs e) => this.Dispose();

        private void panel1_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                ReleaseCapture();
                SendMessage(Handle, WM_NCLBUTTONDOWN, HT_CAPTION, 0);
            }
        }

        private void dateTimePicker1_ValueChanged(object sender, EventArgs e) => _ = LoadSoldItemsAsync();
        private void dateTimePicker2_ValueChanged(object sender, EventArgs e) => _ = LoadSoldItemsAsync();
        private void cbCashier_SelectedIndexChanged(object sender, EventArgs e) => _ = LoadSoldItemsAsync();
        private void panel1_Paint(object sender, PaintEventArgs e) { }
        private void lblTotal1_Click(object sender, EventArgs e) { }

        private void HandleError(string context, Exception ex)
        {
            MessageBox.Show($"{context}: {ex.Message}", _appTitle, MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}