using System;
using System.Data;
using System.Data.SQLite;
using System.Windows.Forms;

namespace PosSystem
{
    public partial class frmAdjustment : Form
    {
        private SQLiteCommand cm;
        private SQLiteConnection cn;
        private SQLiteDataReader dr;
        private Form1 f;
        private int _qty = 0;

        public frmAdjustment(Form1 f)
        {
            InitializeComponent();
            this.f = f;
            txtSearch.WaterMark = "Search here";
        }

        private void frmAdjustment_Load(object sender, EventArgs e)
        {
            referenceNo();
            LoadRecords();
        }

        public void referenceNo()
        {
            Random rnd = new Random();
            txtRef.Text = rnd.Next(100000, 999999).ToString();
        }

        private void pictureBox2_Click(object sender, EventArgs e)
        {
            this.Dispose();
        }

        public void LoadRecords()
        {
            try
            {
                dataGridView1.Rows.Clear();
                int i = 0;

                using (cn = new SQLiteConnection(DBConnection.MyConnection()))
                {
                    string query = @"
                        SELECT p.pcode, p.barcode, p.pdesc, b.brand, c.category, p.price, p.qty
                        FROM TblProduct1 AS p
                        INNER JOIN BrandTbl AS b ON b.id = p.bid
                        INNER JOIN TblCategory AS c ON c.id = p.cid
                        WHERE p.pdesc LIKE @search OR p.pcode LIKE @search OR p.barcode LIKE @search";

                    using (cm = new SQLiteCommand(query, cn))
                    {
                        cm.Parameters.AddWithValue("@search", "%" + txtSearch.Text.Trim() + "%");
                        cn.Open();
                        using (dr = cm.ExecuteReader())
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
                                    dr["qty"].ToString()
                                );
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Load Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void txtSearch_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == (char)Keys.Enter)
            {
                LoadRecords();
            }
        }

        private void dataGridView1_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;

            string colName = dataGridView1.Columns[e.ColumnIndex].Name;
            if (colName == "ActionSelect")
            {
                txtPcode.Text = dataGridView1.Rows[e.RowIndex].Cells[1].Value.ToString();
                txtdesc.Text = dataGridView1.Rows[e.RowIndex].Cells[3].Value.ToString() + " (" +
                               dataGridView1.Rows[e.RowIndex].Cells[2].Value.ToString() + ")";

                // Ensure we get the latest quantity from the grid
                int.TryParse(dataGridView1.Rows[e.RowIndex].Cells[7].Value.ToString(), out _qty);
            }
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(txtPcode.Text))
            {
                MessageBox.Show("Please select a product first.", "Missing Product", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (!int.TryParse(txtQty.Text.Trim(), out int adjustQty) || adjustQty <= 0)
            {
                MessageBox.Show("Please enter a valid quantity.", "Invalid Input", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (string.IsNullOrEmpty(cbCommands.Text))
            {
                MessageBox.Show("Please select a command (Add or Remove).", "Missing Command", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Validation logic for removal
            if (cbCommands.Text == "Remove from Inventory" && adjustQty > _qty)
            {
                MessageBox.Show("Stock on hand quantity (" + _qty + ") is less than adjustment quantity.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                using (cn = new SQLiteConnection(DBConnection.MyConnection()))
                {
                    cn.Open();
                    using (var transaction = cn.BeginTransaction())
                    {
                        // 1. Update stock quantity
                        string sqlUpdate = cbCommands.Text == "Remove from Inventory"
                            ? "UPDATE TblProduct1 SET qty = qty - @qty WHERE pcode = @pcode"
                            : "UPDATE TblProduct1 SET qty = qty + @qty WHERE pcode = @pcode";

                        using (cm = new SQLiteCommand(sqlUpdate, cn))
                        {
                            cm.Parameters.AddWithValue("@qty", adjustQty);
                            cm.Parameters.AddWithValue("@pcode", txtPcode.Text.Trim());
                            cm.ExecuteNonQuery();
                        }

                        // 2. Insert into adjustment log
                        string sqlInsert = @"
                            INSERT INTO tblAdjustment (referenceno, pcode, qty, action, remarks, sdate, [user])
                            VALUES (@ref, @pcode, @qty, @action, @remarks, @sdate, @user)";

                        using (cm = new SQLiteCommand(sqlInsert, cn))
                        {
                            cm.Parameters.AddWithValue("@ref", txtRef.Text.Trim());
                            cm.Parameters.AddWithValue("@pcode", txtPcode.Text.Trim());
                            cm.Parameters.AddWithValue("@qty", adjustQty);
                            cm.Parameters.AddWithValue("@action", cbCommands.Text.Trim());
                            cm.Parameters.AddWithValue("@remarks", txtRemarks.Text.Trim());
                            cm.Parameters.AddWithValue("@sdate", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                            cm.Parameters.AddWithValue("@user", txtUser.Text.Trim());
                            cm.ExecuteNonQuery();
                        }

                        transaction.Commit();
                    }
                }

                MessageBox.Show("Stock has been successfully adjusted.", "Process Completed", MessageBoxButtons.OK, MessageBoxIcon.Information);

                if (f != null) f.MyDashbord();
                LoadRecords();
                Clear();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        public void Clear()
        {
            txtdesc.Clear();
            txtPcode.Clear();
            txtQty.Clear();
            txtRemarks.Clear();
            cbCommands.SelectedIndex = -1;
            _qty = 0;
            referenceNo();
        }
    }
}