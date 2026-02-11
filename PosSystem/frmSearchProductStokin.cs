using System;
using System.Data;
using System.Data.SQLite;
using System.Windows.Forms;

namespace PosSystem
{
    public partial class frmSearchProductStokin : Form
    {
        string stitle = "PosSystem";
        frmStockin slist;

        public frmSearchProductStokin(frmStockin flist)
        {
            InitializeComponent();
            slist = flist;
        }

        private void pictureBox1_Click(object sender, EventArgs e)
        {
            this.Dispose();
        }

        private void dataGridView1_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
            string colName = dataGridView1.Columns[e.ColumnIndex].Name;
            if (colName != "colSelect") return;

            if (string.IsNullOrEmpty(slist.txtRefNo.Text))
            {
                MessageBox.Show("Please enter reference no", stitle, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                slist.txtRefNo.Focus();
                return;
            }

            if (string.IsNullOrEmpty(slist.txtBy.Text))
            {
                MessageBox.Show("Please enter Stock in by", stitle, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                slist.txtBy.Focus();
                return;
            }

            string pcode = dataGridView1.Rows[e.RowIndex].Cells[1].Value.ToString();

            try
            {
                using (var cn = new SQLiteConnection(DBConnection.MyConnection()))
                {
                    cn.Open();

                    // Prevent duplicate entry for same RefNo & PCode
                    using (var checkCmd = new SQLiteCommand(
                        "SELECT COUNT(*) FROM tblStockIn WHERE refno = @refno AND pcode = @pcode AND status = 'Pending'", cn))
                    {
                        checkCmd.Parameters.AddWithValue("@refno", slist.txtRefNo.Text);
                        checkCmd.Parameters.AddWithValue("@pcode", pcode);

                        long count = (long)checkCmd.ExecuteScalar();
                        if (count > 0)
                        {
                            MessageBox.Show("This product is already added to the current reference number.", stitle, MessageBoxButtons.OK, MessageBoxIcon.Information);
                            return;
                        }
                    }

                    using (var cmd = new SQLiteCommand(
                        "INSERT INTO tblStockIn (refno, pcode, sdate, stockinby, vendorid) VALUES (@refno, @pcode, @sdate, @stockinby, @vendorid)", cn))
                    {
                        cmd.Parameters.AddWithValue("@refno", slist.txtRefNo.Text);
                        cmd.Parameters.AddWithValue("@pcode", pcode);
                        cmd.Parameters.AddWithValue("@sdate", slist.dt1.Value.ToString("yyyy-MM-dd HH:mm:ss"));
                        cmd.Parameters.AddWithValue("@stockinby", slist.txtBy.Text);
                        cmd.Parameters.AddWithValue("@vendorid", slist.lblVendorID.Text);
                        cmd.ExecuteNonQuery();
                    }
                }

                MessageBox.Show("Successfully Added!", stitle, MessageBoxButtons.OK, MessageBoxIcon.Information);

                // Reload grid in main form
                slist.LoadStockIn();

                // UX improvement: keep search text, just focus for next entry
                txtSearch.Focus();
                dataGridView1.ClearSelection();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, stitle, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        public void LoadProduct()
        {
            try
            {
                dataGridView1.Rows.Clear();

                using (var cn = new SQLiteConnection(DBConnection.MyConnection()))
                {
                    cn.Open();
                    using (var cmd = new SQLiteCommand(
                        "SELECT pcode, pdesc, qty FROM TblProduct1 WHERE pdesc LIKE @pdesc ORDER BY pdesc", cn))
                    {
                        cmd.Parameters.AddWithValue("@pdesc", "%" + txtSearch.Text + "%");

                        using (var dr = cmd.ExecuteReader())
                        {
                            int i = 0;
                            while (dr.Read())
                            {
                                i++;
                                dataGridView1.Rows.Add(
                                    i,
                                    dr["pcode"].ToString(),
                                    dr["pdesc"].ToString(),
                                    dr["qty"].ToString()
                                );
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, stitle, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void txtSearch_TextChanged(object sender, EventArgs e)
        {
            LoadProduct();
        }

        private void panel1_Paint(object sender, PaintEventArgs e) { }

        private void txtSearch_Click(object sender, EventArgs e)
        {

        }
    }
}