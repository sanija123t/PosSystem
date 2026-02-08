using System;
using System.Data;
using System.Windows.Forms;
using System.Data.SQLite;

namespace PosSystem
{
    public partial class frmBrandList : Form
    {
        SQLiteConnection cn;
        SQLiteCommand cm;
        SQLiteDataReader dr;

        public frmBrandList()
        {
            InitializeComponent();
            LoadRecords();
        }

        public void LoadRecords()
        {
            try
            {
                int i = 0;
                dataGridView1.Rows.Clear();
                using (cn = new SQLiteConnection(DBConnection.MyConnection()))
                {
                    cn.Open();
                    string sql = "SELECT * FROM BrandTbl ORDER BY brand";
                    using (cm = new SQLiteCommand(sql, cn))
                    {
                        using (dr = cm.ExecuteReader())
                        {
                            while (dr.Read())
                            {
                                i++;
                                dataGridView1.Rows.Add(i, dr["id"].ToString(), dr["brand"].ToString());
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void pictureBox3_Click(object sender, EventArgs e) // Add Button
        {
            frmBrand frm = new frmBrand(this);
            frm.button1.Enabled = true;
            frm.button2.Enabled = false;
            frm.ShowDialog();
        }

        private void dataGridView1_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
            string colName = dataGridView1.Columns[e.ColumnIndex].Name;

            try
            {
                if (colName == "Edit")
                {
                    frmBrand frm = new frmBrand(this);
                    frm.lblId.Text = dataGridView1.Rows[e.RowIndex].Cells[1].Value.ToString();
                    frm.txtBrand.Text = dataGridView1.Rows[e.RowIndex].Cells[2].Value.ToString();
                    frm.button1.Enabled = false;
                    frm.button2.Enabled = true;
                    frm.ShowDialog();
                }
                else if (colName == "Delete")
                {
                    if (MessageBox.Show("Delete this brand?", "Confirm", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                    {
                        using (cn = new SQLiteConnection(DBConnection.MyConnection()))
                        {
                            cn.Open();
                            string sql = "DELETE FROM BrandTbl WHERE id = @id";
                            using (cm = new SQLiteCommand(sql, cn))
                            {
                                cm.Parameters.AddWithValue("@id", dataGridView1.Rows[e.RowIndex].Cells[1].Value.ToString());
                                cm.ExecuteNonQuery();
                            }
                        }
                        MessageBox.Show("Brand has been successfully deleted.", "Deleted", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        LoadRecords();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void pictureBox2_Click(object sender, EventArgs e) // Close Button
        {
            this.Dispose();
        }
    }
}