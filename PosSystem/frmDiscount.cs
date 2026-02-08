using System;
using System.Data;
using System.Data.SQLite;
using System.Windows.Forms;

namespace PosSystem
{
    public partial class frmDiscount : Form
    {
        private SQLiteConnection cn;
        private string stitle = "Pos System";
        private Form1 f;
        private frmPOS fPOS;

        public frmDiscount(Form1 frm)
        {
            InitializeComponent();
            f = frm;
            cn = new SQLiteConnection(DBConnection.MyConnection());
            this.KeyPreview = true;
        }

        public frmDiscount(frmPOS frm)
        {
            InitializeComponent();
            fPOS = frm;
            cn = new SQLiteConnection(DBConnection.MyConnection());
            this.KeyPreview = true;
        }

        private void pictureBox2_Click(object sender, EventArgs e)
        {
            this.Dispose();
        }

        private void txtDiscount_TextChanged(object sender, EventArgs e)
        {
            try
            {
                double price = string.IsNullOrEmpty(txtPrice.Text) ? 0 : double.Parse(txtPrice.Text);
                double discountPercent = string.IsNullOrEmpty(txtDiscount.Text) ? 0 : double.Parse(txtDiscount.Text);

                double discount = price * (discountPercent / 100.0);
                txtAmount.Text = discount.ToString("#,##0.00");
            }
            catch
            {
                txtAmount.Text = "0.00";
            }
        }

        private void btnConfirm_Click(object sender, EventArgs e)
        {
            try
            {
                if (MessageBox.Show("Add Discount? Click yes to confirm.", stitle, MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    cn.Open();
                    using (SQLiteCommand cm = new SQLiteCommand("UPDATE tblCart1 SET disc = @disc, disc_precent = @disc_precent WHERE id = @id", cn))
                    {
                        double discAmount = string.IsNullOrEmpty(txtAmount.Text) ? 0 : double.Parse(txtAmount.Text);
                        double discPercent = string.IsNullOrEmpty(txtDiscount.Text) ? 0 : double.Parse(txtDiscount.Text);

                        cm.Parameters.AddWithValue("@disc", discAmount);
                        cm.Parameters.AddWithValue("@disc_precent", discPercent);
                        cm.Parameters.AddWithValue("@id", int.Parse(lblID.Text));

                        cm.ExecuteNonQuery();
                    }
                    cn.Close();

                    // FIX: Only call LoadCart if fPOS is the active parent
                    if (fPOS != null)
                    {
                        fPOS.LoadCart();
                    }

                    this.Dispose();
                }
            }
            catch (Exception ex)
            {
                if (cn.State == ConnectionState.Open) cn.Close();
                MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void txtPrice_TextChanged(object sender, EventArgs e)
        {
            // You can leave this empty or add logic here
        }

        private void frmDiscount_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape)
            {
                this.Dispose();
            }
            else if (e.KeyCode == Keys.Enter)
            {
                btnConfirm_Click(sender, e);
            }
        }

        private void frmDiscount_Load(object sender, EventArgs e)
        {
            txtDiscount.Focus();
        }
    }
}