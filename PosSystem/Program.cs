using System;
using System.Windows.Forms;

namespace PosSystem
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            try
            {
                // Ensure the DB is ready before anything else
                DBConnection.InitializeDatabase();

                Form1 dashboard = new Form1();

                // Make sure these labels exist in Form1 and are set to PUBLIC
                dashboard.lblUser.Text = "DEVELOPER_MODE";
                dashboard.lblRole.Text = "System Owner";

                Application.Run(dashboard);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Startup Error: " + ex.Message);
            }
        }
    }
}