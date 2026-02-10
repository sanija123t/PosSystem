using System;
using System.Data;
using System.Data.SQLite;
using System.Windows.Forms;

namespace PosSystem
{
    public partial class frmProduct_List : Form
    {
        public frmProduct_List()
        {
            InitializeComponent();
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

                using (SQLiteConnection cn = new SQLiteConnection(DBConnection.MyConnection()))
                {
                    cn.Open();
                    string query = @"
                        SELECT p.pcode, p.barcode, p.pdesc, b.brand, c.category, p.price, p.reorder 
                        FROM TblProduct1 AS p 
                        INNER JOIN BrandTbl AS b ON b.id = p.bid 
                        INNER JOIN TblCategory AS c ON c.id = p.cid 
                        WHERE p.pdesc LIKE @search";

                    using (SQLiteCommand cm = new SQLiteCommand(query, cn))
                    {
                        cm.Parameters.AddWithValue("@search", txtSearch.Text + "%");

                        using (SQLiteDataReader dr = cm.ExecuteReader())
                        {
                            while (dr.Read())
                            {
                                i++;
                                dataGridView1.Rows.Add(
                                    i,
                                    dr["pcode"].ToString(),
                                    dr["barcode"].ToString(),
                                    dr["pdesc"].ToString(),
                                    dr["brand"].ToString(),
                                    dr["category"].ToString(),
                                    dr["price"].ToString(),
                                    dr["reorder"].ToString()
                                );
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
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

        private void panel1_Paint_1(object sender, PaintEventArgs e)
        {
            // If you don’t need any custom painting, just leave it empty
        }
        private void dataGridView1_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;

            try
            {
                string colName = dataGridView1.Columns[e.ColumnIndex].Name;
                string pcode = dataGridView1.Rows[e.RowIndex].Cells["pcode"].Value.ToString();

                if (colName == "Edit")
                {
                    frmProduct frm = new frmProduct(this);
                    frm.btnSave.Enabled = false;
                    frm.btnUpdate.Enabled = true;

                    // Map DataGridView cells to Product form
                    frm.TxtPcode.Text = pcode;
                    frm.txtBarcode.Text = dataGridView1.Rows[e.RowIndex].Cells["barcode"].Value.ToString();
                    frm.txtPdesc.Text = dataGridView1.Rows[e.RowIndex].Cells["pdesc"].Value.ToString();
                    frm.comboBox1.Text = dataGridView1.Rows[e.RowIndex].Cells["brand"].Value.ToString();
                    frm.comboBox2.Text = dataGridView1.Rows[e.RowIndex].Cells["category"].Value.ToString();
                    frm.txtPrice.Text = dataGridView1.Rows[e.RowIndex].Cells["price"].Value.ToString();
                    frm.txtReOrder.Text = dataGridView1.Rows[e.RowIndex].Cells["reorder"].Value.ToString();

                    frm.ShowDialog();
                }
                else if (colName == "Delete")
                {
                    if (MessageBox.Show("Are you sure you want to delete this product?", "Delete Product", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                    {
                        using (SQLiteConnection cn = new SQLiteConnection(DBConnection.MyConnection()))
                        {
                            cn.Open();
                            using (SQLiteCommand cm = new SQLiteCommand("DELETE FROM TblProduct1 WHERE pcode = @pcode", cn))
                            {
                                cm.Parameters.AddWithValue("@pcode", pcode);
                                cm.ExecuteNonQuery();
                            }
                        }

                        MessageBox.Show("Item Removed Successfully", "Done", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        LoadRecords();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}