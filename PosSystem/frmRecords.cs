using System;
using System.Data;
using System.Data.SQLite;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Drawing;
using System.Drawing.Printing;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace PosSystem
{
    public partial class frmRecords : Form
    {
        private readonly string stitle = "PosSystem";
        private System.Windows.Forms.Timer searchDelayTimer;

        // Use int for Interlocked operations (0 = false, 1 = true)
        private int _isInventoryLoading = 0;
        private int _isCancelLoading = 0;
        private int _isStockLoading = 0;

        private string _lastInventorySearch = string.Empty;
        private string _lastCancelSearch = string.Empty;

        private CancellationTokenSource _inventoryCts;
        private CancellationTokenSource _cancelCts;
        private CancellationTokenSource _stockCts;

        // Static Font Cache to optimize GDI performance and prevent leaks
        private static readonly Font hFont = new Font("Segoe UI", 9, FontStyle.Bold);
        private static readonly Font cFont = new Font("Segoe UI", 8, FontStyle.Regular);
        private static readonly Font titleFont = new Font("Segoe UI", 16, FontStyle.Bold);
        private static readonly Font dateFont = new Font("Segoe UI", 8);
        private static readonly Pen gridPen = new Pen(Color.Gray, 1);

        private PrintDocument printDoc = new PrintDocument();
        private PrintPreviewDialog previewDlg = new PrintPreviewDialog();
        private DataGridView dgvToPrint;
        private int checkRow = 0;

        public frmRecords()
        {
            InitializeComponent();
            SetDataGridViewFormats();
            InitializeSearchTimer();
            printDoc.PrintPage += new PrintPageEventHandler(PrintDocument_PrintPage);
        }

        // Fixed UI Helper to prevent invocation on disposed handle
        private void UI(Action a)
        {
            if (IsDisposed || !IsHandleCreated) return;
            if (InvokeRequired) BeginInvoke(a);
            else a();
        }

        private void InitializeSearchTimer()
        {
            searchDelayTimer = new System.Windows.Forms.Timer();
            searchDelayTimer.Interval = 400;
            searchDelayTimer.Tick += (s, e) =>
            {
                searchDelayTimer.Stop();
                if (tabControl1.SelectedTab.Name == "tabPageInventory" || tabControl1.SelectedTab.Text == "Inventory List")
                {
                    _ = LoadInventoryAsync();
                }
                else if (tabControl1.SelectedTab.Name == "tabPageCancelled" || tabControl1.SelectedTab.Text == "Cancelled Order")
                {
                    _ = LoadCancelledOrdersAsync();
                }
            };
        }

        private void TriggerSearch()
        {
            searchDelayTimer.Stop();
            searchDelayTimer.Start();
        }

        private void frmRecords_Load(object sender, EventArgs e)
        {
            try
            {
                if (cdTopSelling.Items.Count > 0) cdTopSelling.SelectedIndex = 0;
                if (cbCriticle.Items.Count > 0) cbCriticle.SelectedIndex = 0;
            }
            catch (Exception ex) { MessageBox.Show(ex.Message, stitle); }

            LoadCategoryFilter(cbCategory);
            LoadCategoryFilter(cbcategoryinventorysearch);

            _ = LoadAllAsync();
        }

        public void LoadCategoryFilter(ComboBox combo)
        {
            try
            {
                if (combo == null) return;
                combo.Items.Clear();
                combo.Items.Add("All Categories");
                using (var cn = new SQLiteConnection(DBConnection.MyConnection()))
                {
                    cn.Open();
                    using (var cmd = new SQLiteCommand("SELECT category FROM TblCategory ORDER BY category ASC", cn))
                    using (var reader = cmd.ExecuteReader())
                        while (reader.Read()) combo.Items.Add(reader["category"].ToString());
                }
                combo.SelectedIndex = 0;
            }
            catch (Exception ex) { MessageBox.Show(ex.Message, stitle); }
        }

        private async Task LoadAllAsync()
        {
            try
            {
                await Task.WhenAll(
                    LoadStockHistoryAsync(),
                    LoadInventoryAsync(),
                    LoadCancelledOrdersAsync()
                ).ConfigureAwait(false);
            }
            catch (Exception ex) { Console.WriteLine("Error: " + ex.Message); }
        }

        private void SetDataGridViewFormats()
        {
            DataGridView[] grids = { dataGridView1, dataGridView2, dataGridView3, dataGridView4, dataGridView5, dataGridView6 };
            foreach (var dgv in grids)
            {
                if (dgv == null) continue;
                dgv.ReadOnly = true;
                dgv.AllowUserToAddRows = false;
                dgv.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
                dgv.BackgroundColor = Color.White;
                dgv.RowHeadersVisible = false;
                dgv.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(238, 239, 249);

                typeof(DataGridView).InvokeMember("DoubleBuffered", System.Reflection.BindingFlags.NonPublic |
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.SetProperty, null, dgv, new object[] { true });
            }
        }

        #region Optimized Data Loaders
        public async Task LoadTopSellingAsync() { await Task.Delay(1); }
        public async Task LoadSoldSummaryAsync() { await Task.Delay(1); }
        public async Task LoadCriticalItemsAsync() { await Task.Delay(1); }

        public async Task LoadStockHistoryAsync()
        {
            if (Interlocked.CompareExchange(ref _isStockLoading, 1, 0) != 0) return;

            _stockCts?.Cancel();
            _stockCts?.Dispose();
            _stockCts = new CancellationTokenSource();
            var token = _stockCts.Token;

            DateTime d1 = dateTimePicker8.Value.Date;
            DateTime d2 = dateTimePicker7.Value.Date;

            UI(() => {
                dataGridView6.SuspendLayout();
                dataGridView6.Rows.Clear();
                dataGridView6.ResumeLayout();
            });

            try
            {
                using var cn = new SQLiteConnection(DBConnection.MyConnection());
                await cn.OpenAsync(token);
                string sql = @"SELECT s.id, s.refno, s.pcode, p.pdesc, s.qty, s.sdate, s.stockinby 
                               FROM tblStockIn s 
                               INNER JOIN TblProduct1 p ON s.pcode = p.pcode 
                               WHERE s.sdate BETWEEN @d1 AND @d2 AND s.status = 'Done'
                               ORDER BY s.sdate DESC";

                using var cmd = new SQLiteCommand(sql, cn);
                cmd.CommandTimeout = 5;
                cmd.Parameters.Add("@d1", DbType.Date).Value = d1;
                cmd.Parameters.Add("@d2", DbType.Date).Value = d2;

                using var reader = await cmd.ExecuteReaderAsync(token);
                int i = 0;
                var rows = new List<object[]>();
                while (await reader.ReadAsync(token))
                {
                    rows.Add(new object[] { ++i, reader["id"], reader["refno"], reader["pcode"], reader["pdesc"], reader["qty"], reader["sdate"], reader["stockinby"] });
                }

                if (!token.IsCancellationRequested) UI(() => { foreach (var row in rows) dataGridView6.Rows.Add(row); });
            }
            catch (OperationCanceledException) { }
            catch (Exception ex) { MessageBox.Show(ex.Message, stitle); }
            finally { Interlocked.Exchange(ref _isStockLoading, 0); }
        }

        public async Task LoadInventoryAsync()
        {
            string currentSearch = textboxinventorysearch.Text + cbcategoryinventorysearch.Text;
            if (currentSearch == _lastInventorySearch && _isInventoryLoading == 1) return;

            if (Interlocked.CompareExchange(ref _isInventoryLoading, 1, 0) != 0) return;

            _inventoryCts?.Cancel();
            _inventoryCts?.Dispose();
            _inventoryCts = new CancellationTokenSource();
            var token = _inventoryCts.Token;

            string search = textboxinventorysearch.Text + "%";
            string cat = cbcategoryinventorysearch.Text;

            UI(() => {
                dataGridView4.SuspendLayout();
                dataGridView4.Rows.Clear();
                dataGridView4.ResumeLayout();
            });

            try
            {
                using var cn = new SQLiteConnection(DBConnection.MyConnection());
                await cn.OpenAsync(token);
                string sql = "SELECT p.pcode, p.barcode, p.pdesc, b.brand, c.category, p.price, p.reorder, p.qty FROM TblProduct1 p LEFT JOIN BrandTbl b ON p.bid=b.id LEFT JOIN TblCategory c ON p.cid=c.id WHERE (p.pdesc LIKE @search OR p.pcode LIKE @search)";
                if (cat != "All Categories") sql += " AND c.category=@category";

                using var cmd = new SQLiteCommand(sql, cn);
                cmd.CommandTimeout = 5;
                cmd.Parameters.AddWithValue("@search", search);
                if (cat != "All Categories") cmd.Parameters.AddWithValue("@category", cat);

                using var reader = await cmd.ExecuteReaderAsync(token);
                int i = 0;
                var rows = new List<object[]>();
                while (await reader.ReadAsync(token))
                {
                    rows.Add(new object[] { ++i, reader["pcode"], reader["barcode"], reader["pdesc"], reader["brand"], reader["category"], reader["price"], reader["reorder"], reader["qty"] });
                }

                if (!token.IsCancellationRequested)
                {
                    UI(() => { foreach (var row in rows) dataGridView4.Rows.Add(row); });
                    _lastInventorySearch = currentSearch;
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception ex) { MessageBox.Show(ex.Message, stitle); }
            finally { Interlocked.Exchange(ref _isInventoryLoading, 0); }
        }

        public async Task LoadCancelledOrdersAsync()
        {
            string currentSearch = textsearchTransNo.Text + dateTimePicker5.Value + dateTimePicker6.Value;
            if (currentSearch == _lastCancelSearch && _isCancelLoading == 1) return;

            if (Interlocked.CompareExchange(ref _isCancelLoading, 1, 0) != 0) return;

            _cancelCts?.Cancel();
            _cancelCts?.Dispose();
            _cancelCts = new CancellationTokenSource();
            var token = _cancelCts.Token;

            DateTime d1 = dateTimePicker5.Value.Date;
            DateTime d2 = dateTimePicker6.Value.Date;
            string tn = textsearchTransNo.Text + "%";

            UI(() => {
                dataGridView5.SuspendLayout();
                dataGridView5.Rows.Clear();
                dataGridView5.ResumeLayout();
            });

            try
            {
                using var cn = new SQLiteConnection(DBConnection.MyConnection());
                await cn.OpenAsync(token);
                string sql = @"SELECT c.transno, c.pcode, p.pdesc, c.price, c.qty, c.total, c.sdate, c.voidby, c.cancelledby, c.reason 
                               FROM tblCancel c 
                               LEFT JOIN TblProduct1 p ON c.pcode = p.pcode 
                               WHERE c.sdate BETWEEN @d1 AND @d2 AND c.transno LIKE @tn";

                using var cmd = new SQLiteCommand(sql, cn);
                cmd.CommandTimeout = 5;
                cmd.Parameters.Add("@d1", DbType.Date).Value = d1;
                cmd.Parameters.Add("@d2", DbType.Date).Value = d2;
                cmd.Parameters.AddWithValue("@tn", tn);

                using var reader = await cmd.ExecuteReaderAsync(token);
                int i = 0;
                var rows = new List<object[]>();
                while (await reader.ReadAsync(token))
                {
                    rows.Add(new object[] { ++i, reader["transno"], reader["pcode"], reader["pdesc"], reader["price"], reader["qty"], reader["total"], reader["sdate"], reader["voidby"], reader["cancelledby"], reader["reason"], "Print Slip" });
                }

                if (!token.IsCancellationRequested)
                {
                    UI(() => { foreach (var row in rows) dataGridView5.Rows.Add(row); });
                    _lastCancelSearch = currentSearch;
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception ex) { MessageBox.Show(ex.Message, stitle); }
            finally { Interlocked.Exchange(ref _isCancelLoading, 0); }
        }
        #endregion

        #region Event Handlers
        private void textboxinventorysearch_TextChanged(object sender, EventArgs e) => TriggerSearch();
        private void textsearchTransNo_TextChanged(object sender, EventArgs e) => TriggerSearch();

        private void linkLabel8_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e) { _ = LoadStockHistoryAsync(); }

        private void linkLabel_PrintStock_Click(object sender, LinkLabelLinkClickedEventArgs e)
        {
            PrintGrid(dataGridView6, true);
        }

        private void linkLabel5_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e) { _ = LoadTopSellingAsync(); }
        private void linkLabel9_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e) { _ = LoadCancelledOrdersAsync(); }
        private void linkLabel12_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e) { _ = LoadInventoryAsync(); }
        private void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e) { PrintGrid(dataGridView4, false); }
        private void linkLabel10_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e) { PrintGrid(dataGridView5, true); }

        private void linkLabel7_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e) { }
        private void linkLabel4_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e) { }
        private void linkLabel6_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e) { }
        private void linkLabel3_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e) { }

        private void dataGridView5_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;

            string colName = dataGridView5.Columns[e.ColumnIndex].Name;
            if (colName == "Print Slip")
            {
                // Logic to call PrintCancellationInvoice
            }
        }

        private void PrintGrid(DataGridView dgv, bool isLandscape)
        {
            if (dgv == null || dgv.Rows.Count == 0) return;
            dgvToPrint = dgv;
            checkRow = 0;
            printDoc.DefaultPageSettings.Landscape = isLandscape;
            printDoc.DefaultPageSettings.Margins = new Margins(30, 30, 30, 30);

            using (var dlg = new PrintPreviewDialog())
            {
                dlg.Document = printDoc;
                dlg.WindowState = FormWindowState.Maximized;
                dlg.ShowDialog();
            }
        }
        #endregion

        #region Printing Engine
        private void PrintDocument_PrintPage(object sender, PrintPageEventArgs e)
        {
            if (dgvToPrint == null) return;
            Graphics g = e.Graphics;
            float x = e.MarginBounds.Left;
            float y = e.MarginBounds.Top;
            float cellHeight = 28;

            string reportName = "REPORT";
            if (dgvToPrint == dataGridView5) reportName = "CANCELLED ORDERS HISTORY";
            if (dgvToPrint == dataGridView6) reportName = "STOCK IN HISTORY";

            g.DrawString(reportName, titleFont, Brushes.DarkSlateGray, x, y);
            g.DrawString("Printed on: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm"), dateFont, Brushes.Black, x, y + 25);
            y += 60;

            float totalGridWidth = 0;
            foreach (DataGridViewColumn col in dgvToPrint.Columns)
                if (col.Visible && col.HeaderText != "Print Slip" && col.HeaderText != "ACTION") totalGridWidth += col.Width;

            if (totalGridWidth == 0) return;

            float scale = (float)e.MarginBounds.Width / totalGridWidth;

            float curX = x;
            foreach (DataGridViewColumn col in dgvToPrint.Columns)
            {
                if (col.Visible && col.HeaderText != "Print Slip" && col.HeaderText != "ACTION")
                {
                    float pWidth = col.Width * scale;
                    g.FillRectangle(Brushes.LightGray, curX, y, pWidth, cellHeight);
                    g.DrawRectangle(Pens.Black, curX, y, pWidth, cellHeight);
                    g.DrawString(col.HeaderText, hFont, Brushes.Black, new RectangleF(curX + 2, y + 5, pWidth - 4, cellHeight));
                    curX += pWidth;
                }
            }
            y += cellHeight;

            for (int i = checkRow; i < dgvToPrint.Rows.Count; i++)
            {
                DataGridViewRow row = dgvToPrint.Rows[i];
                curX = x;
                foreach (DataGridViewCell cell in row.Cells)
                {
                    if (cell.OwningColumn.Visible && cell.OwningColumn.HeaderText != "Print Slip" && cell.OwningColumn.HeaderText != "ACTION")
                    {
                        float pWidth = cell.OwningColumn.Width * scale;
                        g.DrawRectangle(gridPen, curX, y, pWidth, cellHeight);
                        string val = cell.Value?.ToString() ?? "";
                        g.DrawString(val, cFont, Brushes.Black, new RectangleF(curX + 2, y + 5, pWidth - 4, cellHeight));
                        curX += pWidth;
                    }
                }
                y += cellHeight;
                checkRow++;

                if (y > e.MarginBounds.Bottom - 20)
                {
                    e.HasMorePages = true;
                    return;
                }
            }
            checkRow = 0;
        }

        public void PrintCancellationInvoice(string transno, string pcode, string pdesc, string price, string qty, string total, string user, string reason, string date)
        {
            string store = "POS SYSTEM", addr = "";
            try
            {
                using var cn = new SQLiteConnection(DBConnection.MyConnection());
                cn.Open();
                using var cmd = new SQLiteCommand("SELECT store, address FROM tblStore", cn);
                using var dr = cmd.ExecuteReader();
                if (dr.Read()) { store = dr["store"].ToString().ToUpper(); addr = dr["address"].ToString(); }
            }
            catch (Exception ex) { MessageBox.Show(ex.Message, stitle); }

            PrintDocument doc = new PrintDocument();
            doc.PrintPage += (s, ev) => {
                Graphics g = ev.Graphics;
                float x = ev.MarginBounds.Left; float y = ev.MarginBounds.Top;
                using Font sFont = new Font("Arial", 18, FontStyle.Bold);
                using Font aFont = new Font("Arial", 9);
                using Font vFont = new Font("Arial", 11, FontStyle.Bold);
                using Font rFont = new Font("Arial", 9, FontStyle.Italic);

                g.DrawString(store, sFont, Brushes.Black, x, y);
                y += 30; g.DrawString(addr, aFont, Brushes.Black, x, y);
                y += 40; g.DrawString($"VOID INVOICE: {transno}", vFont, Brushes.Red, x, y);
                y += 25; g.DrawString($"Item: {pdesc} | Qty: {qty} | Total: {total}", aFont, Brushes.Black, x, y);
                y += 30; g.DrawString($"Reason: {reason}", rFont, Brushes.Black, x, y);
            };

            using (var dlg = new PrintPreviewDialog())
            {
                dlg.Document = doc;
                dlg.WindowState = FormWindowState.Maximized;
                dlg.ShowDialog();
            }
        }
        #endregion

        private void pictureBox2_Click(object sender, EventArgs e) { this.Dispose(); }
    }
}