using System;
using System.Data.SQLite;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PosSystem
{
    public partial class frmDiscount : Form
    {
        private readonly string stitle = "POS System";
        private readonly Form1 f;
        private readonly frmPOS fPOS;

        public frmDiscount(Form1 frm)
        {
            InitializeComponent();
            f = frm;
            KeyPreview = true;
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

        // =========================
        // 🔹 CALCULATION LOGIC
        // =========================
        private void txtDiscount_TextChanged(object sender, EventArgs e)
        {
            CalculateDiscount();
        }

        private void txtPrice_TextChanged(object sender, EventArgs e)
        {
            CalculateDiscount();
        }

        private void CalculateDiscount()
        {
            if (!double.TryParse(txtPrice.Text, out double price))
                price = 0;

            if (!double.TryParse(txtDiscount.Text, out double percent))
                percent = 0;

            // Ensure discount % is within logical range
            if (percent < 0) percent = 0;
            if (percent > 100) percent = 100;

            double discount = price * (percent / 100.0);
            txtAmount.Text = discount.ToString("#,##0.00");
        }

        // =========================
        // 🔹 CONFIRM DISCOUNT (ASYNC + VALIDATION)
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

                if (!double.TryParse(txtDiscount.Text, out double discPercent))
                    discPercent = 0;

                if (!double.TryParse(txtAmount.Text, out double discAmount))
                    discAmount = 0;

                // 🔹 LOGICAL VALIDATION
                if (discPercent < 0 || discPercent > 100)
                {
                    MessageBox.Show("Discount must be between 0% and 100%.", stitle,
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    txtDiscount.Focus();
                    return;
                }

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

                // Refresh cart if POS form is active
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
    }
}