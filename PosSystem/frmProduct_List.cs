using System;
using System.Data;
using System.Data.SQLite; // Switched to SQLite
using System.Windows.Forms;

namespace PosSystem
{
    public partial class frmProduct_List : Form
    {
        SQLiteConnection cn;
        SQLiteCommand cm;
        SQLiteDataReader dr;

        public frmProduct_List()
        {
            InitializeComponent();
            cn = new SQLiteConnection(DBConnection.MyConnection());
        }

        private void pictureBox2_Click(object sender, EventArgs e)
        {
            frmProduct frm = new frmProduct(this);
            frm.btnSave.Enabled = true;
            frm.btnUpdate.Enabled = false;
            frm.LocalBrand();
            frm.LocalCategory();
            frm.ShowDialog();
        }

        public void LoadRecords()
        {
            try
            {
                int i = 0;
                dataGridView1.Rows.Clear();
                cn.Open();

                // Fixed: Corrected table name 'TblCatecory' to 'TblCategory' based on the SQL error 'no such table'
                string query = @"SELECT p.pcode, p.barcode, p.pdesc, b.brand, c.category, p.price, p.reorder 
                                 FROM TblProduct1 AS p 
                                 INNER JOIN BrandTbl AS b ON b.id = p.bid 
                                 INNER JOIN TblCategory AS c ON c.id = p.cid 
                                 WHERE p.pdesc LIKE @search";

                cm = new SQLiteCommand(query, cn);
                cm.Parameters.AddWithValue("@search", txtSearch.Text + "%");
                dr = cm.ExecuteReader();

                while (dr.Read())
                {
                    i++;
                    dataGridView1.Rows.Add(i, dr["pcode"].ToString(), dr["barcode"].ToString(), dr["pdesc"].ToString(), dr["brand"].ToString(), dr["category"].ToString(), dr["price"].ToString(), dr["reorder"].ToString());
                }
                dr.Close();
                cn.Close();
            }
            catch (Exception ex)
            {
                if (cn.State == ConnectionState.Open) cn.Close();
                MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void pictureBox1_Click(object sender, EventArgs e)
        {
            this.Dispose();
        }

        private void txtSearch_TextChanged(object sender, EventArgs e)
        {
            LoadRecords();
        }

        private void dataGridView1_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;

            try
            {
                string colName = dataGridView1.Columns[e.ColumnIndex].Name;
                if (colName == "Edit")
                {
                    frmProduct frm = new frmProduct(this);
                    frm.btnSave.Enabled = false;
                    frm.btnUpdate.Enabled = true;

                    // Mapping DataGridView cells to Product entry form
                    frm.TxtPcode.Text = dataGridView1.Rows[e.RowIndex].Cells[1].Value.ToString();
                    frm.txtBarcode.Text = dataGridView1.Rows[e.RowIndex].Cells[2].Value.ToString();
                    frm.txtPdesc.Text = dataGridView1.Rows[e.RowIndex].Cells[3].Value.ToString();
                    frm.comboBox1.Text = dataGridView1.Rows[e.RowIndex].Cells[4].Value.ToString(); // Brand
                    frm.comboBox2.Text = dataGridView1.Rows[e.RowIndex].Cells[5].Value.ToString(); // Category
                    frm.txtPrice.Text = dataGridView1.Rows[e.RowIndex].Cells[6].Value.ToString();
                    frm.txtReOrder.Text = dataGridView1.Rows[e.RowIndex].Cells[7].Value.ToString();
                    frm.ShowDialog();
                }
                else if (colName == "Delete")
                {
                    if (MessageBox.Show("Are you sure you want to delete this product?", "Delete Product", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                    {
                        cn.Open();
                        cm = new SQLiteCommand("DELETE FROM tblProduct1 WHERE pcode = @pcode", cn);
                        cm.Parameters.AddWithValue("@pcode", dataGridView1.Rows[e.RowIndex].Cells[1].Value.ToString());
                        cm.ExecuteNonQuery();
                        cn.Close();

                        MessageBox.Show("Item Removed Successfully", "Done", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        LoadRecords();
                    }
                }
            }
            catch (Exception ex)
            {
                if (cn.State == ConnectionState.Open) cn.Close();
                MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void panel1_Paint(object sender, PaintEventArgs e) { }
        private void panel1_Paint_1(object sender, PaintEventArgs e) { }
    }
}