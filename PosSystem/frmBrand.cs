using System;
using System.Data.SQLite;
using System.Windows.Forms;

namespace PosSystem
{
    public partial class frmBrand : Form
    {
        SQLiteConnection cn;
        SQLiteCommand cm;
        // DBConnection is static in your latest version, so we call it directly
        frmBrandList frmlist;

        public frmBrand(frmBrandList flist)
        {
            InitializeComponent();
            frmlist = flist;
        }

        private void frmBrand_Load(object sender, EventArgs e)
        {
            // Initial button states can be set here or in Designer
        }

        private void Clear()
        {
            txtBrand.Clear();
            txtBrand.Focus();
            button1.Enabled = true;
            button2.Enabled = false;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(txtBrand.Text))
                {
                    MessageBox.Show("Please enter a brand name.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                if (MessageBox.Show("Are you sure you want to save this brand?", "Confirm", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    using (cn = new SQLiteConnection(DBConnection.MyConnection()))
                    {
                        cn.Open();
                        string sql = "INSERT INTO BrandTbl (brand) VALUES (@brand)";
                        using (cm = new SQLiteCommand(sql, cn))
                        {
                            cm.Parameters.AddWithValue("@brand", txtBrand.Text.Trim());
                            cm.ExecuteNonQuery();
                        }
                    }
                    MessageBox.Show("Record has been successfully saved.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    frmlist.LoadRecords();
                    Clear();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void pictureBox1_Click(object sender, EventArgs e)
        {
            this.Dispose();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(txtBrand.Text))
                {
                    MessageBox.Show("Brand name cannot be empty.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                if (MessageBox.Show("Are you sure you want to update this brand?", "Update Record", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    using (cn = new SQLiteConnection(DBConnection.MyConnection()))
                    {
                        cn.Open();
                        string sql = "UPDATE BrandTbl SET brand = @brand WHERE id = @id";
                        using (cm = new SQLiteCommand(sql, cn))
                        {
                            cm.Parameters.AddWithValue("@brand", txtBrand.Text.Trim());
                            cm.Parameters.AddWithValue("@id", lblId.Text);
                            cm.ExecuteNonQuery();
                        }
                    }
                    MessageBox.Show("Brand has been successfully updated.", "Update", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    frmlist.LoadRecords();
                    this.Dispose();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            this.Dispose();
        }
    }
}