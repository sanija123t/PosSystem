using System;
using System.Data.SQLite;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PosSystem
{
    public partial class frmUserAccount : Form
    {
        private readonly Form1 f;

        public frmUserAccount(Form1 f)
        {
            InitializeComponent();
            this.f = f;

            // Enter key triggers save on "New Account" tab
            this.AcceptButton = button1;
        }

        private void pictureBox2_Click(object sender, EventArgs e) => this.Dispose();

        // Elite async save for New Account (with salt & hashing)
        private async void button1_Click(object sender, EventArgs e)
        {
            string username = txtUser.Text.Trim();
            string password = txtPassword.Text;
            string rePassword = txtRePassword.Text;
            string role = cbRole.Text;
            string name = txtName.Text.Trim();

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password) || string.IsNullOrEmpty(rePassword) || string.IsNullOrEmpty(role))
            {
                MessageBox.Show("Please fill all required fields", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (password != rePassword)
            {
                MessageBox.Show("Passwords do not match!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            button1.Enabled = false;

            try
            {
                await Task.Run(() =>
                {
                    using var cn = new SQLiteConnection(DBConnection.MyConnection());
                    cn.Open();

                    // Create a unique salt
                    string salt = Guid.NewGuid().ToString("N");
                    string hashedPassword = DBConnection.GetHash(password, salt);

                    using var cm = new SQLiteCommand(
                        "INSERT INTO tblUser (username, password, salt, role, name, isactive, isdeleted) " +
                        "VALUES (@username, @password, @salt, @role, @name, 1, 0)", cn);

                    cm.Parameters.AddWithValue("@username", username);
                    cm.Parameters.AddWithValue("@password", hashedPassword);
                    cm.Parameters.AddWithValue("@salt", salt);
                    cm.Parameters.AddWithValue("@role", role);
                    cm.Parameters.AddWithValue("@name", name);

                    cm.ExecuteNonQuery();
                    cn.Close();
                });

                MessageBox.Show("New account has been saved!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                ClearNewAccountFields();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                button1.Enabled = true;
            }
        }

        private void ClearNewAccountFields()
        {
            txtUser.Clear();
            txtPassword.Clear();
            txtRePassword.Clear();
            txtName.Clear();
            cbRole.Text = "";
            txtUser.Focus();
        }

        private void btnSave_Click(object sender, EventArgs e) => ClearNewAccountFields();

        // Elite async Change Password with salted hash
        private async void btnSav_Click(object sender, EventArgs e)
        {
            string oldPass = txtOld.Text;
            string newPass = txtNew.Text;
            string reNewPass = txtRePass.Text;
            string username = txtU.Text.Trim();

            if (string.IsNullOrEmpty(oldPass) || string.IsNullOrEmpty(newPass) || string.IsNullOrEmpty(reNewPass))
            {
                MessageBox.Show("Please fill all password fields", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (newPass != reNewPass)
            {
                MessageBox.Show("Confirm new password did not match!", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            btnSav.Enabled = false;

            try
            {
                await Task.Run(() =>
                {
                    using var cn = new SQLiteConnection(DBConnection.MyConnection());
                    cn.Open();

                    // Get stored hash and salt
                    string storedHash = null;
                    string storedSalt = null;

                    using (var cm = new SQLiteCommand("SELECT password, salt FROM tblUser WHERE username=@username", cn))
                    {
                        cm.Parameters.AddWithValue("@username", username);
                        using var dr = cm.ExecuteReader();
                        if (dr.Read())
                        {
                            storedHash = dr["password"].ToString();
                            storedSalt = dr["salt"].ToString();
                        }
                    }

                    if (storedHash == null)
                        throw new Exception("User does not exist.");

                    if (DBConnection.GetHash(oldPass, storedSalt) != storedHash)
                        throw new Exception("Old password did not match.");

                    // Hash new password with new salt
                    string newSalt = Guid.NewGuid().ToString("N");
                    string newHash = DBConnection.GetHash(newPass, newSalt);

                    using (var cmUpdate = new SQLiteCommand(
                               "UPDATE tblUser SET password=@password, salt=@salt WHERE username=@username", cn))
                    {
                        cmUpdate.Parameters.AddWithValue("@password", newHash);
                        cmUpdate.Parameters.AddWithValue("@salt", newSalt);
                        cmUpdate.Parameters.AddWithValue("@username", username);
                        cmUpdate.ExecuteNonQuery();
                    }

                    cn.Close();
                });

                MessageBox.Show("Password has been successfully changed!", "Done", MessageBoxButtons.OK, MessageBoxIcon.Information);
                txtOld.Clear();
                txtNew.Clear();
                txtRePass.Clear();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            finally
            {
                btnSav.Enabled = true;
            }
        }

        // Activate/Deactivate user status
        private async void txtuser2_TextChanged(object sender, EventArgs e)
        {
            string username = txtuser2.Text.Trim();
            if (string.IsNullOrEmpty(username))
            {
                checkBox1.Checked = false;
                return;
            }

            try
            {
                await Task.Run(() =>
                {
                    using var cn = new SQLiteConnection(DBConnection.MyConnection());
                    cn.Open();
                    using var cm = new SQLiteCommand("SELECT isactive FROM tblUser WHERE username = @username", cn);
                    cm.Parameters.AddWithValue("@username", username);

                    using var dr = cm.ExecuteReader();
                    if (dr.Read())
                        checkBox1.Invoke(() => checkBox1.Checked = Convert.ToInt32(dr["isactive"]) == 1);
                    else
                        checkBox1.Invoke(() => checkBox1.Checked = false);

                    cn.Close();
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private async void button2_Click(object sender, EventArgs e)
        {
            string username = txtuser2.Text.Trim();
            bool isActive = checkBox1.Checked;

            if (string.IsNullOrEmpty(username))
            {
                MessageBox.Show("Enter a username to update.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            button2.Enabled = false;

            try
            {
                int rowsAffected = 0;
                await Task.Run(() =>
                {
                    using var cn = new SQLiteConnection(DBConnection.MyConnection());
                    cn.Open();
                    using var cm = new SQLiteCommand("UPDATE tblUser SET isactive=@isactive WHERE username=@username", cn);
                    cm.Parameters.AddWithValue("@isactive", isActive ? 1 : 0);
                    cm.Parameters.AddWithValue("@username", username);
                    rowsAffected = cm.ExecuteNonQuery();
                    cn.Close();
                });

                if (rowsAffected > 0)
                {
                    MessageBox.Show("Account status has been successfully updated", "Account", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    txtuser2.Clear();
                    checkBox1.Checked = false;
                }
                else
                    MessageBox.Show("Account does not exist!", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            finally
            {
                button2.Enabled = true;
            }
        }

        // Unused designer events
        private void frmUserAccount_Load(object sender, EventArgs e) { }
        private void frmUserAccount_Resize(object sender, EventArgs e) { }
        private void panel2_Paint(object sender, PaintEventArgs e) { }
        private void tabPage2_Click_1(object sender, EventArgs e) { }
        private void txtOld_TextChanged(object sender, EventArgs e) { }
        private void label3_Click(object sender, EventArgs e) { }
        private void textBox2_TextChanged(object sender, EventArgs e) { }
    }

    // Helper for safe UI invocation
    public static class ControlExtensions
    {
        public static void Invoke(this Control control, Action action)
        {
            if (control.InvokeRequired)
                control.Invoke(action);
            else
                action();
        }
    }
}