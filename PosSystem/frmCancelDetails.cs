using System;
using System.Runtime.InteropServices; // Added for draggable logic
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Data.SQLite;
using iTextSharp.text;       // PDF export
using iTextSharp.text.pdf;
using System.IO;

namespace PosSystem
{
    public partial class frmCancelDetails : Form
    {
        #region WINAPI FOR DRAGGING
        [DllImport("user32.dll")]
        private static extern bool ReleaseCapture();
        [DllImport("user32.dll")]
        private static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);

        private const int WM_NCLBUTTONDOWN = 0xA1;
        private const int HT_CAPTION = 0x2;

        private void panel1_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                ReleaseCapture();
                SendMessage(Handle, WM_NCLBUTTONDOWN, HT_CAPTION, 0);
            }
        }
        #endregion

        private readonly frmSoldItems f;
        private decimal unitPrice = 0; // original sold price

        public frmCancelDetails(frmSoldItems frm, decimal soldPrice = 0)
        {
            InitializeComponent();
            f = frm;
            unitPrice = soldPrice;
            KeyPreview = true;

            txtCancelQty.TextChanged += TxtCancelQty_TextChanged;

            // Auto-fill sold item info on Enter
            txtID.KeyDown += TxtID_KeyDown;
            txtPcode.KeyDown += TxtPcode_KeyDown;
        }

        private void frmCancelDetails_Load(object sender, EventArgs e)
        {
            cbAction.Focus();
        }

        private void pictureBox1_Click(object sender, EventArgs e) => Dispose();

        private void cbAction_KeyPress(object sender, KeyPressEventArgs e) => e.Handled = true;

        private void TxtCancelQty_TextChanged(object sender, EventArgs e) => PerformCalculation();

        private void PerformCalculation()
        {
            try
            {
                if (decimal.TryParse(txtCancelQty.Text, out decimal qty))
                    txtTotal.Text = (unitPrice * qty).ToString("N2");
                else
                    txtTotal.Text = "0.00";
            }
            catch
            {
                txtTotal.Text = "0.00";
            }
        }

        private async void btnSave_Click(object sender, EventArgs e)
        {
            btnSave.Enabled = false;
            this.Cursor = Cursors.WaitCursor;

            try
            {
                // Manual fields validation
                if (string.IsNullOrWhiteSpace(cbAction.Text) ||
                    string.IsNullOrWhiteSpace(txtCancelQty.Text) ||
                    string.IsNullOrWhiteSpace(txtReason.Text) ||
                    string.IsNullOrWhiteSpace(txtVoidBy.Text))
                {
                    MessageBox.Show("Please fill all required fields manually (Action, Cancel Qty, Reason, Voided By).",
                        "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                if (!int.TryParse(txtQty.Text, out int currentQty) ||
                    !int.TryParse(txtCancelQty.Text, out int cancelQty))
                {
                    MessageBox.Show("Quantities must be whole numeric values.", "Input Error",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                if (cancelQty <= 0 || cancelQty > currentQty)
                {
                    MessageBox.Show("Cancel quantity must be >0 and <= sold quantity.", "Logic Error",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                // Confirm cancel
                if (MessageBox.Show("This action will void the selected item(s). Proceed?", "Confirm Cancel",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                    return;

                // 🔹 Ask admin approval via frmVoid
                using (frmVoid voidForm = new frmVoid())
                {
                    voidForm.CancelAction = cbAction.Text.Trim();
                    voidForm.CancelQty = cancelQty;
                    voidForm.CancelReason = txtReason.Text.Trim();
                    voidForm.SoldItemForm = f;

                    voidForm.ShowDialog();

                    // If approval not granted, stop
                    if (!voidForm.Approved)
                    {
                        MessageBox.Show("Cancellation not approved by admin.", "Access Denied", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }
                }

                await RefreshList();

                // Export PDF only if cancel approved
                ExportCancelDetailsToPDF();

                Dispose();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Critical Error: " + ex.Message, "Fatal Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                this.Cursor = Cursors.Default;
                btnSave.Enabled = true;
            }
        }

        public async Task RefreshList()
        {
            if (f != null)
                await f.LoadSoldItemsAsync();
        }

        private void frmCancelDetails_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape) Dispose();
            else if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true;
                btnSave_Click(sender, e);
            }
        }

        #region Auto-Fill Sold Info (Manual Cancel Fields Only)

        private void TxtID_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true;
                AutoFillByID(txtID.Text.Trim());
            }
        }

        private void TxtPcode_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true;
                AutoFillByPcode(txtPcode.Text.Trim());
            }
        }

        private void AutoFillByID(string id)
        {
            using (SQLiteConnection con = new SQLiteConnection(f.ConnectionString))
            {
                con.Open();
                using (SQLiteCommand cmd = new SQLiteCommand("SELECT * FROM SoldItems WHERE ID=@id", con))
                {
                    cmd.Parameters.AddWithValue("@id", id);
                    using (SQLiteDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            txtPcode.Text = reader["Pcode"].ToString();
                            txtDesc.Text = reader["Description"].ToString();
                            txtQty.Text = reader["Qty"].ToString();
                            txtPrice.Text = reader["Price"].ToString();
                            unitPrice = Convert.ToDecimal(reader["Price"]);
                        }
                    }
                }
            }
        }

        private void AutoFillByPcode(string pcode)
        {
            using (SQLiteConnection con = new SQLiteConnection(f.ConnectionString))
            {
                con.Open();
                using (SQLiteCommand cmd = new SQLiteCommand("SELECT * FROM SoldItems WHERE Pcode=@pcode", con))
                {
                    cmd.Parameters.AddWithValue("@pcode", pcode);
                    using (SQLiteDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            txtID.Text = reader["ID"].ToString();
                            txtDesc.Text = reader["Description"].ToString();
                            txtQty.Text = reader["Qty"].ToString();
                            txtPrice.Text = reader["Price"].ToString();
                            unitPrice = Convert.ToDecimal(reader["Price"]);
                        }
                    }
                }
            }
        }

        #endregion

        #region Export to PDF

        private void ExportCancelDetailsToPDF()
        {
            try
            {
                string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), $"Cancel_{txtID.Text}_{DateTime.Now:yyyyMMddHHmmss}.pdf");
                Document doc = new Document(PageSize.A4);
                PdfWriter.GetInstance(doc, new FileStream(path, FileMode.Create));
                doc.Open();

                doc.Add(new Paragraph("Cancel Details Report"));
                doc.Add(new Paragraph($"ID: {txtID.Text}"));
                doc.Add(new Paragraph($"Pcode: {txtPcode.Text}"));
                doc.Add(new Paragraph($"Description: {txtDesc.Text}"));
                doc.Add(new Paragraph($"Qty Sold: {txtQty.Text}"));
                doc.Add(new Paragraph($"Qty Cancel: {txtCancelQty.Text}"));
                doc.Add(new Paragraph($"Unit Price: {txtPrice.Text}"));
                doc.Add(new Paragraph($"Total Refund: {txtTotal.Text}"));
                doc.Add(new Paragraph($"Action: {cbAction.Text}"));
                doc.Add(new Paragraph($"Reason: {txtReason.Text}"));
                doc.Add(new Paragraph($"Voided By: {txtVoidBy.Text}"));
                doc.Close();

                MessageBox.Show($"PDF Exported Successfully to Desktop:\n{path}", "PDF Export", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("PDF Export Failed: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        #endregion

        #region Junk Designer Stubs

        private void txtID_TextChanged(object sender, EventArgs e) { }
        private void txtPcode_TextChanged(object sender, EventArgs e) { }
        private void txtDesc_TextChanged(object sender, EventArgs e) { }
        private void txtTransno_TextChanged(object sender, EventArgs e) { }
        private void txtPrice_TextChanged(object sender, EventArgs e) { }
        private void txtQty_TextChanged(object sender, EventArgs e) { }
        private void txtDiscount_TextChanged(object sender, EventArgs e) { }
        private void txtTotal_TextChanged(object sender, EventArgs e) { }
        private void txtVoidBy_TextChanged(object sender, EventArgs e) { }
        private void txtCancelled_TextChanged(object sender, EventArgs e) { }
        private void cbAction_SelectedIndexChanged(object sender, EventArgs e) { }
        private void txtReason_TextChanged(object sender, EventArgs e) { }
        private void txtCancelQty_TextChanged_1(object sender, EventArgs e) { PerformCalculation(); }

        #endregion
    }
}