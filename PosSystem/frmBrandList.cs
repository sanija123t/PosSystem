using System;
using System.Data;
using System.Data.SQLite;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Drawing;

namespace PosSystem
{
    public partial class frmBrandList : Form
    {
        // ENTERPRISE: Connection string pulled once for efficiency
        private readonly string _connectionString = DBConnection.MyConnection();
        private CancellationTokenSource searchTokenSource;

        public frmBrandList()
        {
            InitializeComponent();
            this.StartPosition = FormStartPosition.CenterScreen;
            // Async load records on form load
            _ = LoadRecordsAsync();
        }

        // ============================================================
        // 🔹 ASYNC LOAD RECORDS (Enterprise Implementation)
        // ============================================================
        public async Task LoadRecordsAsync(string search = "")
        {
            try
            {
                // ENTERPRISE: Suspend UI updates to improve rendering speed and prevent flicker
                dataGridView1.SuspendLayout();
                int i = 0;
                dataGridView1.Rows.Clear();

                using (SQLiteConnection cn = new SQLiteConnection(_connectionString))
                {
                    await cn.OpenAsync();

                    // ENTERPRISE: Optimized query with Case-Insensitive search
                    string sql = "SELECT id, brand FROM BrandTbl WHERE brand LIKE @search ORDER BY brand ASC";
                    using (SQLiteCommand cm = new SQLiteCommand(sql, cn))
                    {
                        cm.Parameters.AddWithValue("@search", $"%{search}%");

                        using (var dr = await cm.ExecuteReaderAsync())
                        {
                            while (await dr.ReadAsync())
                            {
                                i++;
                                dataGridView1.Rows.Add(
                                  i,
                                  dr["id"].ToString(),
                                  dr["brand"].ToString()
                                );
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Critical Error loading registry:\n{ex.Message}", "Enterprise System Error",
                  MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                // ENTERPRISE: Resume UI layout
                dataGridView1.ResumeLayout();
            }
        }

        // ============================================================
        // 🔹 ADD NEW BRAND
        // ============================================================
        private void pictureBox3_Click(object sender, EventArgs e)
        {
            frmBrand frm = new frmBrand(this);
            // ENTERPRISE: Direct access to control states if they are public
            frm.button1.Enabled = true;  // Save
            frm.button2.Enabled = false; // Update
            frm.ShowDialog();
        }

        // ============================================================
        // 🔹 EDIT / DELETE BRAND
        // ============================================================
        private async void dataGridView1_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;

            string colName = dataGridView1.Columns[e.ColumnIndex].Name;

            // Safe null-check for cell values
            string brandId = dataGridView1.Rows[e.RowIndex].Cells[1].Value?.ToString() ?? "";
            string brandName = dataGridView1.Rows[e.RowIndex].Cells[2].Value?.ToString() ?? "";

            try
            {
                if (colName == "Edit")
                {
                    frmBrand frm = new frmBrand(this);
                    frm.lblId.Text = brandId;
                    frm.txtBrand.Text = brandName;
                    frm.button1.Enabled = false;
                    frm.button2.Enabled = true;
                    frm.ShowDialog();
                }
                else if (colName == "Delete")
                {
                    // ENTERPRISE: Professional confirmation icons
                    if (MessageBox.Show($"Are you sure you want to permanently delete brand '{brandName}'?",
                      "Confirm Deletion", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
                    {
                        await DeleteBrandAsync(brandId);
                        await LoadRecordsAsync(txtSearch.Text.Trim());

                        MessageBox.Show($"Brand record for '{brandName}' has been purged.", "Action Successful",
                          MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Workflow Error:\n{ex.Message}", "System Error",
                  MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // ============================================================
        // 🔹 DELETE BRAND ASYNC
        // ============================================================
        private async Task DeleteBrandAsync(string brandId)
        {
            using (SQLiteConnection cn = new SQLiteConnection(_connectionString))
            {
                await cn.OpenAsync();
                using (var transaction = cn.BeginTransaction())
                {
                    using (SQLiteCommand cm = new SQLiteCommand("DELETE FROM BrandTbl WHERE id = @id", cn, transaction))
                    {
                        cm.Parameters.AddWithValue("@id", brandId);
                        await cm.ExecuteNonQueryAsync();
                    }
                    transaction.Commit();
                }
            }
        }

        // ============================================================
        // 🔹 CLOSE FORM
        // ============================================================
        private void pictureBox2_Click(object sender, EventArgs e)
        {
            this.Dispose();
        }

        // ============================================================
        // 🔹 SEARCH BRAND WITH ENTERPRISE DEBOUNCE
        // ============================================================
        private async void txtSearch_TextChanged(object sender, EventArgs e)
        {
            // ENTERPRISE: Proper cleanup of previous tokens to prevent memory leaks and "ghost" searches
            if (searchTokenSource != null)
            {
                searchTokenSource.Cancel();
                searchTokenSource.Dispose();
            }

            searchTokenSource = new CancellationTokenSource();
            CancellationToken token = searchTokenSource.Token;

            try
            {
                // ENTERPRISE: 350ms delay prevents the database from being hammered while typing fast
                await Task.Delay(350, token);

                if (!token.IsCancellationRequested)
                {
                    await LoadRecordsAsync(txtSearch.Text.Trim());
                }
            }
            catch (TaskCanceledException)
            {
                // Ignored: Expected when the user is typing faster than the debounce delay
            }
        }

        // ------------------------------------------------------------
        // 🔹 JUNK LINES (Preserved for Designer Compatibility)
        // ------------------------------------------------------------
        private void txtSearch_TextChanged_1(object sender, EventArgs e)
        {
            // ELITE: If the designer is stuck on this event, we redirect it to the main logic
            txtSearch_TextChanged(sender, e);
        }

        private void txtSearch_Click(object sender, EventArgs e)
        {
            // Reserved for future UX enhancements (like auto-selecting text)
        }
    }
}