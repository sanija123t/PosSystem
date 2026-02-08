using System;
using System.Data.SQLite; // CHANGED: From SqlClient to SQLite
using System.Windows.Forms;

namespace PosSystem
{
    public partial class frmCategory : Form
    {
        SQLiteConnection cn;
        SQLiteCommand cm;
        frmCategoryList flist;

        public frmCategory(frmCategoryList frm)
        {
            InitializeComponent();
            cn = new SQLiteConnection(DBConnection.MyConnection());
            flist = frm;
        }

        private void pictureBox1_Click(object sender, EventArgs e)
        {
            this.Dispose();
        }

        public void Clear()
        {
            btnSave.Enabled = true;
            btnUpdate.Enabled = false;
            txtcategory.Clear();
            txtcategory.Focus();
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(txtcategory.Text))
                {
                    MessageBox.Show("Please enter a category name.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                if (MessageBox.Show("Are you sure you want to save this category?", "Saving Record", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    cn.Open();
                    // Note: Table name fixed to 'TblCategory' to match your DBConnection schema
                    cm = new SQLiteCommand("INSERT INTO TblCategory (category) VALUES (@category)", cn);
                    cm.Parameters.AddWithValue("@category", txtcategory.Text.Trim());
                    cm.ExecuteNonQuery();
                    cn.Close();

                    MessageBox.Show("Category has been successfully saved.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    Clear();
                    flist.LoadCategory();
                }
            }
            catch (Exception ex)
            {
                if (cn.State == System.Data.ConnectionState.Open) cn.Close();
                MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnUpdate_Click(object sender, EventArgs e)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(txtcategory.Text))
                {
                    MessageBox.Show("Category name cannot be empty.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                if (MessageBox.Show("Are you sure you want to update this category?", "Update Category", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    cn.Open();
                    // Using parameterized query for ID and fixed table name
                    cm = new SQLiteCommand("UPDATE TblCategory SET category = @category WHERE id = @id", cn);
                    cm.Parameters.AddWithValue("@category", txtcategory.Text.Trim());
                    cm.Parameters.AddWithValue("@id", lblId.Text);
                    cm.ExecuteNonQuery();
                    cn.Close();

                    MessageBox.Show("Record has been successfully updated!", "Update", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    flist.LoadCategory();
                    this.Dispose();
                }
            }
            catch (Exception ex)
            {
                if (cn.State == System.Data.ConnectionState.Open) cn.Close();
                MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            Clear();
        }

        private void frmCategory_Load(object sender, EventArgs e)
        {
        }
    }
}