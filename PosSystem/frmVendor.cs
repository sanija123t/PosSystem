using System;
using System.Data;
using System.Data.SQLite;
using System.Runtime.InteropServices; // Required for Draggable Logic
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PosSystem
{
    public partial class frmVendor : Form
    {
        // --- DRAGGABLE LOGIC (WinAPI) ---
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
        // -------------------------------

        private readonly frm_VendorList f;

        public frmVendor(frm_VendorList f)
        {
            InitializeComponent();
            this.f = f;
        }

        // SAVE (ASYNC + SAFE)
        private async void button1_Click(object sender, EventArgs e)
        {
            // ✅ UX VALIDATION (added only)
            if (string.IsNullOrWhiteSpace(txtVendor.Text))
            {
                MessageBox.Show("Vendor Name is required!",
                    "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (!string.IsNullOrWhiteSpace(txtEmail.Text) && !txtEmail.Text.Contains("@"))
            {
                MessageBox.Show("Please enter a valid email address!",
                    "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            // ✅ END UX VALIDATION

            if (!ConfirmAction("Save this record?")) return;

            ToggleButtons(false);

            try
            {
                using (SQLiteConnection cn = new SQLiteConnection(DBConnection.MyConnection()))
                {
                    await cn.OpenAsync();

                    using (SQLiteCommand cm = new SQLiteCommand(
                        @"INSERT INTO tblVendor
                          (vendor, address, contactperson, telephone, email, fax)
                          VALUES
                          (@vendor, @address, @contactperson, @telephone, @email, @fax)", cn))
                    {
                        BindParameters(cm);
                        await cm.ExecuteNonQueryAsync();
                    }
                }

                MessageBox.Show("Record has been successfully saved.",
                    "POS System", MessageBoxButtons.OK, MessageBoxIcon.Information);

                Clear();
                f.LoadRecords(); // backward-compatible
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Warning",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            finally
            {
                ToggleButtons(true);
            }
        }

        // UPDATE (ASYNC + SAFE)
        private async void btnUpdate_Click(object sender, EventArgs e)
        {
            if (!ConfirmAction("Update this record?")) return;

            ToggleButtons(false);

            try
            {
                using (SQLiteConnection cn = new SQLiteConnection(DBConnection.MyConnection()))
                {
                    await cn.OpenAsync();

                    using (SQLiteCommand cm = new SQLiteCommand(
                        @"UPDATE tblVendor SET
                          vendor = @vendor,
                          address = @address,
                          contactperson = @contactperson,
                          telephone = @telephone,
                          email = @email,
                          fax = @fax
                          WHERE id = @id", cn))
                    {
                        BindParameters(cm);
                        cm.Parameters.AddWithValue("@id", lblD.Text);

                        await cm.ExecuteNonQueryAsync();
                    }
                }

                MessageBox.Show("Record has been successfully updated.",
                    "POS System", MessageBoxButtons.OK, MessageBoxIcon.Information);

                Clear();
                f.LoadRecords();
                this.Dispose();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Warning",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            finally
            {
                ToggleButtons(true);
            }
        }

        // DESIGNER SAFE
        private void frmVendor_Load(object sender, EventArgs e)
        {
        }

        // 🔐 Shared helpers (elite pattern, no redesign)

        private void BindParameters(SQLiteCommand cm)
        {
            cm.Parameters.AddWithValue("@vendor", txtVendor.Text.Trim());
            cm.Parameters.AddWithValue("@address", txtAddress.Text.Trim());
            cm.Parameters.AddWithValue("@contactperson", txtContactPreson.Text.Trim());
            cm.Parameters.AddWithValue("@telephone", txtTelephone.Text.Trim());
            cm.Parameters.AddWithValue("@email", txtEmail.Text.Trim());
            cm.Parameters.AddWithValue("@fax", txtFax.Text.Trim());
        }

        private bool ConfirmAction(string message)
        {
            return MessageBox.Show(message, "Confirm",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes;
        }

        private void ToggleButtons(bool enabled)
        {
            btnSave.Enabled = enabled;
            btnUpdate.Enabled = enabled;
        }

        public void Clear()
        {
            txtVendor.Clear();
            txtAddress.Clear();
            txtContactPreson.Clear();
            txtTelephone.Clear();
            txtEmail.Clear();
            txtFax.Clear();

            txtVendor.Focus();
            btnSave.Enabled = true;
            btnUpdate.Enabled = false;
        }

        private void pictureBox2_Click(object sender, EventArgs e)
        {
            this.Dispose();
        }
    }
}