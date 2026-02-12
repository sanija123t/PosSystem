using System;
using System.Data.SQLite;
using System.Windows.Forms;

namespace PosSystem
{
    public partial class frmSearchProductStokin : Form
    {
        private readonly string stitle = "POS System";
        private readonly frmStockin _stockInForm;

        public frmSearchProductStokin(frmStockin flist)
        {
            InitializeComponent();
            _stockInForm = flist;
        }

        private void frmSearchProductStokin_Load(object sender, EventArgs e)
        {
            LoadProduct();
            txtSearch.Focus();
        }

        private void pictureBox1_Click(object sender, EventArgs e) => this.Dispose();

        private void dataGridView1_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;

            if (dataGridView1.Columns[e.ColumnIndex].Name == "colSelect")
            {
                AddProductToStockIn(e.RowIndex);
            }
        }

        private void AddProductToStockIn(int rowIndex)
        {
            if (dataGridView1.Rows.Count <= rowIndex) return;

            string pcode = dataGridView1.Rows[rowIndex].Cells["Column2"].Value?.ToString();
            string pdesc = dataGridView1.Rows[rowIndex].Cells["Column4"].Value?.ToString();

            if (string.IsNullOrWhiteSpace(_stockInForm.txtRefNo.Text) || string.IsNullOrWhiteSpace(_stockInForm.txtBy.Text))
            {
                MessageBox.Show("Please ensure Reference No and 'Stock In By' fields are filled!", stitle, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                using (var cn = new SQLiteConnection(DBConnection.MyConnection()))
                {
                    cn.Open();
                    using (var transaction = cn.BeginTransaction())
                    {
                        string checkSql = "SELECT COUNT(*) FROM tblStockIn WHERE refno = @ref AND pcode = @pcode AND status = 'Pending'";
                        using (var checkCmd = new SQLiteCommand(checkSql, cn, transaction))
                        {
                            checkCmd.Parameters.AddWithValue("@ref", _stockInForm.txtRefNo.Text);
                            checkCmd.Parameters.AddWithValue("@pcode", pcode);
                            if (Convert.ToInt32(checkCmd.ExecuteScalar()) > 0)
                            {
                                MessageBox.Show($"{pdesc} is already in the list.", stitle, MessageBoxButtons.OK, MessageBoxIcon.Information);
                                return;
                            }
                        }

                        string sql = @"INSERT INTO tblStockIn (refno, pcode, sdate, stockinby, vendorid, status) 
                                       VALUES (@ref, @pcode, @sdate, @by, @vrid, 'Pending')";
                        using (var cmd = new SQLiteCommand(sql, cn, transaction))
                        {
                            cmd.Parameters.AddWithValue("@ref", _stockInForm.txtRefNo.Text);
                            cmd.Parameters.AddWithValue("@pcode", pcode);
                            cmd.Parameters.AddWithValue("@sdate", _stockInForm.dt1.Value.ToString("yyyy-MM-dd HH:mm:ss"));
                            cmd.Parameters.AddWithValue("@by", _stockInForm.txtBy.Text);
                            cmd.Parameters.AddWithValue("@vrid", _stockInForm.lblVendorID.Text);
                            cmd.ExecuteNonQuery();
                        }
                        transaction.Commit();
                    }
                }

                _stockInForm.LoadStockIn();
                txtSearch.Clear();
                txtSearch.Focus();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Database Error: " + ex.Message, stitle, MessageBoxButtons.OK, MessageBoxIcon.Error);
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
                    string sql = "SELECT pcode, pdesc, qty FROM TblProduct1 WHERE pdesc LIKE @search OR pcode LIKE @search ORDER BY pdesc";
                    using (var cmd = new SQLiteCommand(sql, cn))
                    {
                        cmd.Parameters.AddWithValue("@search", $"%{txtSearch.Text}%");
                        using (var dr = cmd.ExecuteReader())
                        {
                            while (dr.Read())
                            {
                                int n = dataGridView1.Rows.Add();
                                dataGridView1.Rows[n].Cells[0].Value = dataGridView1.Rows.Count;
                                dataGridView1.Rows[n].Cells[1].Value = dr["pcode"].ToString();
                                dataGridView1.Rows[n].Cells[2].Value = dr["pdesc"].ToString();
                                dataGridView1.Rows[n].Cells[3].Value = dr["qty"].ToString();
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Search Error: " + ex.Message, stitle, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void txtSearch_TextChanged(object sender, EventArgs e) => LoadProduct();

        private void txtSearch_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                if (dataGridView1.Rows.Count > 0)
                {
                    AddProductToStockIn(0);
                }
                e.SuppressKeyPress = true;
            }
        }

        // --- DESIGNER COMPATIBILITY METHODS ---
        // These keep the Designer from crashing without cluttering your logic.
        private void panel1_Paint(object sender, PaintEventArgs e) { }
        private void txtSearch_Click(object sender, EventArgs e) { }
    }
}