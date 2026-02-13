using System;
using System.Data;
using System.Data.SQLite;
using System.Runtime.InteropServices; // Required for dragging
using System.Windows.Forms;

namespace PosSystem
{
    public partial class frmStoreSetting : Form
    {
        // --- DRAGGABLE LOGIC ---
        [DllImport("user32.dll")]
        public static extern bool ReleaseCapture();

        [DllImport("user32.dll")]
        public static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);

        public const int WM_NCLBUTTONDOWN = 0xA1;
        public const int HT_CAPTION = 0x2;

        private void panelTitle_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                ReleaseCapture();
                SendMessage(Handle, WM_NCLBUTTONDOWN, HT_CAPTION, 0);
            }
        }
        // -----------------------

        private string stitle = "PosSystem";

        public frmStoreSetting()
        {
            InitializeComponent();
        }

        private void pictureBox2_Click(object sender, EventArgs e)
        {
            this.Dispose();
        }

        // Run this once or manually in your DB Manager to fix the "no such table" error
        // CREATE TABLE "tblStore" ("store" TEXT, "address" TEXT, "phone" TEXT);

        public void LoadRecord()
        {
            try
            {
                using (var cn = new SQLiteConnection(DBConnection.MyConnection()))
                {
                    cn.Open();
                    using (var cmd = new SQLiteCommand("SELECT * FROM tblStore", cn))
                    using (var dr = cmd.ExecuteReader())
                    {
                        if (dr.Read())
                        {
                            txtStore.Text = dr["store"].ToString();
                            txtAddress.Text = dr["address"].ToString();
                            txtPhone.Text = dr["phone"].ToString();
                        }
                        else
                        {
                            txtStore.Clear();
                            txtAddress.Clear();
                            txtPhone.Clear();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, stitle, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            try
            {
                if (MessageBox.Show("Save store details?", stitle, MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                    return;

                using (var cn = new SQLiteConnection(DBConnection.MyConnection()))
                {
                    cn.Open();

                    // Check if store record already exists
                    int count = 0;
                    using (var cmd = new SQLiteCommand("SELECT COUNT(*) FROM tblStore", cn))
                    {
                        count = Convert.ToInt32(cmd.ExecuteScalar());
                    }

                    // Insert or update
                    string sql = count > 0
                        ? "UPDATE tblStore SET store=@store, address=@address, phone=@phone"
                        : "INSERT INTO tblStore (store, address, phone) VALUES (@store, @address, @phone)";

                    using (var cmd = new SQLiteCommand(sql, cn))
                    {
                        cmd.Parameters.AddWithValue("@store", txtStore.Text.Trim());
                        cmd.Parameters.AddWithValue("@address", txtAddress.Text.Trim());
                        cmd.Parameters.AddWithValue("@phone", txtPhone.Text.Trim());
                        cmd.ExecuteNonQuery();
                    }
                }

                MessageBox.Show("Store details have been successfully saved!", stitle, MessageBoxButtons.OK, MessageBoxIcon.Information);

                // Refresh UI to reflect saved data
                LoadRecord();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, stitle, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void frmStoreSetting_Load(object sender, EventArgs e)
        {
            LoadRecord();
        }
    }
}