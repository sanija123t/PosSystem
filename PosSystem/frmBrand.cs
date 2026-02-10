using System;
using System.Data.SQLite;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PosSystem
{
    public partial class frmBrand : Form
    {
        private SQLiteConnection cn;
        private SQLiteCommand cm;
        private readonly frmBrandList frmlist;

        public frmBrand(frmBrandList flist)
        {
            InitializeComponent();
            frmlist = flist;

            // Button initial states
            button1.Enabled = true;  // Save
            button2.Enabled = false; // Update

            // Keyboard shortcuts
            this.KeyPreview = true;
            this.KeyDown += FrmBrand_KeyDown;
        }

        // 🔹 Keyboard shortcuts: Enter = Save/Update, Esc = Close
        private void FrmBrand_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape)
                this.Dispose();
            else if (e.KeyCode == Keys.Enter)
            {
                if (button1.Enabled) button1.PerformClick();
                else if (button2.Enabled) button2.PerformClick();
            }
        }

        // 🔹 Clear input for new entry
        private void Clear()
        {
            txtBrand.Clear();
            txtBrand.Focus();
            button1.Enabled = true;
            button2.Enabled = false;
        }

        // =========================
        // 🔹 SAVE BRAND (with double-click protection)
        // =========================
        private async void button1_Click(object sender, EventArgs e)
        {
            button1.Enabled = false; // prevent multiple clicks
            string brandName = txtBrand.Text.Trim();

            if (string.IsNullOrWhiteSpace(brandName))
            {
                MessageBox.Show("Please enter a brand name.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                button1.Enabled = true;
                return;
            }

            if (!await ConfirmAndCheckDuplicate(brandName, isUpdate: false))
            {
                button1.Enabled = true;
                return;
            }

            try
            {
                using (cn = new SQLiteConnection(DBConnection.MyConnection()))
                {
                    await cn.OpenAsync();
                    string sql = "INSERT INTO BrandTbl (brand) VALUES (@brand)";
                    using (cm = new SQLiteCommand(sql, cn))
                    {
                        cm.Parameters.AddWithValue("@brand", brandName);
                        await cm.ExecuteNonQueryAsync();
                    }
                }

                MessageBox.Show($"Brand '{brandName}' successfully added!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);

                if (frmlist != null)
                    await frmlist.LoadRecordsAsync(brandName); // refresh list with search highlight

                Clear();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving brand:\n{ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                button1.Enabled = true; // re-enable in case of error
            }
        }

        // =========================
        // 🔹 UPDATE BRAND (with double-click protection)
        // =========================
        private async void button2_Click(object sender, EventArgs e)
        {
            button2.Enabled = false; // prevent multiple clicks
            string brandName = txtBrand.Text.Trim();

            if (string.IsNullOrWhiteSpace(brandName))
            {
                MessageBox.Show("Brand name cannot be empty.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                button2.Enabled = true;
                return;
            }

            if (!await ConfirmAndCheckDuplicate(brandName, isUpdate: true))
            {
                button2.Enabled = true;
                return;
            }

            try
            {
                using (cn = new SQLiteConnection(DBConnection.MyConnection()))
                {
                    await cn.OpenAsync();
                    string sql = "UPDATE BrandTbl SET brand = @brand WHERE id = @id";
                    using (cm = new SQLiteCommand(sql, cn))
                    {
                        cm.Parameters.AddWithValue("@brand", brandName);
                        cm.Parameters.AddWithValue("@id", lblId.Text);
                        await cm.ExecuteNonQueryAsync();
                    }
                }

                MessageBox.Show($"Brand '{brandName}' successfully updated!", "Update", MessageBoxButtons.OK, MessageBoxIcon.Information);

                if (frmlist != null)
                    await frmlist.LoadRecordsAsync(brandName);

                this.Dispose();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error updating brand:\n{ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                button2.Enabled = true; // re-enable in case of error
            }
        }

        // =========================
        // 🔹 CANCEL / CLOSE FORM
        // =========================
        private void btnCancel_Click(object sender, EventArgs e) => this.Dispose();
        private void pictureBox1_Click(object sender, EventArgs e) => this.Dispose();

        private void frmBrand_Load(object sender, EventArgs e)
        {
            // You can leave this empty or use it to focus the textbox
            txtBrand.Focus();
        }

        // =========================
        // 🔹 DUPLICATE CHECK & CONFIRMATION
        // =========================
        private async Task<bool> ConfirmAndCheckDuplicate(string brandName, bool isUpdate)
        {
            string msg = isUpdate ? "Are you sure you want to update this brand?" :
                                    "Are you sure you want to save this brand?";

            if (MessageBox.Show(msg, "Confirm", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                return false;

            using (cn = new SQLiteConnection(DBConnection.MyConnection()))
            {
                await cn.OpenAsync();
                string sql = "SELECT COUNT(*) FROM BrandTbl WHERE brand = @brand";
                if (isUpdate) sql += " AND id != @id";

                using (cm = new SQLiteCommand(sql, cn))
                {
                    cm.Parameters.AddWithValue("@brand", brandName);
                    if (isUpdate) cm.Parameters.AddWithValue("@id", lblId.Text);

                    int count = Convert.ToInt32(await cm.ExecuteScalarAsync());
                    if (count > 0)
                    {
                        MessageBox.Show("This brand already exists!", "Duplicate", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return false;
                    }
                }
            }

            return true;
        }
    }
}