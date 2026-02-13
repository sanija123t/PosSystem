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
            this.AcceptButton = btnSave;

            txtPass.UseSystemPasswordChar = true;
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
                MessageBox.Show("Please enter credentials.", "Login", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            ToggleUI(false);

            try
            {
                var result = await ValidateLoginAsync(username, password);

                if (result.isSuccess)
                {
                    RedirectUser(result.Username, result.Role);
                }
                else
                {
                    MessageBox.Show(result.Message, "Login failed", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    txtPass.Clear();
                    txtPass.Focus();
                    ToggleUI(true);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Database Error: " + ex.Message, "Critical Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                ToggleUI(true);
            }
        }

        private void ToggleUI(bool enabled)
        {
            btnSave.Enabled = enabled;
            this.UseWaitCursor = !enabled;
        }

        private async Task<(bool isSuccess, string Username, string Role, string Message)> ValidateLoginAsync(string username, string password)
        {
            try
            {
                using (var cn = new SQLiteConnection(DBConnection.MyConnection()))
                {
                    await cn.OpenAsync();
                    string sql = "SELECT username, role, password, salt, isactive FROM tblUser WHERE username=@u AND isdeleted=0 LIMIT 1";

                    using (var cm = new SQLiteCommand(sql, cn))
                    {
                        cm.Parameters.AddWithValue("@u", username.ToLower());
                        using (var dr = await cm.ExecuteReaderAsync())
                        {
                            if (await dr.ReadAsync())
                            {
                                if (Convert.ToInt32(dr["isactive"]) == 0)
                                    return (false, null, null, "Account deactivated.");

                                string storedHash = dr["password"].ToString();
                                string salt = dr["salt"].ToString();

                                string inputHash = await Task.Run(() => DBConnection.GetHash(password, salt));

                                if (inputHash == storedHash)
                                    return (true, dr["username"].ToString(), dr["role"].ToString(), "Success");
                            }
                        }
                    }
                }
            }
            catch { throw; }
            return (false, null, null, "Invalid username or password.");
        }

        private void RedirectUser(string username, string role)
        {
            Form nextForm = null;

            // We create Form1 for BOTH Admin and Cashier so that the session is properly initialized
            Form1 mainDash = new Form1();

            // Assuming you have a method to get the 'Full Name' from your database, 
            // otherwise we use username for both.
            mainDash.SetUserSession(username, role, username);

            if (role.Equals("Admin", StringComparison.OrdinalIgnoreCase) ||
                role.Equals("Administrator", StringComparison.OrdinalIgnoreCase))
            {
                nextForm = mainDash;
            }
            else if (role.Equals("Cashier", StringComparison.OrdinalIgnoreCase))
            {
                // ✅ FIX: Instead of passing 'this' (frmUserLogin), we pass 'mainDash' (Form1)
                frmPOS cashierDash = new frmPOS(mainDash);

                // Accessing controls safely
                Control[] userLabel = cashierDash.Controls.Find("LblUser", true);
                if (userLabel.Length > 0) userLabel[0].Text = username;

                Control[] nameLabel = cashierDash.Controls.Find("lblName", true);
                if (nameLabel.Length > 0) nameLabel[0].Text = username;

                nextForm = cashierDash;
            }

            if (nextForm != null)
            {
                this.Hide();
                nextForm.StartPosition = FormStartPosition.CenterScreen;
                // If the child form (POS) or main form closes, exit the app
                nextForm.FormClosed += (s, e) => Application.Exit();
                nextForm.Show();
            }
            else
            {
                MessageBox.Show($"Role '{role}' not recognized. Access denied.", "Security Error", MessageBoxButtons.OK, MessageBoxIcon.Stop);
                ToggleUI(true);
            }
        }
    }
}