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
        private readonly string stitle = "PosSystem";
        private readonly string connStr;

        public frmStockin(string connectionString)
        {
            InitializeComponent();
            connStr = connectionString;
            LoadVendor();
        }

        private void frmStockin_Load(object sender, EventArgs e)
        {
            GenerateReferenceNo();
        }

        // Validates if there are rows in the grid
        private bool ValidateStockIn()
        {
            if (dataGridView2.Rows.Count == 0)
            {
                MessageBox.Show("No items to save.", stitle, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }
            return true;
        }

        // Atomic reference number generation
        public void GenerateReferenceNo()
        {
            try
            {
                string sdate = DateTime.Now.ToString("yyyyMMdd");
                int nextNumber = 1001;

                using (var cn = new SQLiteConnection(connStr))
                {
                    cn.Open();
                    using (var tran = cn.BeginTransaction())
                    {
                        using (var cmd = new SQLiteCommand("SELECT refno FROM tblStockIn WHERE refno LIKE @sdate ORDER BY id DESC LIMIT 1", cn, tran))
                        {
                            cmd.Parameters.Add("@sdate", DbType.String).Value = sdate + "%";
                            var lastRef = cmd.ExecuteScalar()?.ToString();
                            if (!string.IsNullOrEmpty(lastRef) && lastRef.Length >= 12)
                            {
                                if (int.TryParse(lastRef.Substring(8), out int lastCount))
                                    nextNumber = lastCount + 1;
                            }
                        }
                        txtRefNo.Text = sdate + nextNumber;
                        tran.Commit();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, stitle, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // Load pending stock-in items
        public void LoadStockIn()
        {
            dataGridView2.Rows.Clear();
            try
            {
                using (var cn = new SQLiteConnection(connStr))
                using (var cmd = new SQLiteCommand("SELECT * FROM tblStockIn WHERE refno = @refno AND status='Pending'", cn))
                {
                    cmd.Parameters.Add("@refno", DbType.String).Value = txtRefNo.Text;
                    cn.Open();
                    using (var dr = cmd.ExecuteReader())
                    {
                        int i = 0;
                        while (dr.Read())
                        {
                            i++;
                            dataGridView2.Rows.Add(
                              i,
                              dr["id"].ToString(),
                              dr["refno"].ToString(),
                              dr["pcode"].ToString(),
                              "",
                              "",
                              dr["qty"].ToString(),
                              dr["sdate"].ToString(),
                              dr["stockinby"].ToString()
                            );
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, stitle, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void dataGridView2_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || dataGridView2.Columns[e.ColumnIndex].Name != "Delete") return;

            if (MessageBox.Show("Remove this item?", stitle, MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                try
                {
                    using (var cn = new SQLiteConnection(connStr))
                    using (var cmd = new SQLiteCommand("DELETE FROM tblStockIn WHERE id=@id", cn))
                    {
                        cmd.Parameters.Add("@id", DbType.String).Value = dataGridView2.Rows[e.RowIndex].Cells[1].Value.ToString();
                        cn.Open();
                        cmd.ExecuteNonQuery();
                    }
                    LoadStockIn();
                    MessageBox.Show("Item deleted successfully.", stitle, MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, stitle, MessageBoxButtons.OK, MessageBoxIcon.Error);
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
            if (string.IsNullOrEmpty(txtRefNo.Text))
            {
                MessageBox.Show("Please generate a reference number first.", stitle, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            var frm = new frmSearchProductStokin(this);
            frm.LoadProduct();
            frm.ShowDialog();
        }

        // Async save with transaction & UI thread safety
        private async void btnSave_Click(object sender, EventArgs e)
        {
            if (!ValidateStockIn()) return;

            btnSave.Enabled = false;
            dataGridView2.Enabled = false;

            var stockItems = dataGridView2.Rows
              .Cast<DataGridViewRow>()
              .Where(r => !r.IsNewRow)
              .Select(r => new StockInModel
              {
                  ID = r.Cells[1].Value.ToString(),
                  PCode = r.Cells[3].Value.ToString(),
                  Qty = int.TryParse(r.Cells[6].Value?.ToString(), out int q) ? q : 0
              }).ToList();

            int vendorID = int.TryParse(lblVendorID.Text, out int vid) ? vid : 0;
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
                            foreach (var item in stockItems)
                            {
                                // Update product quantity
                                using (var cmd = new SQLiteCommand("UPDATE TblProduct1 SET qty = qty + @qty WHERE pcode=@pcode", cn, tran))
                                {
                                    cmd.Parameters.Add("@qty", DbType.Int32).Value = item.Qty;
                                    cmd.Parameters.Add("@pcode", DbType.String).Value = item.PCode;
                                    cmd.ExecuteNonQuery();
                                }

                                // Update stock-in status
                                using (var cmd = new SQLiteCommand("UPDATE tblStockIn SET status='Done', vendorid=@vendorid, stockinby=@stockinby WHERE id=@id", cn, tran))
                                {
                                    cmd.Parameters.Add("@id", DbType.String).Value = item.ID;
                                    cmd.Parameters.Add("@vendorid", DbType.Int32).Value = vendorID;
                                    cmd.Parameters.Add("@stockinby", DbType.String).Value = stockInBy;
                                    cmd.ExecuteNonQuery();
                                }
                            }
                            tran.Commit();
                        }
                    }
                });

                MessageBox.Show("Stock-in successfully saved!", stitle, MessageBoxButtons.OK, MessageBoxIcon.Information);
                Clear();
                LoadStockIn();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, stitle, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                btnSave.Enabled = true;
                dataGridView2.Enabled = true;
            }
        }

        // Load stock-in history
        private void LoadStockHistory()
        {
            dataGridView1.Rows.Clear();
            try
            {
                using (var cn = new SQLiteConnection(connStr))
                using (var cmd = new SQLiteCommand("SELECT * FROM tblStockIn WHERE DATE(sdate) BETWEEN @d1 AND @d2 AND status='Done'", cn))
                {
                    cmd.Parameters.Add("@d1", DbType.String).Value = date1.Value.ToString("yyyy-MM-dd");
                    cmd.Parameters.Add("@d2", DbType.String).Value = date2.Value.ToString("yyyy-MM-dd");
                    cn.Open();
                    using (var dr = cmd.ExecuteReader())
                    {
                        int i = 0;
                        while (dr.Read())
                        {
                            i++;
                            dataGridView1.Rows.Add(
                              i,
                              dr["id"].ToString(),
                              dr["refno"].ToString(),
                              dr["pcode"].ToString(),
                              "",
                              "",
                              dr["qty"].ToString(),
                              dr["sdate"].ToString(),
                              dr["stockinby"].ToString()
                            );
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, stitle, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void load_Click(object sender, EventArgs e) => LoadStockHistory();
        private void pictureBox1_Click(object sender, EventArgs e) => this.Dispose();

        public void LoadVendor()
        {
            cbVendor.Items.Clear();
            try
            {
                using (var cn = new SQLiteConnection(connStr))
                using (var cmd = new SQLiteCommand("SELECT * FROM tblVendor", cn))
                {
                    cn.Open();
                    using (var dr = cmd.ExecuteReader())
                    {
                        while (dr.Read())
                            cbVendor.Items.Add(dr["vendor"].ToString());
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Vendor Load Error: " + ex.Message, stitle, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void cbVendor_KeyPress(object sender, KeyPressEventArgs e) => e.Handled = true;

        private void cbVendor_TextChanged(object sender, EventArgs e)
        {
            try
            {
                using (var cn = new SQLiteConnection(connStr))
                using (var cmd = new SQLiteCommand("SELECT * FROM tblVendor WHERE vendor=@vendor", cn))
                {
                    cmd.Parameters.Add("@vendor", DbType.String).Value = cbVendor.Text;
                    cn.Open();
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
            catch { }
        }

        private void linkLabel2_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e) => GenerateReferenceNo();

        private void panel1_Paint(object sender, PaintEventArgs e) { }
        private void panel2_Paint(object sender, PaintEventArgs e) { }
        private void textBox2_TextChanged(object sender, EventArgs e) { }
        private void dataGridView1_CellContentClick(object sender, DataGridViewCellEventArgs e) { }
        private void cbVendor_SelectedIndexChanged(object sender, EventArgs e) { }
        private void txtSearch_Click(object sender, EventArgs e) { }
    }

    // Model class for Stock-In item
    public class StockInModel
    {
        public string ID { get; set; }
        public string PCode { get; set; }
        public int Qty { get; set; }
    }
}