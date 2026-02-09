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
    public partial class frmUserAccount : Form
    {
        SQLiteConnection cn = new SQLiteConnection();
        SQLiteCommand cm = new SQLiteCommand();
        SQLiteDataReader dr;
        Form1 f;

        public frmUserAccount(Form1 f)
        {
            InitializeComponent();
            cn = new SQLiteConnection(DBConnection.MyConnection());
            this.f = f;
        }

        private void pictureBox2_Click(object sender, EventArgs e)
        {
            this.Dispose();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            try
            {
                if (txtPassword.Text != txtRePassword.Text)
                {
                    MessageBox.Show("Password did not match!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                cn.Open();
                cm = new SQLiteCommand("INSERT INTO tblUser (username, password, role, name) VALUES (@username, @password, @role, @name)", cn);
                cm.Parameters.AddWithValue("@username", txtUser.Text);
                cm.Parameters.AddWithValue("@password", DBConnection.GetHash(txtPassword.Text));
                cm.Parameters.AddWithValue("@role", cbRole.Text);
                cm.Parameters.AddWithValue("@name", txtName.Text);
                cm.ExecuteNonQuery();
                cn.Close();
                MessageBox.Show("New Account has saved!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                Clear();
            }
            catch (Exception ex)
            {
                cn.Close();
                MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void Clear()
        {
            txtName.Clear();
            txtPassword.Clear();
            txtRePassword.Clear();
            txtUser.Clear();
            cbRole.Text = "";
            txtUser.Focus();
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            Clear();
        }

        private void btnSav_Click(object sender, EventArgs e)
        {
            try
            {
                if (DBConnection.GetHash(txtOld.Text) != f._pass)
                {
                    MessageBox.Show("Old password did not match!", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                if (txtNew.Text != txtRePass.Text)
                {
                    MessageBox.Show("Confirm new password did not match!", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                cn.Open();
                cm = new SQLiteCommand("UPDATE tblUser SET password = @password WHERE username = @username", cn);
                cm.Parameters.AddWithValue("@password", DBConnection.GetHash(txtNew.Text));
                cm.Parameters.AddWithValue("@username", txtU.Text);

                cm.ExecuteNonQuery();
                cn.Close();
                MessageBox.Show("Password has been successfully changed!", "Done", MessageBoxButtons.OK, MessageBoxIcon.Information);

                txtRePass.Clear();
                txtOld.Clear();
                txtNew.Clear();
            }
            catch (Exception ex)
            {
                cn.Close();
                MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void txtuser2_TextChanged(object sender, EventArgs e)
        {
            try
            {
                cn.Open();
                cm = new SQLiteCommand("SELECT isactive FROM tblUser WHERE username = @username", cn);
                cm.Parameters.AddWithValue("@username", txtuser2.Text);
                dr = cm.ExecuteReader();
                dr.Read();
                if (dr.HasRows)
                {
                    checkBox1.Checked = dr["isactive"].ToString() == "1" ? true : false;
                }
                else
                {
                    checkBox1.Checked = false;
                }
                dr.Close();
                cn.Close();
            }
            catch (Exception ex)
            {
                cn.Close();
                MessageBox.Show(ex.Message, "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            try
            {
                cn.Open();
                cm = new SQLiteCommand("UPDATE tblUser SET isactive = @isactive WHERE username = @username", cn);
                cm.Parameters.AddWithValue("@isactive", checkBox1.Checked ? 1 : 0);
                cm.Parameters.AddWithValue("@username", txtuser2.Text);
                int i = cm.ExecuteNonQuery();
                cn.Close();

                if (i > 0)
                {
                    MessageBox.Show("Account status has been successfully updated", "Account", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    txtuser2.Clear();
                    checkBox1.Checked = false;
                }
                else
                {
                    MessageBox.Show("Account does not exist!", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            catch (Exception ex)
            {
                cn.Close();
                MessageBox.Show(ex.Message, "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void frmUserAccount_Load(object sender, EventArgs e) { }
        private void frmUserAccount_Resize(object sender, EventArgs e) { }
        private void panel2_Paint(object sender, PaintEventArgs e) { }
        private void tabPage2_Click_1(object sender, EventArgs e) { }
        private void txtOld_TextChanged(object sender, EventArgs e) { }
        private void label3_Click(object sender, EventArgs e) { }
        private void textBox2_TextChanged(object sender, EventArgs e) { }
    }
}