using System;
using System.Data.SQLite;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace PosSystem
{
    public partial class frmQty : Form
    {
        private string pcode;
        private double price;
        private int qty;
        private string transno;

        // References to parent forms
        private Form1 f;
        private frmPOS fPOS;

        public frmQty(Form1 frm)
        {
            InitializeComponent();
            f = frm;
        }

        public frmQty(frmPOS frm)
        {
            InitializeComponent();
            fPOS = frm;
        }

        private void frmQty_Load(object sender, EventArgs e)
        {
            txtQty.Focus();
        }

        public void ProductDetails(string pcode, double price, string transno, int qty)
        {
            this.pcode = pcode;
            this.price = price;
            this.transno = transno;
            this.qty = qty;
        }

        private void txtQty_KeyPress(object sender, KeyPressEventArgs e)
        {
            // Enter key pressed
            if (e.KeyChar != (char)13) return;

            if (string.IsNullOrWhiteSpace(txtQty.Text))
            {
                MessageBox.Show("Please enter a quantity.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtQty.Focus();
                return;
            }

            if (!int.TryParse(txtQty.Text, out int inputQty))
            {
                MessageBox.Show("Invalid quantity entered.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtQty.Focus();
                return;
            }

            try
            {
                string currentTransNo = fPOS != null ? fPOS.lblTransno.Text : transno;
                string currentUser = fPOS != null ? fPOS.LblUser.Text : f?.lblUser.Text ?? "Unknown Cashier";

                // Check if product already exists in cart
                bool found = false;
                int cartQty = 0;
                string cartId = "";

                using (var cn = new SQLiteConnection(DBConnection.MyConnection()))
                {
                    cn.Open();

                    using (var cmd = new SQLiteCommand(
                        "SELECT id, qty FROM tblCart1 WHERE transno = @transno AND pcode = @pcode", cn))
                    {
                        cmd.Parameters.AddWithValue("@transno", currentTransNo);
                        cmd.Parameters.AddWithValue("@pcode", pcode);

                        using (var dr = cmd.ExecuteReader())
                        {
                            if (dr.Read())
                            {
                                found = true;
                                cartId = dr["id"].ToString();
                                cartQty = Convert.ToInt32(dr["qty"]);
                            }
                        }
                    }

                    if (found)
                    {
                        if (qty < (inputQty + cartQty))
                        {
                            MessageBox.Show($"Unable to proceed. Remaining qty on hand is {qty}.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            return;
                        }

                        using (var cmd = new SQLiteCommand("UPDATE tblCart1 SET qty = qty + @inputQty WHERE id = @id", cn))
                        {
                            cmd.Parameters.AddWithValue("@inputQty", inputQty);
                            cmd.Parameters.AddWithValue("@id", cartId);
                            cmd.ExecuteNonQuery();
                        }
                    }
                    else
                    {
                        if (qty < inputQty)
                        {
                            MessageBox.Show($"Unable to proceed. Remaining qty on hand is {qty}.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            return;
                        }

                        using (var cmd = new SQLiteCommand(
                            "INSERT INTO tblCart1 (transno, pcode, price, qty, sdate, cashier) VALUES (@transno, @pcode, @price, @qty, @sdate, @cashier)", cn))
                        {
                            cmd.Parameters.AddWithValue("@transno", currentTransNo);
                            cmd.Parameters.AddWithValue("@pcode", pcode);
                            cmd.Parameters.AddWithValue("@price", price);
                            cmd.Parameters.AddWithValue("@qty", inputQty);
                            cmd.Parameters.AddWithValue("@sdate", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                            cmd.Parameters.AddWithValue("@cashier", currentUser);
                            cmd.ExecuteNonQuery();
                        }
                    }
                }

                // Refresh parent POS UI
                if (fPOS != null)
                {
                    fPOS.LoadCart();
                    fPOS.txtSearch.Clear();
                    fPOS.txtSearch.Focus();
                }

                this.Dispose();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void txtQty_TextChanged(object sender, EventArgs e)
        {
            // Remove non-numeric characters instead of clearing the entire box
            if (Regex.IsMatch(txtQty.Text, "[^0-9]"))
            {
                int cursorPos = txtQty.SelectionStart - 1;
                txtQty.Text = Regex.Replace(txtQty.Text, "[^0-9]", "");
                txtQty.SelectionStart = Math.Max(cursorPos, 0);
            }
        }
    }
}