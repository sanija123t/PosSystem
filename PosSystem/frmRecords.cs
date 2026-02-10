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
            // Run all loading tasks in parallel
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
                );
            }
            finally
            {
                Cursor = Cursors.Default;
            }
        }

        private void SetDataGridViewFormats()
        {
            // Top Selling
            dataGridView1.Columns[4].DefaultCellStyle.Format = "#,##0.00"; // total
            // Sold Summary
            dataGridView2.Columns[3].DefaultCellStyle.Format = "#,##0.00"; // price
            dataGridView2.Columns[6].DefaultCellStyle.Format = "#,##0.00"; // total
            // Inventory
            dataGridView4.Columns[5].DefaultCellStyle.Format = "#,##0.00"; // price
            // You can add others similarly
        }

        #region Top Selling
        public async Task LoadTopSellingAsync()
        {
            if (string.IsNullOrEmpty(cdTopSelling.Text))
            {
                MessageBox.Show("Please select a criteria from dropdown.", stitle,
                    MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                return;
            }

            dataGridView1.Rows.Clear();
            int i = 0;

            DateTime startDate = dateTimePicker1.Value.Date;
            DateTime endDate = dateTimePicker2.Value.Date.AddDays(1).AddSeconds(-1);

            try
            {
                using var cn = new SQLiteConnection(DBConnection.MyConnection());
                await cn.OpenAsync();

                string query = cdTopSelling.Text switch
                {
                    "Short by Qty" => @"
                        SELECT pcode, pdesc, IFNULL(SUM(qty),0) AS qty, IFNULL(SUM(total),0) AS total
                        FROM vwSoldItems
                        WHERE sdate BETWEEN @d1 AND @d2 AND status = 'sold'
                        GROUP BY pcode, pdesc
                        ORDER BY qty DESC
                        LIMIT 10",
                    "Short by Total Amount" => @"
                        SELECT pcode, pdesc, IFNULL(SUM(qty),0) AS qty, IFNULL(SUM(total),0) AS total
                        FROM vwSoldItems
                        WHERE sdate BETWEEN @d1 AND @d2 AND status = 'sold'
                        GROUP BY pcode, pdesc
                        ORDER BY total DESC
                        LIMIT 10",
                    _ => ""
                };

                using var cmd = new SQLiteCommand(query, cn);
                cmd.Parameters.AddWithValue("@d1", startDate);
                cmd.Parameters.AddWithValue("@d2", endDate);

                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    i++;
                    double total = reader["total"] != DBNull.Value ? Convert.ToDouble(reader["total"]) : 0;

                    dataGridView1.Rows.Add(i, reader["pcode"], reader["pdesc"], reader["qty"], total);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, stitle, MessageBoxButtons.OK, MessageBoxIcon.Error);
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
                await cn.OpenAsync();

                string query = @"
                    SELECT c.pcode, p.pdesc, c.price,
                           SUM(c.qty) AS tot_qty,
                           SUM(c.disc) AS tot_disc,
                           SUM(c.total) AS total
                    FROM tblCart1 AS c
                    INNER JOIN TblProduct1 AS p ON c.pcode = p.pcode
                    WHERE status = 'Sold' AND sdate BETWEEN @d1 AND @d2
                    GROUP BY c.pcode, p.pdesc, c.price";

                using var cmd = new SQLiteCommand(query, cn);
                cmd.Parameters.AddWithValue("@d1", startDate);
                cmd.Parameters.AddWithValue("@d2", endDate);

                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    i++;
                    double total = reader["total"] != DBNull.Value ? Convert.ToDouble(reader["total"]) : 0;
                    totalAmount += total;

                    dataGridView2.Rows.Add(i, reader["pcode"], reader["pdesc"],
                        Convert.ToDouble(reader["price"]),
                        reader["tot_qty"], reader["tot_disc"], total);
                }

                lblTotal.Text = totalAmount.ToString("#,##0.00");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, stitle, MessageBoxButtons.OK, MessageBoxIcon.Warning);
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
                await cn.OpenAsync();

                using var cmd = new SQLiteCommand("SELECT * FROM vwCriticalItems", cn);
                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    i++;
                    dataGridView3.Rows.Add(i, reader[0], reader[1], reader[2], reader[3],
                        reader[4], reader[5], reader[6], reader[7]);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, stitle, MessageBoxButtons.OK, MessageBoxIcon.Warning);
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
                await cn.OpenAsync();

                string query = @"
                    SELECT p.pcode, p.barcode, p.pdesc, b.brand, c.category, p.price, p.qty, p.reorder
                    FROM TblProduct1 AS p
                    INNER JOIN BrandTbl AS b ON p.bid = b.id
                    INNER JOIN TblCategory AS c ON p.cid = c.id";

                using var cmd = new SQLiteCommand(query, cn);
                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    i++;
                    dataGridView4.Rows.Add(i, reader["pcode"], reader["barcode"], reader["pdesc"],
                        reader["brand"], reader["category"], reader["price"], reader["reorder"], reader["qty"]);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, stitle, MessageBoxButtons.OK, MessageBoxIcon.Error);
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
                await cn.OpenAsync();

                string query = @"SELECT * FROM vwCancelledOrder WHERE sdate BETWEEN @d1 AND @d2";

                using var cmd = new SQLiteCommand(query, cn);
                cmd.Parameters.AddWithValue("@d1", startDate);
                cmd.Parameters.AddWithValue("@d2", endDate);

                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    i++;
                    dataGridView5.Rows.Add(i, reader["transno"], reader["pcode"], reader["pdesc"],
                        reader["price"], reader["qty"], reader["total"], reader["sdate"],
                        reader["voidby"], reader["cancelledby"], reader["reason"], reader["action"]);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, stitle, MessageBoxButtons.OK, MessageBoxIcon.Error);
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
                await cn.OpenAsync();

                string query = @"SELECT * FROM tblStockIn WHERE sdate BETWEEN @d1 AND @d2 AND status = 'Done'";

                using var cmd = new SQLiteCommand(query, cn);
                cmd.Parameters.AddWithValue("@d1", startDate);
                cmd.Parameters.AddWithValue("@d2", endDate);

                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    i++;
                    dataGridView6.Rows.Add(i, reader[0], reader[1], reader[2],
                        reader[3], reader[4], reader[5], reader[6]);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, stitle, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        #endregion

        private void pictureBox2_Click(object sender, EventArgs e)
        {
            Dispose();
        }

    }
}