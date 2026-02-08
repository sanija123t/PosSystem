using System;
using System.Data;
using System.Windows.Forms;
using System.Data.SQLite;

namespace PosSystem
{
    public partial class frmSettel : Form
    {
        // Connection string retrieved from static class
        private string connectionString = DBConnection.MyConnection();
        private Form1 f;
        private frmPOS fPOS;

        public frmSettel(Form1 fp)
        {
            InitializeComponent();
            f = fp;
            this.KeyPreview = true;
        }

        public frmSettel(frmPOS fp)
        {
            InitializeComponent();
            fPOS = fp;
            this.KeyPreview = true;
        }

        private void frmSettel_Load(object sender, EventArgs e)
        {
            // Your code to run when the settlement window opens
        }

        private void txtSale_TextChanged(object sender, EventArgs e)
        {
            // Your code to run when the sale amount changes
        }
        private void pictureBox2_Click(object sender, EventArgs e) => this.Dispose();

        private void txtCash_TextChanged(object sender, EventArgs e)
        {
            try
            {
                double sale = double.Parse(txtSale.Text);
                double cash = string.IsNullOrEmpty(txtCash.Text) ? 0 : double.Parse(txtCash.Text);
                double change = cash - sale;
                txtChange.Text = change.ToString("#,##0.00");
            }
            catch
            {
                txtChange.Text = "0.00";
            }
        }

        private void btnEnter_Click(object sender, EventArgs e)
        {
            try
            {
                double changeAmount = 0;
                double.TryParse(txtChange.Text, out changeAmount);

                if (string.IsNullOrEmpty(txtCash.Text) || changeAmount < 0)
                {
                    MessageBox.Show("Insufficient amount. Please enter the correct amount!", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                // Identify which DataGridView to use from the parent forms
                DataGridView dgv = (fPOS != null) ? fPOS.dataGridView1 : f.dataGridView1;

                if (dgv.Rows.Count == 0) return;

                using (SQLiteConnection cn = new SQLiteConnection(connectionString))
                {
                    cn.Open();
                    using (var transaction = cn.BeginTransaction())
                    {
                        for (int i = 0; i < dgv.Rows.Count; i++)
                        {
                            // Ensure cells aren't null before calling ToString()
                            string pcode = dgv.Rows[i].Cells[2].Value?.ToString();
                            int qty = int.Parse(dgv.Rows[i].Cells[5].Value?.ToString() ?? "0");
                            int cartId = int.Parse(dgv.Rows[i].Cells[1].Value?.ToString() ?? "0");

                            // 1. Update Product Stock
                            using (SQLiteCommand cmd = new SQLiteCommand("UPDATE TblProduct1 SET qty = qty - @qty WHERE pcode = @pcode", cn))
                            {
                                cmd.Parameters.AddWithValue("@qty", qty);
                                cmd.Parameters.AddWithValue("@pcode", pcode);
                                cmd.ExecuteNonQuery();
                            }

                            // 2. Update Cart Status
                            using (SQLiteCommand cmd = new SQLiteCommand("UPDATE tblCart1 SET status = 'Sold' WHERE id = @id", cn))
                            {
                                cmd.Parameters.AddWithValue("@id", cartId);
                                cmd.ExecuteNonQuery();
                            }
                        }
                        transaction.Commit();
                    }
                }

                // 3. Handle Receipt Generation (CS1503 Fix)
                // Passing the active form as a generic Form object
                Form activeForm = (fPOS != null) ? (Form)fPOS : (Form)f;
                frmResipt frm = new frmResipt(activeForm);
                frm.LoadReport(txtCash.Text, txtChange.Text);
                frm.ShowDialog();

                MessageBox.Show("Payment successfully saved!", "Payment", MessageBoxButtons.OK, MessageBoxIcon.Information);

                // 4. Reset Parent Form
                if (fPOS != null)
                {
                    fPOS.GetTransNo();
                    fPOS.LoadCart();
                }
                else
                {
                    f.GetTransNo();
                    f.LoadCart();
                }

                this.Dispose();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        #region Numeric Keypad Logic
        private void btn7_Click(object sender, EventArgs e) => txtCash.Text += "7";
        private void btn8_Click(object sender, EventArgs e) => txtCash.Text += "8";
        private void btn9_Click(object sender, EventArgs e) => txtCash.Text += "9";
        private void btn4_Click(object sender, EventArgs e) => txtCash.Text += "4";
        private void btn5_Click(object sender, EventArgs e) => txtCash.Text += "5";
        private void btn6_Click(object sender, EventArgs e) => txtCash.Text += "6";
        private void btn1_Click(object sender, EventArgs e) => txtCash.Text += "1";
        private void btn2_Click(object sender, EventArgs e) => txtCash.Text += "2";
        private void btn3_Click(object sender, EventArgs e) => txtCash.Text += "3";
        private void btn0_Click(object sender, EventArgs e) => txtCash.Text += "0";
        private void btn00_Click(object sender, EventArgs e) => txtCash.Text += "00";
        private void btnC_Click(object sender, EventArgs e) { txtCash.Clear(); txtCash.Focus(); }
        #endregion

        private void frmSettel_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape) this.Dispose();
            else if (e.KeyCode == Keys.Enter) btnEnter_Click(sender, e);
        }
    }
}