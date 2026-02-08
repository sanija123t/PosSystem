using System;
using System.Data;
using System.Data.SQLite;
using System.Windows.Forms;

namespace PosSystem
{
    public partial class frmLookUp : Form
    {
        Form1 f;
        frmPOS fPOS;
        SQLiteConnection cn;
        SQLiteCommand cm;
        SQLiteDataReader dr;

        public frmLookUp(Form1 frm)
        {
            InitializeComponent();
            cn = new SQLiteConnection(DBConnection.MyConnection());
            f = frm;
            this.KeyPreview = true;
        }

        public frmLookUp(frmPOS frm)
        {
            InitializeComponent();
            cn = new SQLiteConnection(DBConnection.MyConnection());
            fPOS = frm;
            this.KeyPreview = true;
        }

        private void pictureBox2_Click(object sender, EventArgs e)
        {
            this.Dispose();
        }

        public void LoadRecords()
        {
            try
            {
                int i = 0;
                dataGridView1.Rows.Clear();
                cn.Open();

                string query = @"SELECT p.pcode, p.barcode, p.pdesc, b.brand, c.category, p.price, p.qty 
                                 FROM TblProduct1 AS p 
                                 INNER JOIN BrandTbl AS b ON b.id = p.bid 
                                 INNER JOIN TblCatecory AS c ON c.id = p.cid 
                                 WHERE p.pdesc LIKE @search";

                cm = new SQLiteCommand(query, cn);
                cm.Parameters.AddWithValue("@search", txtSearch.Text + "%");
                dr = cm.ExecuteReader();

                while (dr.Read())
                {
                    i++;
                    dataGridView1.Rows.Add(i, dr["pcode"].ToString(), dr["barcode"].ToString(), dr["pdesc"].ToString(), dr["brand"].ToString(), dr["category"].ToString(), dr["price"].ToString(), dr["qty"].ToString());
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

        private void txtSearch_TextChanged(object sender, EventArgs e)
        {
            LoadRecords();
        }

        private void dataGridView1_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            try
            {
                string colName = dataGridView1.Columns[e.ColumnIndex].Name;
                if (colName == "Select")
                {
                    // FIX: Call the constructor based on which parent form is active
                    frmQty frm;
                    if (fPOS != null)
                    {
                        frm = new frmQty(fPOS);
                    }
                    else
                    {
                        frm = new frmQty(f);
                    }

                    string pcode = dataGridView1.Rows[e.RowIndex].Cells[1].Value.ToString();
                    double price = Double.Parse(dataGridView1.Rows[e.RowIndex].Cells[6].Value.ToString());
                    int qtyHand = int.Parse(dataGridView1.Rows[e.RowIndex].Cells[7].Value.ToString());

                    string transno = "";
                    if (fPOS != null)
                    {
                        transno = fPOS.lblTransno.Text;
                    }
                    else if (f != null)
                    {
                        // Ensure lblTransno exists on Form1 or handle via fPOS
                        // Since you moved it, this might need verification
                        transno = "0000";
                    }

                    frm.ProductDetails(pcode, price, transno, qtyHand);
                    frm.ShowDialog();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Selection Error: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void frmLookUp_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape)
            {
                this.Dispose();
            }
        }

        private void panel2_Paint(object sender, PaintEventArgs e) { }
        private void panel1_Paint(object sender, PaintEventArgs e) { }
        private void frmLookUp_KeyPress(object sender, KeyPressEventArgs e) { }
    }
}