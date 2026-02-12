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
                // We run the logic and get a result object back
                var (isSuccess, foundUsername, role, message) = await Task.Run(() => ValidateLogin(username, password));

                if (isSuccess)
                {
                    OpenDashboard(foundUsername, role);
                }
                else
                {
                    // If login failed, show the specific message returned by ValidateLogin
                    MessageBox.Show(message, "Login", MessageBoxButtons.OK, MessageBoxIcon.Warning);
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

        private (bool isSuccess, string Username, string Role, string Message) ValidateLogin(string username, string password)
        {
            using (var cn = new SQLiteConnection(DBConnection.MyConnection()))
            {
                cn.Open();

                // We select isactive as well to check it in C#
                string sql = @"SELECT username, role, password, salt, isactive 
                               FROM tblUser 
                               WHERE username = @u 
                               AND isdeleted = 0";

                using (var cm = new SQLiteCommand(sql, cn))
                {
                    cm.Parameters.AddWithValue("@u", username);

                    using (var dr = cm.ExecuteReader())
                    {
                        if (dr.Read())
                        {
                            // 1. Check if the account is active first
                            int activeStatus = Convert.ToInt32(dr["isactive"]);
                            if (activeStatus == 0)
                            {
                                return (false, null, null, "Your account has been deactivated. Please contact your administrator.");
                            }

                            // 2. If active, validate the password
                            string storedHash = dr["password"].ToString();
                            string salt = dr["salt"].ToString();
                            string inputHash = DBConnection.GetHash(password, salt);

                            if (inputHash == storedHash)
                            {
                                return (true, dr["username"].ToString(), dr["role"].ToString(), "Success");
                            }
                        }
                    }
                }
            }

            // Default fallback for wrong username or wrong password
            return (false, null, null, "Invalid login credentials");
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