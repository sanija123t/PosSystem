using System;
using System.Data;
using System.Windows.Forms;
using System.Data.SQLite;

namespace PosSystem
{
    public partial class frmVendor : Form
    {
        frm_VendorList f;
        SQLiteConnection cn;
        SQLiteCommand cm;

        public frmVendor(frm_VendorList f)
        {
            InitializeComponent();
            cn = new SQLiteConnection(DBConnection.MyConnection());
            this.f = f;
        }

        // Renamed from btnSave_Click to button1_Click to fix Designer Error CS1061
        private void button1_Click(object sender, EventArgs e)
        {
            try
            {
                if (MessageBox.Show("Save this record? click yes to Confirm", "Confirm", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    cn.Open();
                    cm = new SQLiteCommand("INSERT INTO tblVendor(vendor,address,contactperson,telephone,email,fax) values (@vendor,@address,@contactperson,@telephone,@email,@fax)", cn);
                    cm.Parameters.AddWithValue("@vendor", txtVendor.Text);
                    cm.Parameters.AddWithValue("@address", txtAddress.Text);
                    cm.Parameters.AddWithValue("@contactperson", txtContactPreson.Text);
                    cm.Parameters.AddWithValue("@telephone", txtTelephone.Text);
                    cm.Parameters.AddWithValue("@email", txtEmail.Text);
                    cm.Parameters.AddWithValue("@fax", txtFax.Text);
                    cm.ExecuteNonQuery();
                    cn.Close();

                    MessageBox.Show("Record has been successfully saved.", "POS System", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    Clear();
                    f.LoadRecords();
                }
            }
            catch (Exception ex)
            {
                if (cn.State == ConnectionState.Open) cn.Close();
                MessageBox.Show(ex.Message, "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void btnUpdate_Click(object sender, EventArgs e)
        {
            try
            {
                if (MessageBox.Show("Update this record? Click yes to Confirm", "Confirm", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    cn.Open();
                    cm = new SQLiteCommand("UPDATE tblVendor SET vendor = @vendor, address = @address, contactperson = @contactperson, telephone = @telephone, email= @email, fax= @fax WHERE id = @id", cn);
                    cm.Parameters.AddWithValue("@vendor", txtVendor.Text);
                    cm.Parameters.AddWithValue("@address", txtAddress.Text);
                    cm.Parameters.AddWithValue("@contactperson", txtContactPreson.Text);
                    cm.Parameters.AddWithValue("@telephone", txtTelephone.Text);
                    cm.Parameters.AddWithValue("@email", txtEmail.Text);
                    cm.Parameters.AddWithValue("@fax", txtFax.Text);
                    cm.Parameters.AddWithValue("@id", lblD.Text);
                    cm.ExecuteNonQuery();
                    cn.Close();

                    MessageBox.Show("Record has been successfully updated.", "POS System", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    Clear();
                    f.LoadRecords();
                    this.Dispose();
                }
            }
            catch (Exception ex)
            {
                if (cn.State == ConnectionState.Open) cn.Close();
                MessageBox.Show(ex.Message, "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        // Added missing Load event to fix Designer Error CS1061
        private void frmVendor_Load(object sender, EventArgs e)
        {
        }

        public void Clear()
        {
            txtAddress.Clear();
            txtEmail.Clear();
            txtFax.Clear();
            txtContactPreson.Clear();
            txtTelephone.Clear();
            txtVendor.Clear();
            txtVendor.Focus();
            btnSave.Enabled = true;
            btnUpdate.Enabled = false;
        }

        private void pictureBox2_Click(object sender, EventArgs e)
        {
            this.Dispose();
        }
    }
}