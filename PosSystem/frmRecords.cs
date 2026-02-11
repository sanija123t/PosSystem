using System;
using System.Data;
using System.Data.SQLite;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PosSystem
{
    public partial class frmRecords : Form
    {
        private readonly string stitle = "PosSystem";

        public frmRecords()
        {
            InitializeComponent();
            SetDataGridViewFormats();
        }

        private void frmRecords_Load(object sender, EventArgs e)
        {
            // Check top-selling dropdown on load
            if (string.IsNullOrWhiteSpace(cdTopSelling.Text))
            {
                MessageBox.Show("Please select a criteria from the Top Selling dropdown before loading records.", stitle,
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            _ = LoadAllAsync();
        }

        private async Task LoadAllAsync()
        {
            try
            {
                Cursor = Cursors.WaitCursor;
                await Task.WhenAll(
                    LoadTopSellingAsync(),
                    LoadSoldSummaryAsync(),
                    LoadCriticalItemsAsync(),
                    LoadInventoryAsync(),
                    LoadCancelledOrdersAsync(),
                    LoadStockHistoryAsync()
                ).ConfigureAwait(false);
            }
            finally
            {
                Cursor = Cursors.Default;
            }
        }

        private void SetDataGridViewFormats()
        {
            // Top Selling
            if (dataGridView1.Columns.Count >= 5)
                dataGridView1.Columns[4].DefaultCellStyle.Format = "#,##0.00";

            // Sold Summary
            if (dataGridView2.Columns.Count >= 7)
            {
                dataGridView2.Columns[3].DefaultCellStyle.Format = "#,##0.00"; // price
                dataGridView2.Columns[6].DefaultCellStyle.Format = "#,##0.00"; // total
            }

            // Inventory
            if (dataGridView4.Columns.Count >= 8)
            {
                dataGridView4.Columns[5].DefaultCellStyle.Format = "#,##0.00"; // price
                dataGridView4.Columns[7].DefaultCellStyle.Format = "#,##0";   // reorder
                dataGridView4.Columns[6].DefaultCellStyle.Format = "#,##0";   // qty
            }

            // Critical Items
            if (dataGridView3.Columns.Count >= 8)
            {
                dataGridView3.Columns[5].DefaultCellStyle.Format = "#,##0.00"; // price
                dataGridView3.Columns[6].DefaultCellStyle.Format = "#,##0";   // qty
                dataGridView3.Columns[7].DefaultCellStyle.Format = "#,##0";   // reorder
            }

            // Cancelled Orders
            if (dataGridView5.Columns.Count >= 7)
                dataGridView5.Columns[4].DefaultCellStyle.Format = "#,##0.00"; // price/total

            // Stock History
            if (dataGridView6.Columns.Count >= 5)
                dataGridView6.Columns[3].DefaultCellStyle.Format = "#,##0"; // qty
        }

        #region Top Selling
        public async Task LoadTopSellingAsync()
        {
            if (string.IsNullOrWhiteSpace(cdTopSelling.Text))
            {
                MessageBox.Show("Please select a Top Selling criteria from the dropdown.", stitle, MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                return;
            }

            dataGridView1.Rows.Clear();
            int i = 0;

            DateTime startDate = dateTimePicker1.Value.Date;
            DateTime endDate = dateTimePicker2.Value.Date.AddDays(1).AddSeconds(-1);

            try
            {
                using var cn = new SQLiteConnection(DBConnection.MyConnection());
                await cn.OpenAsync().ConfigureAwait(false);

                string orderBy = cdTopSelling.Text.Contains("Qty") ? "total_qty" : "total_amount";

                string query = $@"
                    SELECT c.pcode, p.pdesc,
                           SUM(c.qty) AS total_qty,
                           SUM(c.total) AS total_amount
                    FROM tblCart1 c
                    INNER JOIN TblProduct1 p ON c.pcode = p.pcode
                    WHERE c.status='Sold' AND sdate BETWEEN @d1 AND @d2
                    GROUP BY c.pcode, p.pdesc
                    ORDER BY {orderBy} DESC
                    LIMIT 10";

                using var cmd = new SQLiteCommand(query, cn);
                cmd.Parameters.AddWithValue("@d1", startDate);
                cmd.Parameters.AddWithValue("@d2", endDate);

                using var reader = await cmd.ExecuteReaderAsync().ConfigureAwait(false);
                while (await reader.ReadAsync().ConfigureAwait(false))
                {
                    i++;
                    double total = reader["total_amount"] != DBNull.Value ? Convert.ToDouble(reader["total_amount"]) : 0;
                    int qty = reader["total_qty"] != DBNull.Value ? Convert.ToInt32(reader["total_qty"]) : 0;

                    dataGridView1.Rows.Add(i, reader["pcode"], reader["pdesc"], qty, total);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Top Selling Load Error: " + ex.Message, stitle, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        #endregion

        #region Sold Items Summary
        public async Task LoadSoldSummaryAsync()
        {
            dataGridView2.Rows.Clear();
            int i = 0;
            double totalAmount = 0;

            DateTime startDate = dateTimePicker4.Value.Date;
            DateTime endDate = dateTimePicker3.Value.Date.AddDays(1).AddSeconds(-1);

            try
            {
                using var cn = new SQLiteConnection(DBConnection.MyConnection());
                await cn.OpenAsync().ConfigureAwait(false);

                string query = @"
                    SELECT c.pcode, p.pdesc, c.price,
                           SUM(c.qty) AS tot_qty,
                           SUM(c.disc) AS tot_disc,
                           SUM(c.total) AS total
                    FROM tblCart1 c
                    INNER JOIN TblProduct1 p ON c.pcode = p.pcode
                    WHERE c.status='Sold' AND sdate BETWEEN @d1 AND @d2
                    GROUP BY c.pcode, p.pdesc, c.price";

                using var cmd = new SQLiteCommand(query, cn);
                cmd.Parameters.AddWithValue("@d1", startDate);
                cmd.Parameters.AddWithValue("@d2", endDate);

                using var reader = await cmd.ExecuteReaderAsync().ConfigureAwait(false);
                while (await reader.ReadAsync().ConfigureAwait(false))
                {
                    i++;
                    double total = reader["total"] != DBNull.Value ? Convert.ToDouble(reader["total"]) : 0;
                    double price = reader["price"] != DBNull.Value ? Convert.ToDouble(reader["price"]) : 0;
                    double qty = reader["tot_qty"] != DBNull.Value ? Convert.ToDouble(reader["tot_qty"]) : 0;
                    double disc = reader["tot_disc"] != DBNull.Value ? Convert.ToDouble(reader["tot_disc"]) : 0;

                    totalAmount += total;

                    dataGridView2.Rows.Add(i, reader["pcode"], reader["pdesc"], price, qty, disc, total);
                }

                lblTotal.Text = totalAmount.ToString("#,##0.00");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Sold Summary Load Error: " + ex.Message, stitle, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
        #endregion

        #region Critical Items
        public async Task LoadCriticalItemsAsync()
        {
            dataGridView3.Rows.Clear();
            int i = 0;

            try
            {
                using var cn = new SQLiteConnection(DBConnection.MyConnection());
                await cn.OpenAsync().ConfigureAwait(false);

                string query = @"
                    SELECT p.pcode, p.pdesc, b.brand, c.category, p.price, p.qty, p.reorder,
                           CASE WHEN p.qty <= p.reorder THEN 'Critical' ELSE 'OK' END AS status
                    FROM TblProduct1 p
                    LEFT JOIN BrandTbl b ON p.bid = b.id
                    LEFT JOIN TblCategory c ON p.cid = c.id
                    ORDER BY status DESC, p.pdesc";

                using var cmd = new SQLiteCommand(query, cn);
                using var reader = await cmd.ExecuteReaderAsync().ConfigureAwait(false);
                while (await reader.ReadAsync().ConfigureAwait(false))
                {
                    i++;
                    double price = reader["price"] != DBNull.Value ? Convert.ToDouble(reader["price"]) : 0;
                    int qty = reader["qty"] != DBNull.Value ? Convert.ToInt32(reader["qty"]) : 0;
                    int reorder = reader["reorder"] != DBNull.Value ? Convert.ToInt32(reader["reorder"]) : 0;

                    dataGridView3.Rows.Add(i, reader["pcode"], reader["pdesc"], reader["brand"], reader["category"], price, qty, reorder, reader["status"]);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Critical Items Load Error: " + ex.Message, stitle, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
        #endregion

        #region Inventory
        public async Task LoadInventoryAsync()
        {
            dataGridView4.Rows.Clear();
            int i = 0;

            try
            {
                using var cn = new SQLiteConnection(DBConnection.MyConnection());
                await cn.OpenAsync().ConfigureAwait(false);

                string query = @"
                    SELECT p.pcode, p.barcode, p.pdesc, b.brand, c.category, p.price, p.qty, p.reorder
                    FROM TblProduct1 p
                    LEFT JOIN BrandTbl b ON p.bid = b.id
                    LEFT JOIN TblCategory c ON p.cid = c.id
                    ORDER BY p.pdesc";

                using var cmd = new SQLiteCommand(query, cn);
                using var reader = await cmd.ExecuteReaderAsync().ConfigureAwait(false);
                while (await reader.ReadAsync().ConfigureAwait(false))
                {
                    i++;
                    double price = reader["price"] != DBNull.Value ? Convert.ToDouble(reader["price"]) : 0;
                    int qty = reader["qty"] != DBNull.Value ? Convert.ToInt32(reader["qty"]) : 0;
                    int reorder = reader["reorder"] != DBNull.Value ? Convert.ToInt32(reader["reorder"]) : 0;

                    dataGridView4.Rows.Add(i, reader["pcode"], reader["barcode"], reader["pdesc"], reader["brand"], reader["category"], price, reorder, qty);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Inventory Load Error: " + ex.Message, stitle, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        #endregion

        #region Cancelled Orders
        public async Task LoadCancelledOrdersAsync()
        {
            dataGridView5.Rows.Clear();
            int i = 0;

            DateTime startDate = dateTimePicker5.Value.Date;
            DateTime endDate = dateTimePicker6.Value.Date.AddDays(1).AddSeconds(-1);

            try
            {
                using var cn = new SQLiteConnection(DBConnection.MyConnection());
                await cn.OpenAsync().ConfigureAwait(false);

                string query = @"
                    SELECT transno, pcode, pdesc, price, qty, total, sdate,
                           voidby, cancelledby, reason, action
                    FROM tblCancel
                    WHERE sdate BETWEEN @d1 AND @d2
                    ORDER BY sdate DESC";

                using var cmd = new SQLiteCommand(query, cn);
                cmd.Parameters.AddWithValue("@d1", startDate);
                cmd.Parameters.AddWithValue("@d2", endDate);

                using var reader = await cmd.ExecuteReaderAsync().ConfigureAwait(false);
                while (await reader.ReadAsync().ConfigureAwait(false))
                {
                    i++;
                    double price = reader["price"] != DBNull.Value ? Convert.ToDouble(reader["price"]) : 0;
                    double total = reader["total"] != DBNull.Value ? Convert.ToDouble(reader["total"]) : 0;
                    int qty = reader["qty"] != DBNull.Value ? Convert.ToInt32(reader["qty"]) : 0;

                    dataGridView5.Rows.Add(i, reader["transno"], reader["pcode"], reader["pdesc"], price, qty, total,
                        reader["sdate"], reader["voidby"], reader["cancelledby"], reader["reason"], reader["action"]);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Cancelled Orders Load Error: " + ex.Message, stitle, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        #endregion

        #region Stock History
        public async Task LoadStockHistoryAsync()
        {
            dataGridView6.Rows.Clear();
            int i = 0;

            DateTime startDate = dateTimePicker8.Value.Date;
            DateTime endDate = dateTimePicker7.Value.Date.AddDays(1).AddSeconds(-1);

            try
            {
                using var cn = new SQLiteConnection(DBConnection.MyConnection());
                await cn.OpenAsync().ConfigureAwait(false);

                string query = @"
                    SELECT refno AS transno, pcode, pdesc, qty, action, sdate, [user]
                    FROM tblStockIn
                    WHERE sdate BETWEEN @d1 AND @d2 AND status='Done'
                    ORDER BY sdate DESC";

                using var cmd = new SQLiteCommand(query, cn);
                cmd.Parameters.AddWithValue("@d1", startDate);
                cmd.Parameters.AddWithValue("@d2", endDate);

                using var reader = await cmd.ExecuteReaderAsync().ConfigureAwait(false);
                while (await reader.ReadAsync().ConfigureAwait(false))
                {
                    i++;
                    int qty = reader["qty"] != DBNull.Value ? Convert.ToInt32(reader["qty"]) : 0;
                    dataGridView6.Rows.Add(i, reader["transno"], reader["pcode"], reader["pdesc"], qty,
                        reader["action"], reader["sdate"], reader["user"]);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Stock History Load Error: " + ex.Message, stitle, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        #endregion

        private void pictureBox2_Click(object sender, EventArgs e)
        {
            Dispose();
        }
    }
}