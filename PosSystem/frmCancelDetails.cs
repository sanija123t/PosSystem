using System;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PosSystem
{
    public partial class frmCancelDetails : Form
    {
        private readonly frmSoldItems f;
        private decimal unitPrice = 0; // optional: unit price for refund calculation

        public frmCancelDetails(frmSoldItems frm, decimal soldPrice = 0)
        {
            InitializeComponent();
            f = frm;
            unitPrice = soldPrice;
            KeyPreview = true;

            // ELITE: Explicitly re-binding to ensure the 'not working' bars are forced to trigger logic
            txtCancelQty.TextChanged += TxtCancelQty_TextChanged;
        }

        private void frmCancelDetails_Load(object sender, EventArgs e)
        {
            cbAction.Focus();
        }

        private void pictureBox1_Click(object sender, EventArgs e)
        {
            Dispose();
        }

        // Prevent typing in the ComboBox
        private void cbAction_KeyPress(object sender, KeyPressEventArgs e)
        {
            e.Handled = true;
        }

        // ============================================================
        // 🔹 DYNAMIC REFUND CALCULATION (Enterprise Logic)
        // ============================================================
        private void TxtCancelQty_TextChanged(object sender, EventArgs e)
        {
            PerformCalculation();
        }

        private void PerformCalculation()
        {
            try
            {
                // ENTERPRISE: Use decimal for financial accuracy
                if (decimal.TryParse(txtCancelQty.Text, out decimal qty))
                {
                    txtTotal.Text = (unitPrice * qty).ToString("N2");
                }
                else
                {
                    txtTotal.Text = "0.00";
                }
            }
            catch
            {
                txtTotal.Text = "0.00";
            }
        }

        // ============================================================
        // 🔹 SAVE CANCEL ACTION (Elite Implementation)
        // ============================================================
        private async void btnSave_Click(object sender, EventArgs e)
        {
            // Lock UI to prevent race conditions
            btnSave.Enabled = false;
            this.Cursor = Cursors.WaitCursor;

            try
            {
                // 1. ELITE VALIDATION: Ensure all enterprise fields are satisfied
                if (string.IsNullOrWhiteSpace(cbAction.Text) ||
                    string.IsNullOrWhiteSpace(txtCancelQty.Text) ||
                    string.IsNullOrWhiteSpace(txtReason.Text) ||
                    string.IsNullOrWhiteSpace(txtVoidBy.Text))
                {
                    MessageBox.Show("Security and Data Integrity Error: Please fill in all required fields, including Voided By.",
                        "Validation Failure", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                // 2. DATA PARSING: Prevent crashes from non-numeric input
                if (!int.TryParse(txtQty.Text, out int currentQty) ||
                    !int.TryParse(txtCancelQty.Text, out int cancelQty))
                {
                    MessageBox.Show("Format Error: Quantities must be whole numeric values.", "Input Error",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                // 3. LOGIC CHECK
                if (cancelQty <= 0)
                {
                    MessageBox.Show("Quantity must be greater than zero.", "Invalid Input", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                if (cancelQty > currentQty)
                {
                    MessageBox.Show("Inventory Conflict: Cancel quantity exceeds original sold quantity.", "Logic Error",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                // 4. CONFIRMATION
                if (MessageBox.Show("Confirming this action will void the selected item(s). Proceed?", "System Confirmation",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                    return;

                // 5. PROCESSING: Pass data to frmVoid
                using (frmVoid voidForm = new frmVoid())
                {
                    voidForm.CancelAction = cbAction.Text.Trim();
                    voidForm.CancelQty = cancelQty;
                    voidForm.CancelReason = txtReason.Text.Trim();
                    voidForm.SoldItemForm = f;

                    voidForm.ShowDialog();
                }

                // 6. REFRESH & DISPOSE
                await RefreshList();
                this.Dispose();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Enterprise Critical Error: " + ex.Message, "Fatal Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                this.Cursor = Cursors.Default;
                btnSave.Enabled = true;
            }
        }

        // =========================
        // 🔹 ASYNC REFRESH SOLD ITEMS
        // =========================
        public async Task RefreshList()
        {
            if (f != null)
            {
                await f.LoadSoldItemsAsync();
            }
        }

        // =========================
        // 🔹 KEYBOARD SHORTCUTS
        // =========================
        private void frmCancelDetails_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape)
                Dispose();
            else if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true; // Prevents the 'ding' sound
                btnSave_Click(sender, e);
            }
        }

        // --------------------------------------------------------------------------------------
        // 🔹 JUNK LINES / DESIGNER DEPENDENCIES 
        // DO NOT REMOVE: Removing these will break the WinForms Designer (InitializeComponent)
        // --------------------------------------------------------------------------------------
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

        private void txtCancelQty_TextChanged_1(object sender, EventArgs e)
        {
            // ELITE: Redirecting designer duplicate to the main calculation logic
            PerformCalculation();
        }
    }
}