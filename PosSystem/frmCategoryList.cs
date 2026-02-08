using System;
using System.Data.SQLite; // CHANGED: From SqlClient to SQLite
using System.Windows.Forms;

namespace PosSystem
{
    public partial class frmCategoryList : Form
    {
        SQLiteConnection cn;
        SQLiteCommand cm;
        SQLiteDataReader dr;

        public frmCategoryList()
        {
            InitializeComponent();
            cn = new SQLiteConnection(DBConnection.MyConnection());
            LoadCategory();
        }

        private void pictureBox1_Click(object sender, EventArgs e)
        {
            this.Dispose();
        }

        public void LoadCategory()
        {
            try
            {
                int i = 0;
                dataGridView1.Rows.Clear();
                cn.Open();
                // Fixed table name spelling to 'TblCategory' to match typical naming conventions and previous fixes
                cm = new SQLiteCommand("SELECT * FROM TblCategory ORDER BY category", cn);
                dr = cm.ExecuteReader();
                while (dr.Read())
                {
                    i++;
                    dataGridView1.Rows.Add(i, dr["id"].ToString(), dr["category"].ToString());
                }
                dr.Close();
                cn.Close();
            }
            catch (Exception ex)
            {
                if (cn.State == System.Data.ConnectionState.Open) cn.Close();
                MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void pictureBox2_Click(object sender, EventArgs e) // Add Category
        {
            frmCategory frm = new frmCategory(this);
            frm.btnSave.Enabled = true;
            frm.btnUpdate.Enabled = false;
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
                    frmCategory frm = new frmCategory(this);
                    frm.lblId.Text = dataGridView1.Rows[e.RowIndex].Cells[1].Value.ToString();
                    frm.txtcategory.Text = dataGridView1.Rows[e.RowIndex].Cells[2].Value.ToString();
                    frm.btnSave.Enabled = false;
                    frm.btnUpdate.Enabled = true;
                    frm.ShowDialog();
                }
                else if (colName == "Delete")
                {
                    if (MessageBox.Show("Are you sure you want to delete this category?", "Delete Category", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                    {
                        cn.Open();
                        cm = new SQLiteCommand("DELETE FROM TblCategory WHERE id = @id", cn);
                        cm.Parameters.AddWithValue("@id", dataGridView1.Rows[e.RowIndex].Cells[1].Value.ToString());
                        cm.ExecuteNonQuery();
                        cn.Close();
                        MessageBox.Show("Record has been successfully deleted!", "Deleted", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        LoadCategory();
                    }
                }
            }
            catch (Exception ex)
            {
                if (cn.State == System.Data.ConnectionState.Open) cn.Close();
                MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}