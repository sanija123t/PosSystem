using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Data.SQLite; // MUST BE SQLite

namespace PosSystem
{
    public partial class frm_VendorList : Form
    {
        // Change these to SQLite types
        SQLiteConnection cn;
        SQLiteCommand cm;
        SQLiteDataReader dr;

        public frm_VendorList()
        {
            InitializeComponent();
            // Use the correct SQLite constructor
            cn = new SQLiteConnection(DBConnection.MyConnection());
        }

        public void LoadRecords()
        {
            try
            {
                dataGridView1.Rows.Clear();
                int i = 0;
                cn.Open();
                cm = new SQLiteCommand("SELECT * FROM tblVendor ORDER BY vendor ASC", cn);
                dr = cm.ExecuteReader();
                while (dr.Read())
                {
                    i++;
                    dataGridView1.Rows.Add(i, dr["id"].ToString(), dr["vendor"].ToString(), dr["address"].ToString(), dr["contactperson"].ToString(), dr["telephone"].ToString(), dr["email"].ToString(), dr["fax"].ToString());
                }
                dr.Close();
                cn.Close();
            }
            catch (Exception ex)
            {
                if (cn.State == ConnectionState.Open) cn.Close();
                MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void pictureBox2_Click(object sender, EventArgs e)
        {
            frmVendor frm = new frmVendor(this);
            frm.btnSave.Enabled = true;
            frm.btnUpdate.Enabled = false;
            frm.ShowDialog();
        }

        private void pictureBox1_Click(object sender, EventArgs e)
        {
            this.Dispose();
        }

        private void frm_VendorList_Load(object sender, EventArgs e)
        {
            LoadRecords();
        }

        private void dataGridView1_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;

            string colname = dataGridView1.Columns[e.ColumnIndex].Name;
            if (colname == "Edit")
            {
                frmVendor f = new frmVendor(this);
                f.lblD.Text = dataGridView1.Rows[e.RowIndex].Cells[1].Value.ToString();
                f.txtVendor.Text = dataGridView1.Rows[e.RowIndex].Cells[2].Value.ToString();
                f.txtAddress.Text = dataGridView1.Rows[e.RowIndex].Cells[3].Value.ToString();
                f.txtContactPreson.Text = dataGridView1.Rows[e.RowIndex].Cells[4].Value.ToString();
                f.txtTelephone.Text = dataGridView1.Rows[e.RowIndex].Cells[5].Value.ToString();
                f.txtEmail.Text = dataGridView1.Rows[e.RowIndex].Cells[6].Value.ToString();
                f.txtFax.Text = dataGridView1.Rows[e.RowIndex].Cells[7].Value.ToString();

                f.btnSave.Enabled = false;
                f.btnUpdate.Enabled = true;
                f.ShowDialog();
            }
            else if (colname == "Delete")
            {
                if (MessageBox.Show("Delete this record?", "Delete", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    try
                    {
                        cn.Open();
                        cm = new SQLiteCommand("DELETE FROM tblVendor WHERE id = @id", cn);
                        cm.Parameters.AddWithValue("@id", dataGridView1.Rows[e.RowIndex].Cells[1].Value.ToString());
                        cm.ExecuteNonQuery();
                        cn.Close();
                        MessageBox.Show("Record deleted!", "POS", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        LoadRecords();
                    }
                    catch (Exception ex)
                    {
                        if (cn.State == ConnectionState.Open) cn.Close();
                        MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }
    }
}