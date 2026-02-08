using System;
using System.Data.SQLite;
using System.Windows.Forms;

namespace PosSystem
{
    public partial class frmUserLogin : Form
    {
        public frmUserLogin()
        {
            InitializeComponent();
        }

        private void frmUserLogin_Load(object sender, EventArgs e)
        {
            txtUser.Focus();
        }

        private void btnCancle_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtUser.Text) || string.IsNullOrWhiteSpace(txtPass.Text))
            {
                MessageBox.Show("Enter username and password");
                return;
            }

            try
            {
                using (SQLiteConnection cn = new SQLiteConnection(DBConnection.MyConnection()))
                {
                    cn.Open();

                    string sql = @"SELECT username, role 
                                   FROM tblUser 
                                   WHERE username = @u 
                                   AND password = @p 
                                   AND isactive = 1 
                                   AND isdeleted = 0";

                    using (SQLiteCommand cm = new SQLiteCommand(sql, cn))
                    {
                        cm.Parameters.AddWithValue("@u", txtUser.Text.Trim());
                        cm.Parameters.AddWithValue("@p", DBConnection.GetHash(txtPass.Text));

                        using (SQLiteDataReader dr = cm.ExecuteReader())
                        {
                            if (dr.Read())
                            {
                                OpenDashboard(
                                    dr["username"].ToString(),
                                    dr["role"].ToString()
                                );
                            }
                            else
                            {
                                MessageBox.Show("Invalid login");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Login error: " + ex.Message);
            }
        }

        private void OpenDashboard(string username, string role)
        {
            this.Hide();

            Form1 dashboard = new Form1
            {
                StartPosition = FormStartPosition.CenterScreen
            };

            dashboard.lblUser.Text = username;
            dashboard.lblRole.Text = role;

            dashboard.FormClosed += (s, e) => this.Close();

            dashboard.Show(); // NOT ShowDialog
        }
    }
}