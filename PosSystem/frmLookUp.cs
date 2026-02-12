using System;
using System.Data.SQLite;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PosSystem
{
    public partial class frmLookUp : Form
    {
        // 🔥 Callback instead of direct form reference
        public Action<string, double, string, int> OnProductSelected;

        // 🔥 Debounce controller for search
        private CancellationTokenSource _searchCts;

        // Properties to get the selected item
        public string SelectedPCode { get; private set; }
        public string SelectedDescription { get; private set; }
        public decimal SelectedPrice { get; private set; }

        // ✅ Parameterless constructor (required)
        public frmLookUp()
        {
            InitializeComponent();
            KeyPreview = true;
        }

        // ✅ Optional: constructor with callback if you want to pass OnProductSelected directly
        public frmLookUp(Action<string, double, string, int> callback) : this()
        {
            OnProductSelected = callback;
        }

        // ✅ btnOK selects the row and closes the form
        private void btnOK_Click(object sender, EventArgs e)
        {
            if (dataGridView1.CurrentRow != null)
            {
                SelectedPCode = dataGridView1.CurrentRow.Cells["pcode"].Value.ToString();
                SelectedDescription = dataGridView1.CurrentRow.Cells["pdesc"].Value.ToString();
                SelectedPrice = Convert.ToDecimal(dataGridView1.CurrentRow.Cells["price"].Value);

                this.DialogResult = DialogResult.OK;
                this.Close();
            }
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
                        INNER JOIN TblCategory c ON c.id = p.cid
                        WHERE p.pdesc LIKE @search OR p.barcode LIKE @search";

                    using (var cm = new SQLiteCommand(query, cn))
                    {
                        cm.Parameters.AddWithValue("@search", "%" + search + "%");

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
                // Ignore, user typing
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        // 🔥 Debounced search input
        private async void txtSearch_TextChanged(object sender, EventArgs e)
        {
            if (_searchCts != null)
            {
                _searchCts.Cancel();
                _searchCts.Dispose();
            }

            _searchCts = new CancellationTokenSource();
            var token = _searchCts.Token;

            try
            {
                await Task.Delay(300, token); // debounce
                await LoadRecordsAsync(txtSearch.Text.Trim(), token);
            }
            catch (OperationCanceledException) { }
        }

        private void dataGridView1_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0) return;

            string colName = dataGridView1.Columns[e.ColumnIndex].Name;
            if (colName != "Select") return;

            try
            {
                string pcode = dataGridView1.Rows[e.RowIndex].Cells["pcode"].Value?.ToString();
                double.TryParse(dataGridView1.Rows[e.RowIndex].Cells["price"].Value?.ToString(), out double price);
                int.TryParse(dataGridView1.Rows[e.RowIndex].Cells["qty"].Value?.ToString(), out int qtyHand);

                // Call the callback if used
                OnProductSelected?.Invoke(pcode, price, DateTime.Now.ToString("yyyyMMddHHmmss"), qtyHand);

                Dispose(); // close lookup
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

        // ✅ Fixed missing method body
        private void txtSearch_Click(object sender, EventArgs e)
        {
            // optional: do something on click
        }
    }
}