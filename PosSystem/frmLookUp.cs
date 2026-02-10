using System;
using System.Data.SQLite;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PosSystem
{
    public partial class frmLookUp : Form
    {
        private readonly Form1 f;
        private readonly frmPOS fPOS;

        // 🔥 Debounce controller
        private CancellationTokenSource _searchCts;

        public frmLookUp(Form1 frm)
        {
            InitializeComponent();
            f = frm;
            KeyPreview = true;
        }

        public frmLookUp(frmPOS frm)
        {
            InitializeComponent();
            fPOS = frm;
            KeyPreview = true;
        }

        private void pictureBox2_Click(object sender, EventArgs e)
        {
            Dispose();
        }

        // 🔥 ASYNC + DEBOUNCED LOAD
        private async Task LoadRecordsAsync(string search, CancellationToken token)
        {
            try
            {
                dataGridView1.Rows.Clear();

                using (var cn = new SQLiteConnection(DBConnection.MyConnection()))
                {
                    await cn.OpenAsync(token);

                    string query = @"
                        SELECT p.pcode, p.barcode, p.pdesc, 
                               b.brand, c.category, p.price, p.qty
                        FROM TblProduct1 p
                        INNER JOIN BrandTbl b ON b.id = p.bid
                        INNER JOIN TblCatecory c ON c.id = p.cid
                        WHERE p.pdesc LIKE @search";

                    using (var cm = new SQLiteCommand(query, cn))
                    {
                        cm.Parameters.AddWithValue("@search", search + "%");

                        using (var dr = await cm.ExecuteReaderAsync(token))
                        {
                            int i = 0;
                            while (await dr.ReadAsync(token))
                            {
                                i++;
                                dataGridView1.Rows.Add(
                                    i,
                                    dr["pcode"].ToString(),
                                    dr["barcode"].ToString(),
                                    dr["pdesc"].ToString(),
                                    dr["brand"].ToString(),
                                    dr["category"].ToString(),
                                    dr["price"].ToString(),
                                    dr["qty"].ToString()
                                );
                            }
                        }
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // Expected when user keeps typing – ignore silently
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        // 🔥 DEBOUNCED SEARCH
        private async void txtSearch_TextChanged(object sender, EventArgs e)
        {
            _searchCts?.Cancel();
            _searchCts = new CancellationTokenSource();
            var token = _searchCts.Token;

            try
            {
                // Wait until user stops typing
                await Task.Delay(300, token);

                await LoadRecordsAsync(txtSearch.Text.Trim(), token);
            }
            catch (OperationCanceledException)
            {
                // Typing continued → previous request cancelled
            }
        }

        private void dataGridView1_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0) return;

            try
            {
                string colName = dataGridView1.Columns[e.ColumnIndex].Name;
                if (colName != "Select") return;

                // Decide parent safely
                frmQty frm = fPOS != null ? new frmQty(fPOS) : new frmQty(f);

                string pcode = dataGridView1.Rows[e.RowIndex].Cells["pcode"].Value?.ToString();

                double.TryParse(dataGridView1.Rows[e.RowIndex].Cells["price"].Value?.ToString(), out double price);
                int.TryParse(dataGridView1.Rows[e.RowIndex].Cells["qty"].Value?.ToString(), out int qtyHand);

                string transno = fPOS != null ? fPOS.lblTransno.Text : "0000";

                frm.ProductDetails(pcode, price, transno, qtyHand);
                frm.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Selection Error: " + ex.Message,
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void frmLookUp_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape)
                Dispose();
        }

        private void panel2_Paint(object sender, PaintEventArgs e) { }
        private void panel1_Paint(object sender, PaintEventArgs e) { }
        private void frmLookUp_KeyPress(object sender, KeyPressEventArgs e) { }
    }
}