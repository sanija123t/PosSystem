using System;
using System.Data.SQLite;
using System.Threading.Tasks;
using System.Runtime.InteropServices; // Added for draggable logic
using System.Windows.Forms;

namespace PosSystem
{
    public partial class frmDiscount : Form
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

        private readonly string stitle = "POS System";
        private readonly frmPOS fPOS;

        // 🔹 Callback approach for decoupling
        public Action<double, double> OnDiscountApplied;

        // ✅ Property for external access
        public double DiscountAmount
        {
            get
            {
                if (double.TryParse(txtAmount.Text, out double amount))
                    return amount;
                return 0;
            }
            set
            {
                txtAmount.Text = value.ToString("0.00");
            }
        }

        public frmDiscount(frmPOS frm)
        {
            InitializeComponent();
            fPOS = frm;
            KeyPreview = true;
        }

        private void pictureBox2_Click(object sender, EventArgs e)
        {
            Dispose();
        }

        // ============================================================
        // 🔹 ELITE CALCULATION ENGINE (Bi-Directional)
        // ============================================================

        // Triggers when Percentage is typed
        private void txtDiscount_TextChanged(object sender, EventArgs e)
        {
            if (txtDiscount.Focused) // Only calculate if user is actually typing here
            {
                CalculateFromPercentage();
            }
        }

        // Triggers when Amount is typed
        private void txtAmount_TextChanged(object sender, EventArgs e)
        {
            if (txtAmount.Focused) // Only calculate if user is actually typing here
            {
                CalculateFromAmount();
            }
        }

        private void CalculateFromPercentage()
        {
            try
            {
                double price = double.TryParse(txtPrice.Text, out double p) ? p : 0;
                double percent = double.TryParse(txtDiscount.Text, out double d) ? d : 0;

                // Elite Clamping
                if (percent > 100) percent = 100;

                double amount = price * (percent / 100.0);
                txtAmount.Text = amount.ToString("0.00");
            }
            catch { }
        }

        private void CalculateFromAmount()
        {
            try
            {
                double price = double.TryParse(txtPrice.Text, out double p) ? p : 0;
                double amount = double.TryParse(txtAmount.Text, out double a) ? a : 0;

                if (price > 0)
                {
                    // Elite Clamping: Cannot discount more than the price
                    if (amount > price) amount = price;

                    double percent = (amount / price) * 100.0;
                    txtDiscount.Text = percent.ToString("0.00");
                }
            }
            catch { }
        }

        private void txtPrice_TextChanged(object sender, EventArgs e)
        {
            // If price changes, update the amount based on current percentage
            CalculateFromPercentage();
        }

        // =========================
        // 🔹 CONFIRM DISCOUNT
        // =========================
        private async void btnConfirm_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("Add Discount? Click Yes to confirm.", stitle,
                MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                return;

            btnConfirm.Enabled = false;

            try
            {
                if (!int.TryParse(lblID.Text, out int cartId))
                    throw new Exception("Invalid cart item ID.");

                double.TryParse(txtDiscount.Text, out double discPercent);
                double.TryParse(txtAmount.Text, out double discAmount);

                string sql = @"UPDATE tblCart1 
                               SET disc = @disc, disc_precent = @disc_precent 
                               WHERE id = @id";

                using (var cn = new SQLiteConnection(DBConnection.MyConnection()))
                using (var cm = new SQLiteCommand(sql, cn))
                {
                    cm.Parameters.AddWithValue("@disc", discAmount);
                    cm.Parameters.AddWithValue("@disc_precent", discPercent);
                    cm.Parameters.AddWithValue("@id", cartId);

                    await cn.OpenAsync();
                    await cm.ExecuteNonQueryAsync();
                }

                OnDiscountApplied?.Invoke(discAmount, discPercent);
                fPOS?.LoadCart();
                Dispose();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            finally
            {
                btnConfirm.Enabled = true;
            }
        }

        // =========================
        // 🔹 SHORTCUTS & UX
        // =========================
        private void frmDiscount_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape)
                Dispose();
            else if (e.KeyCode == Keys.Enter)
                btnConfirm_Click(sender, e);
        }

        private void frmDiscount_Load(object sender, EventArgs e)
        {
            txtDiscount.Focus();
        }

        // =========================
        // 🔹 PRESERVED JUNK LINES
        // =========================
        private void CalculateDiscount() { /* Old method logic replaced by Bi-Directional Engine */ }
    }
}