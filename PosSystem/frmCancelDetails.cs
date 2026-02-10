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

            // Optional: handle dynamic refund calculation
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

        // =========================
        // 🔹 DYNAMIC REFUND CALCULATION
        // =========================
        private void TxtCancelQty_TextChanged(object sender, EventArgs e)
        {
            if (int.TryParse(txtCancelQty.Text, out int qty))
                txtTotal.Text = (unitPrice * qty).ToString("0.00"); // optional display
            else
                txtTotal.Text = "0.00";
        }

        // =========================
        // 🔹 SAVE CANCEL ACTION
        // =========================
        private async void btnSave_Click(object sender, EventArgs e)
        {
            btnSave.Enabled = false;

            try
            {
                // Validate required fields
                if (string.IsNullOrWhiteSpace(cbAction.Text) ||
                    string.IsNullOrWhiteSpace(txtCancelQty.Text) ||
                    string.IsNullOrWhiteSpace(txtReason.Text))
                {
                    MessageBox.Show("Please fill in all required fields.", "Validation Error",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                // Parse quantities
                if (!int.TryParse(txtQty.Text, out int currentQty) ||
                    !int.TryParse(txtCancelQty.Text, out int cancelQty))
                {
                    MessageBox.Show("Please enter valid numeric quantities.", "Input Error",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                if (cancelQty > currentQty)
                {
                    MessageBox.Show("Cancel quantity cannot be greater than the sold quantity.", "Invalid Quantity",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                if (MessageBox.Show("Are you sure you want to cancel this item?", "Confirm Cancel",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                    return;

                // ===== FIXED: create frmVoid instance correctly =====
                frmVoid voidForm = new frmVoid(); // Use parameterless constructor

                // Set the necessary properties for void action
                voidForm.CancelAction = cbAction.Text.Trim();
                voidForm.CancelQty = cancelQty;
                voidForm.CancelReason = txtReason.Text.Trim();
                voidForm.SoldItemForm = f; // optional: link to parent to refresh or update

                voidForm.ShowDialog();

                // Refresh sold items list
                await RefreshList();

                Dispose(); // Close form after successful cancel
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
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
                btnSave.PerformClick();
        }
    }
}