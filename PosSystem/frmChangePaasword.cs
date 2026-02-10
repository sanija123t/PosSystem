using System;
using System.Data.SQLite;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PosSystem
{
    public partial class frmChangePaasword : Form
    {
        private readonly Form1 f;
        private readonly string stitle = "POS System";

        public frmChangePaasword(Form1 frm)
        {
            InitializeComponent();
            f = frm;
            KeyPreview = true;
        }

        private void frmChangePaasword_Load(object sender, EventArgs e)
        {
            txtOld.Focus();
        }

        // =========================
        // 🔹 SAVE PASSWORD ASYNC
        // =========================
        private async void btnSave_Click(object sender, EventArgs e)
        {
            string oldPass = txtOld.Text.Trim();
            string newPass = txtNew.Text.Trim();
            string confirmPass = txtConfirm.Text.Trim();

            // Defensive validation
            if (string.IsNullOrWhiteSpace(oldPass) || string.IsNullOrWhiteSpace(newPass) || string.IsNullOrWhiteSpace(confirmPass))
            {
                MessageBox.Show("Please fill in all fields.", stitle, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (newPass != confirmPass)
            {
                MessageBox.Show("Confirm new password did not match!", stitle, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            btnSave.Enabled = false;

            try
            {
                string hashedOldInput = DBConnection.GetHash(oldPass);
                string username = f.lblUserName.Text;
                bool oldPasswordCorrect = false;

                using (var cn = new SQLiteConnection(DBConnection.MyConnection()))
                {
                    await cn.OpenAsync();

                    // Check old password
                    using (var checkCmd = new SQLiteCommand("SELECT password FROM tblUser WHERE username = @username", cn))
                    {
                        checkCmd.Parameters.AddWithValue("@username", username);
                        var result = await checkCmd.ExecuteScalarAsync();

                        if (result != null && result.ToString() == hashedOldInput)
                            oldPasswordCorrect = true;
                    }

                    if (!oldPasswordCorrect)
                    {
                        MessageBox.Show("Old password did not match!", stitle, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }

                    // Update new password
                    if (MessageBox.Show("Change Password?", stitle, MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                    {
                        using (var updateCmd = new SQLiteCommand("UPDATE tblUser SET password = @password WHERE username = @username", cn))
                        {
                            updateCmd.Parameters.AddWithValue("@password", DBConnection.GetHash(newPass));
                            updateCmd.Parameters.AddWithValue("@username", username);
                            await updateCmd.ExecuteNonQueryAsync();
                        }

                        MessageBox.Show("Password updated successfully!", stitle, MessageBoxButtons.OK, MessageBoxIcon.Information);
                        Dispose();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                btnSave.Enabled = true;
            }
        }

        private void pictureBox1_Click(object sender, EventArgs e) => Dispose();

        private void frmChangePaasword_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape) Dispose();
            else if (e.KeyCode == Keys.Enter) btnSave_Click(sender, e);
        }
    }
}