using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Data.SQLite;

namespace PosSystem
{
    public partial class frmStockin : Form
    {
        SQLiteConnection cn;
        SQLiteCommand cm;
        SQLiteDataReader dr;
        string stitle = "PosSystem";

        public frmStockin()
        {
            InitializeComponent();
            cn = new SQLiteConnection(DBConnection.MyConnection());
            LoadVendor();
        }

        private void frmStockin_Load(object sender, EventArgs e)
        {
            GenerateReferenceNo();
        }

        // Automated Reference Number Generation (Safe parsing)
        public void GenerateReferenceNo()
        {
            try
            {
                string sdate = DateTime.Now.ToString("yyyyMMdd");
                int count = 1000; // default starting number
                cn.Open();
                cm = new SQLiteCommand("SELECT refno FROM tblStockIn WHERE refno LIKE @sdate ORDER BY id DESC LIMIT 1", cn);
                cm.Parameters.AddWithValue("@sdate", sdate + "%");
                dr = cm.ExecuteReader();
                if (dr.Read())
                {
                    string lastRef = dr[0].ToString();
                    if (lastRef.Length >= 12 && int.TryParse(lastRef.Substring(8), out int lastCount))
                    {
                        count = lastCount;
                    }
                }
                txtRefNo.Text = sdate + (count + 1);
                dr.Close();
                cn.Close();
            }
            catch (Exception ex)
            {
                if (cn.State == ConnectionState.Open) cn.Close();
                MessageBox.Show(ex.Message, stitle, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        public void LoadStockIn()
        {
            try
            {
                int i = 0;
                dataGridView2.Rows.Clear();
                cn.Open();
                cm = new SQLiteCommand("SELECT * FROM tblStockIn WHERE refno LIKE @refno AND status = 'Pending'", cn);
                cm.Parameters.AddWithValue("@refno", txtRefNo.Text);
                dr = cm.ExecuteReader();
                while (dr.Read())
                {
                    i++;
                    dataGridView2.Rows.Add(i, dr["id"].ToString(), dr["refno"].ToString(), dr["pcode"].ToString(), "", "", dr["qty"].ToString(), dr["sdate"].ToString(), dr["stockinby"].ToString());
                }
                dr.Close();
                cn.Close();
            }
            catch (Exception ex)
            {
                if (cn.State == ConnectionState.Open) cn.Close();
                MessageBox.Show(ex.Message, stitle, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void dataGridView2_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
            string colName = dataGridView2.Columns[e.ColumnIndex].Name;
            if (colName == "Delete")
            {
                if (MessageBox.Show("Remove this item?", stitle, MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    cn.Open();
                    cm = new SQLiteCommand("DELETE FROM tblStockIn WHERE id = @id", cn);
                    cm.Parameters.AddWithValue("@id", dataGridView2.Rows[e.RowIndex].Cells[1].Value.ToString());
                    cm.ExecuteNonQuery();
                    cn.Close();
                    MessageBox.Show("Item has been successfully deleted", stitle, MessageBoxButtons.OK, MessageBoxIcon.Information);
                    LoadStockIn();
                }
            }
        }

        public void Clear()
        {
            txtBy.Clear();
            txtRefNo.Clear();
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
            frmSearchProductStokin frm = new frmSearchProductStokin(this);
            frm.LoadProduct();
            frm.ShowDialog();
        }

        // Async Stock-in Save with Transaction & Safe UI
        private async void btnSave_Click(object sender, EventArgs e)
        {
            if (dataGridView2.Rows.Count == 0) return;

            if (MessageBox.Show("Are you sure you want to save this record?", stitle, MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                return;

            btnSave.Enabled = false;
            dataGridView2.Enabled = false; // prevent edits while saving

            try
            {
                await Task.Run(() =>
                {
                    cn.Open();
                    using (var transaction = cn.BeginTransaction())
                    {
                        try
                        {
                            foreach (DataGridViewRow row in dataGridView2.Rows)
                            {
                                if (row.IsNewRow) continue;
                                if (!int.TryParse(row.Cells[6].Value?.ToString(), out int qty)) qty = 0;
                                string pcode = row.Cells[3].Value.ToString();
                                string stockInID = row.Cells[1].Value.ToString();

                                // Update product qty
                                using (SQLiteCommand cmd = new SQLiteCommand("UPDATE TblProduct1 SET qty = qty + @qty WHERE pcode = @pcode", cn))
                                {
                                    cmd.Parameters.AddWithValue("@qty", qty);
                                    cmd.Parameters.AddWithValue("@pcode", pcode);
                                    cmd.ExecuteNonQuery();
                                }

                                // Update stockin status
                                using (SQLiteCommand cmd = new SQLiteCommand("UPDATE tblStockIn SET status = 'Done' WHERE id = @id", cn))
                                {
                                    cmd.Parameters.AddWithValue("@id", stockInID);
                                    cmd.ExecuteNonQuery();
                                }
                            }

                            transaction.Commit();
                        }
                        catch (Exception ex)
                        {
                            transaction.Rollback();
                            throw ex;
                        }
                        finally
                        {
                            cn.Close();
                        }
                    }
                });

                MessageBox.Show("Stock in successfully saved!", stitle, MessageBoxButtons.OK, MessageBoxIcon.Information);
                Clear();
                LoadStockIn();
            }
            catch (Exception ex)
            {
                if (cn.State == System.Data.ConnectionState.Open) cn.Close();
                MessageBox.Show(ex.Message, stitle, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            finally
            {
                btnSave.Enabled = true;
                dataGridView2.Enabled = true;
            }
        }

        private void LoadStockHistory()
        {
            try
            {
                int i = 0;
                dataGridView1.Rows.Clear();
                cn.Open();
                cm = new SQLiteCommand("SELECT * FROM tblStockIn WHERE sdate BETWEEN @d1 AND @d2 AND status = 'Done'", cn);
                cm.Parameters.AddWithValue("@d1", date1.Value.ToString("yyyy-MM-dd"));
                cm.Parameters.AddWithValue("@d2", date2.Value.ToString("yyyy-MM-dd"));
                dr = cm.ExecuteReader();
                while (dr.Read())
                {
                    i++;
                    dataGridView1.Rows.Add(i, dr["id"].ToString(), dr["refno"].ToString(), dr["pcode"].ToString(), "", "", dr["qty"].ToString(), dr["sdate"].ToString(), dr["stockinby"].ToString());
                }
                dr.Close();
                cn.Close();
            }
            catch (Exception ex)
            {
                if (cn.State == ConnectionState.Open) cn.Close();
                MessageBox.Show(ex.Message, stitle, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void load_Click(object sender, EventArgs e)
        {
            LoadStockHistory();
        }

        private void pictureBox1_Click(object sender, EventArgs e)
        {
            this.Dispose();
        }

        public void LoadVendor()
        {
            try
            {
                cbVendor.Items.Clear();
                cn.Open();
                cm = new SQLiteCommand("SELECT * FROM tblVendor", cn);
                dr = cm.ExecuteReader();
                while (dr.Read())
                {
                    cbVendor.Items.Add(dr["vendor"].ToString());
                }
                dr.Close();
                cn.Close();
            }
            catch (Exception ex)
            {
                if (cn.State == ConnectionState.Open) cn.Close();
                MessageBox.Show("Vendor Load Error: " + ex.Message, stitle, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void cbVendor_KeyPress(object sender, KeyPressEventArgs e)
        {
            e.Handled = true;
        }

        private void cbVendor_TextChanged(object sender, EventArgs e)
        {
            try
            {
                cn.Open();
                cm = new SQLiteCommand("SELECT * FROM tblVendor WHERE vendor = @vendor", cn);
                cm.Parameters.AddWithValue("@vendor", cbVendor.Text);
                dr = cm.ExecuteReader();
                if (dr.Read())
                {
                    lblVendorID.Text = dr["id"].ToString();
                    txtAddress.Text = dr["address"].ToString();
                    txtPerson.Text = dr["contactperson"].ToString();
                }
                dr.Close();
                cn.Close();
            }
            catch
            {
                if (cn.State == ConnectionState.Open) cn.Close();
            }
        }

        private void linkLabel2_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            GenerateReferenceNo();
        }

        private void panel1_Paint(object sender, PaintEventArgs e) { }
        private void panel2_Paint(object sender, PaintEventArgs e) { }
        private void textBox2_TextChanged(object sender, EventArgs e) { }
        private void dataGridView1_CellContentClick(object sender, DataGridViewCellEventArgs e) { }
        private void cbVendor_SelectedIndexChanged(object sender, EventArgs e) { }
        private void txtSearch_Click(object sender, EventArgs e) { }
    }
}