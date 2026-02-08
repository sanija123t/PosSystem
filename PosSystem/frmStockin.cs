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
        }

        private void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            frmSearchProductStokin frm = new frmSearchProductStokin(this);
            frm.LoadProduct();
            frm.ShowDialog();
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            try
            {
                if (dataGridView2.Rows.Count > 0)
                {
                    if (MessageBox.Show("Are you sure you want to save this record?", stitle, MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                    {
                        for (int i = 0; i < dataGridView2.Rows.Count; i++)
                        {
                            cn.Open();
                            cm = new SQLiteCommand("UPDATE TblProduct1 SET qty = qty + @qty WHERE pcode = @pcode", cn);
                            cm.Parameters.AddWithValue("@qty", int.Parse(dataGridView2.Rows[i].Cells[6].Value.ToString()));
                            cm.Parameters.AddWithValue("@pcode", dataGridView2.Rows[i].Cells[3].Value.ToString());
                            cm.ExecuteNonQuery();
                            cn.Close();

                            cn.Open();
                            cm = new SQLiteCommand("UPDATE tblStockIn SET status = 'Done' WHERE id = @id", cn);
                            cm.Parameters.AddWithValue("@id", dataGridView2.Rows[i].Cells[1].Value.ToString());
                            cm.ExecuteNonQuery();
                            cn.Close();
                        }
                        MessageBox.Show("Stock in successfully saved!", stitle, MessageBoxButtons.OK, MessageBoxIcon.Information);
                        Clear();
                        LoadStockIn();
                    }
                }
            }
            catch (Exception ex)
            {
                if (cn.State == ConnectionState.Open) cn.Close();
                MessageBox.Show(ex.Message, stitle, MessageBoxButtons.OK, MessageBoxIcon.Warning);
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
            Random rnd = new Random();
            txtRefNo.Text = "REF" + rnd.Next(100000, 999999).ToString();
        }

        private void panel1_Paint(object sender, PaintEventArgs e) { }
        private void panel2_Paint(object sender, PaintEventArgs e) { }
        private void textBox2_TextChanged(object sender, EventArgs e) { }
        private void dataGridView1_CellContentClick(object sender, DataGridViewCellEventArgs e) { }
        private void cbVendor_SelectedIndexChanged(object sender, EventArgs e) { }
        private void txtSearch_Click(object sender, EventArgs e) { }
    }
}