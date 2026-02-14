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
        private bool _isSaving = false; // async-safe flag

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
            if (_isSaving) return; // prevent re-entry
            _isSaving = true;

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

                bool saveSuccess = false;

                // 🔹 Ask admin approval via frmVoid
                using (frmVoid voidForm = new frmVoid())
                {
                    voidForm.CancelAction = cbAction.Text.Trim();
                    voidForm.CancelQty = cancelQty;
                    voidForm.CancelReason = txtReason.Text.Trim();
                    voidForm.ProductCode = txtPcode.Text;
                    voidForm.CartId = txtID.Text;
                    voidForm.Price = unitPrice;
                    voidForm.CancelledBy = txtVoidBy.Text;
                    voidForm.SoldItemForm = f;

                    voidForm.ShowDialog();

                    saveSuccess = voidForm.SaveSuccess;

                    if (!saveSuccess)
                    {
                        MessageBox.Show("Cancellation was not completed or approved by admin.", "Access Denied", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }
                }

                // Async-safe refresh list
                if (f != null)
                    await f.LoadSoldItemsAsync();

                // Export PDF after completion
                await Task.Run(() => ExportCancelDetailsToPDF());

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
                _isSaving = false; // reset async flag
            }
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
            using (SQLiteConnection con = new SQLiteConnection(DBConnection.MyConnection()))
            {
                con.Open();
                using (SQLiteCommand cmd = new SQLiteCommand("SELECT * FROM vwSoldItems WHERE id=@id LIMIT 1", con))
                {
                    cmd.Parameters.AddWithValue("@id", id);
                    using (SQLiteDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            txtID.Text = reader["id"].ToString();
                            txtPcode.Text = reader["pcode"].ToString();
                            txtDesc.Text = reader["pdesc"].ToString();
                            txtQty.Text = reader["qty"].ToString();
                            txtPrice.Text = reader["price"].ToString();

                            if (!decimal.TryParse(reader["price"].ToString(), out unitPrice))
                                unitPrice = 0;

                            PerformCalculation();
                        }
                        else
                        {
                            MessageBox.Show("No sold record found.", "Search", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            txtPcode.Clear();
                            txtDesc.Clear();
                            txtQty.Clear();
                            txtPrice.Clear();
                            txtTotal.Text = "0.00";
                        }
                    }
                }
            }
        }

        private void AutoFillByPcode(string pcode)
        {
            using (SQLiteConnection con = new SQLiteConnection(DBConnection.MyConnection()))
            {
                con.Open();
                using (SQLiteCommand cmd = new SQLiteCommand("SELECT * FROM vwSoldItems WHERE pcode=@pcode LIMIT 1", con))
                {
                    cmd.Parameters.AddWithValue("@pcode", pcode);
                    using (SQLiteDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            txtID.Text = reader["id"].ToString();
                            txtPcode.Text = reader["pcode"].ToString();
                            txtDesc.Text = reader["pdesc"].ToString();
                            txtQty.Text = reader["qty"].ToString();
                            txtPrice.Text = reader["price"].ToString();

                            if (!decimal.TryParse(reader["price"].ToString(), out unitPrice))
                                unitPrice = 0;

                            PerformCalculation();
                        }
                        else
                        {
                            MessageBox.Show("No sold record found.", "Search", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            txtID.Clear();
                            txtDesc.Clear();
                            txtQty.Clear();
                            txtPrice.Clear();
                            txtTotal.Text = "0.00";
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
                string folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "POS_Cancellations");
                if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);

                string path = Path.Combine(folder, $"Void_{txtID.Text}_{DateTime.Now:yyyyMMdd_HHmmss}.pdf");

                Document doc = new Document(PageSize.A4, 25, 25, 30, 30);
                PdfWriter.GetInstance(doc, new FileStream(path, FileMode.Create));
                doc.Open();

                var titleFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 18);
                var standardFont = FontFactory.GetFont(FontFactory.HELVETICA, 12);

                doc.Add(new Paragraph("VOIDED TRANSACTION REPORT", titleFont));
                doc.Add(new Paragraph($"Date generated: {DateTime.Now:f}", standardFont));
                doc.Add(new Paragraph("---------------------------------------------------------"));
                doc.Add(new Paragraph($"Cart Item ID (tblCart1): {txtID.Text}", standardFont));
                doc.Add(new Paragraph($"Product: [{txtPcode.Text}] {txtDesc.Text}", standardFont));
                doc.Add(new Paragraph($"Original Sold Qty: {txtQty.Text}", standardFont));
                doc.Add(new Paragraph($"Voided Qty: {txtCancelQty.Text}", standardFont));
                doc.Add(new Paragraph($"Unit Price: {txtPrice.Text}", standardFont));
                doc.Add(new Paragraph($"Refund Amount: {txtTotal.Text}", standardFont));
                doc.Add(new Paragraph($"Action (Restored Stock?): {cbAction.Text}", standardFont));
                doc.Add(new Paragraph($"Reason: {txtReason.Text}", standardFont));
                doc.Add(new Paragraph($"Authorized By: {txtVoidBy.Text}", standardFont));

                doc.Close();
                MessageBox.Show($"Void Record Exported:\n{path}", "PDF Export", MessageBoxButtons.OK, MessageBoxIcon.Information);
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