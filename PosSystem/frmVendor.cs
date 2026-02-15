using System;
using System.Data;
using System.Data.SQLite;
using System.Drawing; // Added for UI coloring
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PosSystem
{
    public partial class frmVendor : Form
    {
        #region DRAGGABLE LOGIC
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
        #endregion

        private readonly frm_VendorList f;

        public frmVendor(frm_VendorList f)
        {
            InitializeComponent();
            this.f = f;
            SetupUX();
        }

        private void SetupUX()
        {
            // Attach Focus events for visual feedback
            foreach (Control ctrl in this.Controls)
            {
                if (ctrl is TextBox)
                {
                    ctrl.Enter += (s, e) => ctrl.BackColor = Color.LightYellow;
                    ctrl.Leave += (s, e) => ctrl.BackColor = Color.White;
                    // Move to next control on Enter key
                    ctrl.KeyDown += (s, e) => {
                        if (e.KeyCode == Keys.Enter)
                        {
                            this.SelectNextControl((Control)s, true, true, true, true);
                            e.SuppressKeyPress = true;
                        }
                    };
                }
            }
        }

        private async void btnSave_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtVendor.Text) || string.IsNullOrWhiteSpace(txtVendorID.Text))
            {
                MessageBox.Show("Vendor ID and Name are required fields.", "UX Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                using (SQLiteConnection cn = new SQLiteConnection(DBConnection.MyConnection()))
                {
                    await cn.OpenAsync();
                    using (SQLiteCommand checkCmd = new SQLiteCommand("SELECT COUNT(*) FROM tblVendor WHERE id = @id", cn))
                    {
                        checkCmd.Parameters.AddWithValue("@id", txtVendorID.Text.Trim());
                        if (Convert.ToInt32(await checkCmd.ExecuteScalarAsync()) > 0)
                        {
                            MessageBox.Show("This Vendor ID is already registered.", "Duplicate Found", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            return;
                        }
                    }

                    if (!ConfirmAction("Are you sure you want to save this new vendor?")) return;

                    using (SQLiteCommand cm = new SQLiteCommand(
                        @"INSERT INTO tblVendor (id, vendor, address, contactperson, telephone, email, fax)
                          VALUES (@id, @vendor, @address, @contactperson, @telephone, @email, @fax)", cn))
                    {
                        cm.Parameters.AddWithValue("@id", txtVendorID.Text.Trim());
                        BindParameters(cm);
                        await cm.ExecuteNonQueryAsync();
                    }
                }

                MessageBox.Show("Vendor successfully saved!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                Clear();
                f.LoadRecords();
            }
            catch (Exception ex) { MessageBox.Show(ex.Message, "System Error", MessageBoxButtons.OK, MessageBoxIcon.Error); }
        }

        private async void btnUpdate_Click(object sender, EventArgs e)
        {
            if (!ConfirmAction("Update existing vendor information?")) return;

            try
            {
                using (SQLiteConnection cn = new SQLiteConnection(DBConnection.MyConnection()))
                {
                    await cn.OpenAsync();
                    using (SQLiteCommand cm = new SQLiteCommand(
                        @"UPDATE tblVendor SET vendor=@vendor, address=@address, contactperson=@contactperson, 
                          telephone=@telephone, email=@email, fax=@fax WHERE id=@id", cn))
                    {
                        cm.Parameters.AddWithValue("@id", txtVendorID.Text.Trim());
                        BindParameters(cm);
                        await cm.ExecuteNonQueryAsync();
                    }
                }
                MessageBox.Show("Vendor details updated successfully.", "Update", MessageBoxButtons.OK, MessageBoxIcon.Information);
                Clear();
                f.LoadRecords();
                this.Dispose();
            }
            catch (Exception ex) { MessageBox.Show(ex.Message, "System Error", MessageBoxButtons.OK, MessageBoxIcon.Error); }
        }

        private async void txtVendorID_TextChanged(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtVendorID.Text))
            {
                btnUpdate.Enabled = false;
                btnSave.Enabled = true;
                return;
            }

            try
            {
                using (SQLiteConnection cn = new SQLiteConnection(DBConnection.MyConnection()))
                {
                    await cn.OpenAsync();
                    using (SQLiteCommand cm = new SQLiteCommand("SELECT * FROM tblVendor WHERE id = @id", cn))
                    {
                        cm.Parameters.AddWithValue("@id", txtVendorID.Text.Trim());
                        using (var dr = await cm.ExecuteReaderAsync())
                        {
                            if (await dr.ReadAsync())
                            {
                                txtVendor.Text = dr["vendor"].ToString();
                                txtAddress.Text = dr["address"].ToString();
                                txtContactPreson.Text = dr["contactperson"].ToString();
                                txtTelephone.Text = dr["telephone"].ToString();
                                txtEmail.Text = dr["email"].ToString();
                                txtFax.Text = dr["fax"].ToString();

                                btnSave.Enabled = false;
                                btnUpdate.Enabled = true;
                            }
                            else
                            {
                                btnSave.Enabled = true;
                                btnUpdate.Enabled = false;
                            }
                        }
                    }
                }
            }
            catch { /* Silent log for typing UX */ }
        }

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
            return MessageBox.Show(message, "Confirm Action", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes;
        }

        public void Clear()
        {
            foreach (Control c in this.Controls) if (c is TextBox) ((TextBox)c).Clear();
            btnSave.Enabled = true;
            btnUpdate.Enabled = false;
            txtVendorID.Focus();
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            if (ConfirmAction("Discard all unsaved changes and clear the form?")) Clear();
        }

        private void pictureBox2_Click(object sender, EventArgs e) => this.Dispose();
        // --- Junk Handlers to satisfy Designer.cs references ---
        private void frmVendor_Load(object sender, EventArgs e) { }
        private void txtVendor_TextChanged(object sender, EventArgs e) { }
        private void txtAddress_TextChanged(object sender, EventArgs e) { }
        private void txtContactPreson_TextChanged(object sender, EventArgs e) { }
        private void txtTelephone_TextChanged(object sender, EventArgs e) { }
        private void txtEmail_TextChanged(object sender, EventArgs e) { }
        private void txtFax_TextChanged(object sender, EventArgs e) { }
        private void button1_Click(object sender, EventArgs e) => btnSave_Click(sender, e);

        // Data input validation for numeric fields
        private void txtTelephone_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar) && (e.KeyChar != '+')) e.Handled = true;
        }
    }
}