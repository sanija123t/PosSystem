using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PosSystem
{
    public partial class frmStockin : Form
    {
        private readonly string stitle = "POS System Management";
        private readonly string connStr;

        public frmStockin(string connectionString)
        {
            InitializeComponent();
            connStr = connectionString;
            if (!string.IsNullOrEmpty(connStr))
            {
                LoadVendor();
            }
        }

        private void frmStockin_Load(object sender, EventArgs e)
        {
            dt1.Value = DateTime.Now;
            date1.Value = DateTime.Now.AddDays(-7);
            date2.Value = DateTime.Now;
            GenerateReferenceNo();
        }

        public void GenerateReferenceNo()
        {
            try
            {
                string sdate = dt1.Value.ToString("yyyyMMdd");
                int nextNumber = 1001;

                using (var cn = new SQLiteConnection(connStr))
                {
                    cn.Open();
                    // Refined query to find the max suffix for TODAY'S date only
                    string query = "SELECT refno FROM tblStockIn WHERE refno LIKE @sdate ORDER BY refno DESC LIMIT 1";
                    using (var cmd = new SQLiteCommand(query, cn))
                    {
                        cmd.Parameters.AddWithValue("@sdate", sdate + "%");
                        var lastRef = cmd.ExecuteScalar()?.ToString();

                        if (!string.IsNullOrEmpty(lastRef) && lastRef.Length >= 12)
                        {
                            // Safely extract the numeric part
                            string suffix = lastRef.Substring(8);
                            if (int.TryParse(suffix, out int lastCount))
                                nextNumber = lastCount + 1;
                        }
                    }
                }
                txtRefNo.Text = sdate + nextNumber;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ref Error: " + ex.Message, stitle, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        public void LoadStockIn()
        {
            dataGridView2.Rows.Clear();
            try
            {
                using (var cn = new SQLiteConnection(connStr))
                {
                    string query = @"SELECT s.id, s.refno, s.pcode, p.pdesc, p.category, s.qty, s.sdate, s.stockinby 
                                   FROM tblStockIn s 
                                   INNER JOIN TblProduct1 p ON s.pcode = p.pcode 
                                   WHERE s.refno = @refno AND s.status = 'Pending'";

                    using (var cmd = new SQLiteCommand(query, cn))
                    {
                        cmd.Parameters.AddWithValue("@refno", txtRefNo.Text);
                        cn.Open();
                        using (var dr = cmd.ExecuteReader())
                        {
                            int i = 0;
                            while (dr.Read())
                            {
                                i++;
                                dataGridView2.Rows.Add(i, dr["id"], dr["refno"], dr["pcode"], dr["pdesc"], dr["category"], dr["qty"], dr["sdate"], dr["stockinby"]);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Load Error: " + ex.Message, stitle, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async void btnSave_Click(object sender, EventArgs e)
        {
            if (dataGridView2.Rows.Count == 0)
            {
                MessageBox.Show("No items in the list to save.", stitle, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(cbVendor.Text) || string.IsNullOrWhiteSpace(txtBy.Text))
            {
                MessageBox.Show("Please fill in Vendor and 'Stock In By' fields.", stitle, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (MessageBox.Show("Are you sure you want to save this stock entry?", stitle, MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;

            // ELITE FIX: Capture UI values BEFORE entering the background thread
            string vendorID = lblVendorID.Text;
            string stockInBy = txtBy.Text;

            try
            {
                await Task.Run(() =>
                {
                    using (var cn = new SQLiteConnection(connStr))
                    {
                        cn.Open();
                        using (var tran = cn.BeginTransaction())
                        {
                            try
                            {
                                foreach (DataGridViewRow row in dataGridView2.Rows)
                                {
                                    if (row.Cells[1].Value == null || row.Cells[3].Value == null) continue;

                                    string id = row.Cells[1].Value.ToString();
                                    string pcode = row.Cells[3].Value.ToString();
                                    int qty = Convert.ToInt32(row.Cells[6].Value);

                                    // 1. Update Product Quantity
                                    using (var cmd = new SQLiteCommand("UPDATE TblProduct1 SET qty = qty + @qty WHERE pcode = @pcode", cn, tran))
                                    {
                                        cmd.Parameters.AddWithValue("@qty", qty);
                                        cmd.Parameters.AddWithValue("@pcode", pcode);
                                        cmd.ExecuteNonQuery();
                                    }

                                    // 2. Mark StockIn as Done
                                    using (var cmd = new SQLiteCommand("UPDATE tblStockIn SET status = 'Done', vendorid = @vid, stockinby = @by WHERE id = @id", cn, tran))
                                    {
                                        cmd.Parameters.AddWithValue("@vid", vendorID);
                                        cmd.Parameters.AddWithValue("@by", stockInBy);
                                        cmd.Parameters.AddWithValue("@id", id);
                                        cmd.ExecuteNonQuery();
                                    }
                                }
                                tran.Commit();
                            }
                            catch { tran.Rollback(); throw; }
                        }
                    }
                });

                MessageBox.Show("Stock records updated successfully.", stitle, MessageBoxButtons.OK, MessageBoxIcon.Information);
                Clear();
                LoadStockIn();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Save Error: " + ex.Message, stitle, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        public void LoadStockHistory()
        {
            dataGridView1.Rows.Clear();
            try
            {
                using (var cn = new SQLiteConnection(connStr))
                {
                    string query = @"SELECT s.id, s.refno, s.pcode, p.pdesc, p.category, s.qty, s.sdate, s.stockinby 
                                   FROM tblStockIn s 
                                   INNER JOIN TblProduct1 p ON s.pcode = p.pcode 
                                   WHERE DATE(s.sdate) BETWEEN @d1 AND @d2 AND s.status = 'Done'";

                    using (var cmd = new SQLiteCommand(query, cn))
                    {
                        cmd.Parameters.AddWithValue("@d1", date1.Value.ToString("yyyy-MM-dd"));
                        cmd.Parameters.AddWithValue("@d2", date2.Value.ToString("yyyy-MM-dd"));
                        cn.Open();
                        using (var dr = cmd.ExecuteReader())
                        {
                            int i = 0;
                            while (dr.Read())
                            {
                                i++;
                                dataGridView1.Rows.Add(i, dr["id"], dr["refno"], dr["pcode"], dr["pdesc"], dr["category"], dr["qty"], dr["sdate"], dr["stockinby"]);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("History Error: " + ex.Message, stitle, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void load_Click(object sender, EventArgs e) => LoadStockHistory();

        private void dataGridView2_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
            string colName = dataGridView2.Columns[e.ColumnIndex].Name;

            if (colName == "Delete")
            {
                if (MessageBox.Show("Remove this item from pending list?", stitle, MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    using (var cn = new SQLiteConnection(connStr))
                    {
                        cn.Open();
                        using (var cmd = new SQLiteCommand("DELETE FROM tblStockIn WHERE id = @id", cn))
                        {
                            cmd.Parameters.AddWithValue("@id", dataGridView2.Rows[e.RowIndex].Cells[1].Value);
                            cmd.ExecuteNonQuery();
                        }
                    }
                    LoadStockIn();
                }
            }
        }

        public void LoadVendor()
        {
            cbVendor.Items.Clear();
            using (var cn = new SQLiteConnection(connStr))
            {
                cn.Open();
                using (var cmd = new SQLiteCommand("SELECT vendor FROM tblVendor", cn))
                using (var dr = cmd.ExecuteReader())
                {
                    while (dr.Read()) cbVendor.Items.Add(dr["vendor"].ToString());
                }
            }
        }

        private void cbVendor_TextChanged(object sender, EventArgs e)
        {
            using (var cn = new SQLiteConnection(connStr))
            {
                cn.Open();
                using (var cmd = new SQLiteCommand("SELECT * FROM tblVendor WHERE vendor = @v", cn))
                {
                    cmd.Parameters.AddWithValue("@v", cbVendor.Text);
                    using (var dr = cmd.ExecuteReader())
                    {
                        if (dr.Read())
                        {
                            lblVendorID.Text = dr["id"].ToString();
                            txtAddress.Text = dr["address"].ToString();
                            txtPerson.Text = dr["contactperson"].ToString();
                        }
                    }
                }
            }
        }

        private void Clear()
        {
            txtBy.Clear();
            dt1.Value = DateTime.Now;
            GenerateReferenceNo();
        }

        private void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            var frm = new frmSearchProductStokin(this);
            frm.ShowDialog();
        }

        private void linkLabel2_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e) => GenerateReferenceNo();
        private void pictureBox1_Click(object sender, EventArgs e) => this.Dispose();
        private void cbVendor_KeyPress(object sender, KeyPressEventArgs e) => e.Handled = true;

        // --- ELITE FIX: Placeholder Methods for Designer Compatibility ---
        private void txtRefNo_TextChanged(object sender, EventArgs e) { }
        private void dt1_ValueChanged(object sender, EventArgs e) { }
        private void date1_ValueChanged(object sender, EventArgs e) { }
        private void date2_ValueChanged(object sender, EventArgs e) { }
        private void dataGridView1_CellContentClick(object sender, DataGridViewCellEventArgs e) { }
        private void cbVendor_SelectedIndexChanged(object sender, EventArgs e) { }
        private void panel1_Paint(object sender, PaintEventArgs e) { }
        private void panel2_Paint(object sender, PaintEventArgs e) { }
        private void textBox2_TextChanged(object sender, EventArgs e) { }
    }

    public class StockInModel
    {
        public string ID { get; set; }
        public string PCode { get; set; }
        public int Qty { get; set; }
    }
}