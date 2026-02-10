using System;
using System.Data;
using System.Data.SQLite;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Forms;

namespace PosSystem
{
    public partial class frmBrandList : Form
    {
        private CancellationTokenSource searchTokenSource; // for debounce

        public frmBrandList()
        {
            InitializeComponent();
            // Async load records on form load
            _ = LoadRecordsAsync();
        }

        // =========================
        // 🔹 ASYNC LOAD RECORDS
        // =========================
        public async Task LoadRecordsAsync(string search = "")
        {
            try
            {
                int i = 0;
                dataGridView1.Rows.Clear();

                using (SQLiteConnection cn = new SQLiteConnection(DBConnection.MyConnection()))
                {
                    await cn.OpenAsync();

                    string sql = "SELECT * FROM BrandTbl WHERE brand LIKE @search ORDER BY brand";
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
                MessageBox.Show($"Error loading brands:\n{ex.Message}", "Error",
                  MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // =========================
        // 🔹 ADD NEW BRAND
        // =========================
        private void pictureBox3_Click(object sender, EventArgs e)
        {
            frmBrand frm = new frmBrand(this)
            {
                button1 = { Enabled = true },
                button2 = { Enabled = false } // disable update
            };
            frm.ShowDialog();
        }

        // =========================
        // 🔹 EDIT / DELETE BRAND
        // =========================
        private async void dataGridView1_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;

            string colName = dataGridView1.Columns[e.ColumnIndex].Name;
            string brandId = dataGridView1.Rows[e.RowIndex].Cells[1].Value.ToString();
            string brandName = dataGridView1.Rows[e.RowIndex].Cells[2].Value.ToString();

            try
            {
                if (colName == "Edit")
                {
                    frmBrand frm = new frmBrand(this)
                    {
                        lblId = { Text = brandId },
                        txtBrand = { Text = brandName },
                        button1 = { Enabled = false },
                        button2 = { Enabled = true }
                    };
                    frm.ShowDialog();
                }
                else if (colName == "Delete")
                {
                    if (MessageBox.Show($"Are you sure you want to delete '{brandName}'?",
                      "Confirm Delete", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
                    {
                        await DeleteBrandAsync(brandId);
                        await LoadRecordsAsync(txtSearch.Text.Trim());
                        MessageBox.Show($"Brand '{brandName}' successfully deleted!", "Deleted",
                          MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error processing brand:\n{ex.Message}", "Error",
                  MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // =========================
        // 🔹 DELETE BRAND ASYNC
        // =========================
        private async Task DeleteBrandAsync(string brandId)
        {
            using (SQLiteConnection cn = new SQLiteConnection(DBConnection.MyConnection()))
            {
                await cn.OpenAsync();
                using (SQLiteCommand cm = new SQLiteCommand("DELETE FROM BrandTbl WHERE id = @id", cn))
                {
                    cm.Parameters.AddWithValue("@id", brandId);
                    await cm.ExecuteNonQueryAsync();
                }
            }
        }

        // =========================
        // 🔹 CLOSE FORM
        // =========================
        private void pictureBox2_Click(object sender, EventArgs e)
        {
            this.Dispose();
        }

        // =========================
        // 🔹 SEARCH BRAND WITH DEBOUNCE
        // =========================
        private async void txtSearch_TextChanged(object sender, EventArgs e)
        {
            // Cancel previous debounce
            searchTokenSource?.Cancel();
            searchTokenSource = new CancellationTokenSource();
            CancellationToken token = searchTokenSource.Token;

            try
            {
                // Wait 300ms before actually searching
                await Task.Delay(300, token);

                if (!token.IsCancellationRequested)
                {
                    await LoadRecordsAsync(txtSearch.Text.Trim());
                }
            }
            catch (TaskCanceledException)
            {
                // Ignored: expected when typing quickly
            }
        }
    }
}