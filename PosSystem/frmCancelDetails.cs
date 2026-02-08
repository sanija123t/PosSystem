using System;
using System.Windows.Forms;

namespace PosSystem
{
    public partial class frmCancelDetails : Form
    {
        frmSoldItems f;
        public frmCancelDetails(frmSoldItems frm)
        {
            InitializeComponent();
            f = frm;
        }

        private void frmCancelDetails_Load(object sender, EventArgs e)
        {
            // Initial focus or setup if needed
        }

        private void pictureBox1_Click(object sender, EventArgs e)
        {
            this.Dispose();
        }

        private void cbAction_KeyPress(object sender, KeyPressEventArgs e)
        {
            e.Handled = true; // Prevents typing in the ComboBox
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            try
            {
                // Validate fields are not empty
                if (string.IsNullOrWhiteSpace(cbAction.Text) || string.IsNullOrWhiteSpace(txtCancelQty.Text) || string.IsNullOrWhiteSpace(txtReason.Text))
                {
                    MessageBox.Show("Please fill in all required fields.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                // Parse quantities safely
                if (int.TryParse(txtQty.Text, out int currentQty) && int.TryParse(txtCancelQty.Text, out int cancelQty))
                {
                    // Logic check: Cannot cancel more than what was sold
                    if (cancelQty <= currentQty)
                    {
                        frmVoid F = new frmVoid(this);
                        F.ShowDialog();
                    }
                    else
                    {
                        MessageBox.Show("Cancel quantity cannot be greater than the sold quantity.", "Invalid Quantity", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                }
                else
                {
                    MessageBox.Show("Please enter valid numeric quantities.", "Input Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        public void RefreshList()
        {
            // Changed from LoadRecords() to LocalRecord() to match the method name in your frmSoldItems class
            f.LocalRecord();
        }
    }
}