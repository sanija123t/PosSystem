using System;
using System.Data.SQLite;
using System.Windows.Forms;

namespace PosSystem
{
    public partial class frmChangePaasword : Form
    {
        private SQLiteConnection cn;
        private SQLiteCommand cm;
        // Reference to Form1 (which seems to be your POS form)
        private readonly Form1 f;

        public frmChangePaasword(Form1 frm)
        {
            InitializeComponent();
            // Use the class name DBConnection directly because it is static
            cn = new SQLiteConnection(DBConnection.MyConnection());
            f = frm;
        }

        private void frmChangePaasword_Load(object sender, EventArgs e)
        {
            // You can leave this empty or add initialization logic here
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(txtOld.Text) || string.IsNullOrWhiteSpace(txtNew.Text))
                {
                    MessageBox.Show("Please fill in all fields.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                string hashedOldInput = DBConnection.GetHash(txtOld.Text);
                bool oldPasswordCorrect = false;

                cn.Open();
                // We use f.lblUser.Text to find the right person
                string checkQuery = "SELECT password FROM tblUser WHERE username = @username";
                using (SQLiteCommand checkCmd = new SQLiteCommand(checkQuery, cn))
                {
                    checkCmd.Parameters.AddWithValue("@username", f.lblUserName.Text);
                    object result = checkCmd.ExecuteScalar();

                    if (result != null && result.ToString() == hashedOldInput)
                    {
                        oldPasswordCorrect = true;
                    }
                }
                cn.Close();

                if (!oldPasswordCorrect)
                {
                    MessageBox.Show("Old password did not match!", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                if (txtNew.Text != txtConfirm.Text)
                {
                    MessageBox.Show("Confirm new password did not match!", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                if (MessageBox.Show("Change Password?", "Confirm", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    cn.Open();
                    cm = new SQLiteCommand("UPDATE tblUser SET password = @password WHERE username = @username", cn);
                    cm.Parameters.AddWithValue("@password", DBConnection.GetHash(txtNew.Text));
                    cm.Parameters.AddWithValue("@username", f.lblUserName.Text);
                    cm.ExecuteNonQuery();
                    cn.Close();

                    MessageBox.Show("Password updated!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    this.Dispose();
                }
            }
            catch (Exception ex)
            {
                if (cn.State == System.Data.ConnectionState.Open) cn.Close();
                MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void pictureBox1_Click(object sender, EventArgs e) => this.Dispose();
    }
}