using System;
using System.Data.SQLite;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Drawing;

namespace PosSystem
{
    public partial class frmBrand : Form
    {
        // ELITE: Connection string cached from our updated DBConnection
        private readonly string _connectionString = DBConnection.MyConnection();
        private readonly frmBrandList _frmlist;

        public frmBrand(frmBrandList flist)
        {
            InitializeComponent();
            _frmlist = flist;

            this.StartPosition = FormStartPosition.CenterParent;
            this.KeyPreview = true;
            this.KeyDown += FrmBrand_KeyDown;
        }

        private void FrmBrand_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape) this.Dispose();
            else if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true;
                if (button1.Enabled) button1_Click(sender, e);
                else if (button2.Enabled) button2_Click(sender, e);
            }
        }

        private void Clear()
        {
            txtBrand.Clear();
            lblId.Text = ""; // Clear the hidden ID
            txtBrand.Focus();
            button1.Enabled = true;
            button2.Enabled = false;
        }

        // ============================================================
        // 🔹 SAVE BRAND (Enterprise Implementation)
        // ============================================================
        private async void button1_Click(object sender, EventArgs e)
        {
            string brandName = txtBrand.Text.Trim();

            // ENTERPRISE: Strict Input Validation
            if (string.IsNullOrWhiteSpace(brandName))
            {
                MessageBox.Show("Brand name is required to proceed.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtBrand.Focus();
                return;
            }

            try
            {
                // ENTERPRISE: Lock UI to prevent duplicate entries
                this.Cursor = Cursors.WaitCursor;
                button1.Enabled = false;

                // 1. Check Duplicate First
                if (await IsDuplicate(brandName))
                {
                    MessageBox.Show($"Brand '{brandName}' already exists in the system.", "Duplicate Conflict", MessageBoxButtons.OK, MessageBoxIcon.Stop);
                    return;
                }

                // 2. Confirm Save
                if (MessageBox.Show($"Are you sure you want to save '{brandName}'?", "Confirm Save", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.No)
                    return;

                using (var cn = new SQLiteConnection(_connectionString))
                {
                    await cn.OpenAsync();
                    // ENTERPRISE: Using Transaction for Atomic consistency
                    using (var transaction = cn.BeginTransaction())
                    {
                        using (var cm = new SQLiteCommand("INSERT INTO BrandTbl (brand) VALUES (@brand)", cn, transaction))
                        {
                            cm.Parameters.AddWithValue("@brand", brandName);
                            await cm.ExecuteNonQueryAsync();
                        }
                        transaction.Commit();
                    }
                }

                MessageBox.Show("New brand has been successfully registered.", "Registry Updated", MessageBoxButtons.OK, MessageBoxIcon.Information);

                if (_frmlist != null) await _frmlist.LoadRecordsAsync();
                Clear();
            }
            catch (Exception ex)
            {
                MessageBox.Show("System Error while saving: " + ex.Message, "Enterprise Database Failure", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                this.Cursor = Cursors.Default;
                button1.Enabled = true; // Unlock UI
            }
        }

        // ============================================================
        // 🔹 UPDATE BRAND (Enterprise Implementation)
        // ============================================================
        private async void button2_Click(object sender, EventArgs e)
        {
            string brandName = txtBrand.Text.Trim();

            if (string.IsNullOrWhiteSpace(brandName) || string.IsNullOrEmpty(lblId.Text))
            {
                MessageBox.Show("Target brand ID or name is missing. Please reload the record.", "System Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                this.Cursor = Cursors.WaitCursor;
                button2.Enabled = false;

                if (await IsDuplicate(brandName, lblId.Text))
                {
                    MessageBox.Show($"The name '{brandName}' is already assigned to another brand.", "Conflict Detected", MessageBoxButtons.OK, MessageBoxIcon.Stop);
                    return;
                }

                if (MessageBox.Show($"Update current record to '{brandName}'?", "Confirm Update", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.No)
                    return;

                using (var cn = new SQLiteConnection(_connectionString))
                {
                    await cn.OpenAsync();
                    using (var transaction = cn.BeginTransaction())
                    {
                        using (var cm = new SQLiteCommand("UPDATE BrandTbl SET brand=@brand WHERE id=@id", cn, transaction))
                        {
                            cm.Parameters.AddWithValue("@brand", brandName);
                            cm.Parameters.AddWithValue("@id", lblId.Text);
                            await cm.ExecuteNonQueryAsync();
                        }
                        transaction.Commit();
                    }
                }

                MessageBox.Show("Record modified successfully.", "Update Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
                if (_frmlist != null) await _frmlist.LoadRecordsAsync();
                this.Dispose();
            }
            catch (Exception ex)
            {
                MessageBox.Show("System Error while updating: " + ex.Message, "Enterprise Update Failure", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                this.Cursor = Cursors.Default;
                button2.Enabled = true;
            }
        }

        // ============================================================
        // 🔹 HELPER: DUPLICATE CHECK
        // ============================================================
        private async Task<bool> IsDuplicate(string brandName, string id = "")
        {
            try
            {
                using (var cn = new SQLiteConnection(_connectionString))
                {
                    await cn.OpenAsync();
                    // Case-insensitive check using LOWER()
                    string sql = "SELECT COUNT(*) FROM BrandTbl WHERE LOWER(brand) = LOWER(@brand)";
                    if (!string.IsNullOrEmpty(id)) sql += " AND id != @id";

                    using (var cm = new SQLiteCommand(sql, cn))
                    {
                        cm.Parameters.AddWithValue("@brand", brandName);
                        if (!string.IsNullOrEmpty(id)) cm.Parameters.AddWithValue("@id", id);

                        int count = Convert.ToInt32(await cm.ExecuteScalarAsync());
                        return count > 0;
                    }
                }
            }
            catch { return false; } // Fail-safe
        }

        // ------------------------------------------------------------
        // 🔹 JUNK LINES (Preserving for Designer Compatibility)
        // ------------------------------------------------------------
        private void btnCancel_Click(object sender, EventArgs e) => this.Dispose();
        private void pictureBox1_Click(object sender, EventArgs e) => this.Dispose();
        private void frmBrand_Load(object sender, EventArgs e) => txtBrand.Focus();
        private void button3_Click(object sender, EventArgs e) => Clear();

        private void txtBrand_TextChanged(object sender, EventArgs e)
        {
            // Do not remove: Designer dependency
        }
    }
}