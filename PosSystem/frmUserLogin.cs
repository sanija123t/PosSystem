using System;
using System.Data.SQLite;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PosSystem
{
    public partial class frmUserLogin : Form
    {
        public frmUserLogin()
        {
            InitializeComponent();
            this.AcceptButton = btnSave; // Enter triggers login

            // Ensure password masking
            txtPass.UseSystemPasswordChar = true;

            // MetroTextBox placeholder equivalent
            txtUser.WaterMark = "Enter your username";
            txtPass.WaterMark = "Enter your password";
        }

        private void frmUserLogin_Load(object sender, EventArgs e)
        {
            txtUser.Focus();
        }

        private void btnCancle_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private async void btnSave_Click(object sender, EventArgs e)
        {
            string username = txtUser.Text.Trim();
            string password = txtPass.Text;

            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                MessageBox.Show("Enter both username and password", "Login",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            btnSave.Enabled = false;
            Cursor = Cursors.WaitCursor;

            try
            {
                var loginResult = await Task.Run(() => ValidateLogin(username, password));

                if (loginResult.isSuccess)
                {
                    OpenDashboard(loginResult.Username, loginResult.Role);
                }
                else
                {
                    MessageBox.Show("Invalid login credentials", "Login",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    txtPass.Clear();
                    txtPass.Focus();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Login error: " + ex.Message, "Login",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                btnSave.Enabled = true;
                Cursor = Cursors.Default;
            }
        }

        private (bool isSuccess, string Username, string Role) ValidateLogin(string username, string password)
        {
            using (var cn = new SQLiteConnection(DBConnection.MyConnection()))
            {
                cn.Open();

                string sql = @"SELECT username, role, password, salt 
                               FROM tblUser 
                               WHERE username = @u 
                               AND isactive = 1 
                               AND isdeleted = 0";

                using (var cm = new SQLiteCommand(sql, cn))
                {
                    cm.Parameters.AddWithValue("@u", username);

                    using (var dr = cm.ExecuteReader())
                    {
                        if (dr.Read())
                        {
                            string storedHash = dr["password"].ToString();
                            string salt = dr["salt"].ToString();

                            string inputHash = DBConnection.GetHash(password, salt);

                            if (inputHash == storedHash)
                                return (true, dr["username"].ToString(), dr["role"].ToString());
                        }
                    }
                }
            }

            return (false, null, null);
        }

        private void OpenDashboard(string username, string role)
        {
            try
            {
                this.Hide();

                Form1 dashboard = new Form1
                {
                    StartPosition = FormStartPosition.CenterScreen
                };

                dashboard.lblUserName.Text = username;
                dashboard.lblRole.Text = role;

                dashboard.FormClosed += (s, e) => Application.Exit();
                dashboard.Show();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error opening dashboard: " + ex.Message,
                    "Login", MessageBoxButtons.OK, MessageBoxIcon.Error);
                this.Show();
            }
        }
    }
}