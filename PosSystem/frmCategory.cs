using System;
using System.Data.SQLite;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PosSystem
{
    public partial class frmCategory : Form
    {
        private readonly frmCategoryList flist;

        public frmCategory(frmCategoryList frm)
        {
            InitializeComponent();
            flist = frm;
        }

        private void pictureBox1_Click(object sender, EventArgs e)
        {
            Dispose();
        }

        // =========================
        // 🔹 CLEAR FORM
        // =========================
        public void Clear()
        {
            btnSave.Enabled = true;
            btnUpdate.Enabled = false;
            txtcategory.Clear();
            txtcategory.Focus();
        }

        // =========================
        // 🔹 SAVE CATEGORY
        // =========================
        private async void btnSave_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtcategory.Text))
            {
                MessageBox.Show("Please enter a category name.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            btnSave.Enabled = false;

            try
            {
                using (var cn = new SQLiteConnection(DBConnection.MyConnection()))
                {
                    await cn.OpenAsync();

                    // Check for duplicate category
                    string checkSql = "SELECT COUNT(*) FROM TblCategory WHERE category = @name";
                    using (var cmdCheck = new SQLiteCommand(checkSql, cn))
                    {
                        cmdCheck.Parameters.AddWithValue("@name", txtcategory.Text.Trim());
                        int count = Convert.ToInt32(await cmdCheck.ExecuteScalarAsync());
                        if (count > 0)
                        {
                            MessageBox.Show("This category already exists.", "Duplicate Entry", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            return;
                        }
                    }

                    // Confirm save
                    if (MessageBox.Show("Are you sure you want to save this category?", "Saving Record",
                        MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                        return;

                    // Save new category
                    using (var cm = new SQLiteCommand("INSERT INTO TblCategory (category) VALUES (@category)", cn))
                    {
                        cm.Parameters.AddWithValue("@category", txtcategory.Text.Trim());
                        await cm.ExecuteNonQueryAsync();
                    }
                }

                MessageBox.Show("Category has been successfully saved.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                Clear();
                if (flist != null)
                    await flist.LoadCategoryAsync(); // Refresh list
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                btnSave.Enabled = true;
            }
        }

        // =========================
        // 🔹 UPDATE CATEGORY
        // =========================
        private async void btnUpdate_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtcategory.Text))
            {
                MessageBox.Show("Category name cannot be empty.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            btnUpdate.Enabled = false;

            try
            {
                using (var cn = new SQLiteConnection(DBConnection.MyConnection()))
                {
                    await cn.OpenAsync();

                    // Optional: Prevent duplicate on update (exclude current ID)
                    string checkSql = "SELECT COUNT(*) FROM TblCategory WHERE category = @name AND id != @id";
                    using (var cmdCheck = new SQLiteCommand(checkSql, cn))
                    {
                        cmdCheck.Parameters.AddWithValue("@name", txtcategory.Text.Trim());
                        cmdCheck.Parameters.AddWithValue("@id", lblId.Text);
                        int count = Convert.ToInt32(await cmdCheck.ExecuteScalarAsync());
                        if (count > 0)
                        {
                            MessageBox.Show("Another category with this name already exists.", "Duplicate Entry", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            return;
                        }
                    }

                    if (MessageBox.Show("Are you sure you want to update this category?", "Update Category",
                        MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                        return;

                    using (var cm = new SQLiteCommand("UPDATE TblCategory SET category = @category WHERE id = @id", cn))
                    {
                        cm.Parameters.AddWithValue("@category", txtcategory.Text.Trim());
                        cm.Parameters.AddWithValue("@id", lblId.Text);
                        await cm.ExecuteNonQueryAsync();
                    }
                }

                MessageBox.Show("Record has been successfully updated!", "Update", MessageBoxButtons.OK, MessageBoxIcon.Information);
                if (flist != null)
                    await flist.LoadCategoryAsync(); // Refresh list

                Dispose();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                btnUpdate.Enabled = true;
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            Clear();
        }

        private void frmCategory_Load(object sender, EventArgs e)
        {
            txtcategory.Focus();
        }
    }
}