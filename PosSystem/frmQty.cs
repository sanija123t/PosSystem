using System;
using System.Data;
using System.Data.SQLite;
using System.Windows.Forms;

namespace PosSystem
{
    public partial class frmQty : Form
    {
        SQLiteConnection cn;
        SQLiteCommand cm;
        SQLiteDataReader dr;

        private string pcode;
        private double price;
        private int qty;
        private string transno;

        // References for parent forms
        Form1 f;
        frmPOS fPOS;

        public frmQty(Form1 frm)
        {
            InitializeComponent();
            cn = new SQLiteConnection(DBConnection.MyConnection());
            f = frm;
        }

        public frmQty(frmPOS frm)
        {
            InitializeComponent();
            cn = new SQLiteConnection(DBConnection.MyConnection());
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
            if ((e.KeyChar == 13) && (!string.IsNullOrEmpty(txtQty.Text)))
            {
                try
                {
                    string id = "";
                    bool found = false;
                    int cart_qty = 0;
                    int inputQty = int.Parse(txtQty.Text);

                    // --- Parent Data Resolution ---
                    string currentTransNo = (fPOS != null) ? fPOS.lblTransno.Text : transno;

                    // Handle the User/Cashier Name
                    string currentUser = "Unknown Cashier";
                    if (fPOS != null)
                    {
                        currentUser = fPOS.LblUser.Text;
                    }
                    else if (f != null)
                    {
                        currentUser = f.lblUser.Text;
                    }

                    cn.Open();
                    cm = new SQLiteCommand("SELECT id, qty FROM tblCart1 WHERE transno = @transno AND pcode = @pcode", cn);
                    cm.Parameters.AddWithValue("@transno", currentTransNo);
                    cm.Parameters.AddWithValue("@pcode", pcode);
                    dr = cm.ExecuteReader();

                    if (dr.Read())
                    {
                        found = true;
                        id = dr["id"].ToString();
                        cart_qty = int.Parse(dr["qty"].ToString());
                    }
                    dr.Close();
                    cn.Close();

                    if (found)
                    {
                        if (qty < (inputQty + cart_qty))
                        {
                            MessageBox.Show("Unable to proceed. Remaining qty on hand is " + qty, "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            return;
                        }

                        cn.Open();
                        cm = new SQLiteCommand("UPDATE tblCart1 SET qty = (qty + @inputQty) WHERE id = @id", cn);
                        cm.Parameters.AddWithValue("@inputQty", inputQty);
                        cm.Parameters.AddWithValue("@id", id);
                        cm.ExecuteNonQuery();
                        cn.Close();
                    }
                    else
                    {
                        if (qty < inputQty)
                        {
                            MessageBox.Show("Unable to proceed. Remaining qty on hand is " + qty, "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            return;
                        }

                        cn.Open();
                        cm = new SQLiteCommand("INSERT INTO tblCart1 (transno, pcode, price, qty, sdate, cashier) VALUES (@transno, @pcode, @price, @qty, @sdate, @cashier)", cn);
                        cm.Parameters.AddWithValue("@transno", currentTransNo);
                        cm.Parameters.AddWithValue("@pcode", pcode);
                        cm.Parameters.AddWithValue("@price", price);
                        cm.Parameters.AddWithValue("@qty", inputQty);
                        cm.Parameters.AddWithValue("@sdate", DateTime.Now.ToString("yyyy-MM-dd"));
                        cm.Parameters.AddWithValue("@cashier", currentUser);
                        cm.ExecuteNonQuery();
                        cn.Close();
                    }

                    // --- UI Refresh Logic ---
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
                    if (cn.State == ConnectionState.Open) cn.Close();
                    MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void txtQty_TextChanged(object sender, EventArgs e)
        {
            if (System.Text.RegularExpressions.Regex.IsMatch(txtQty.Text, "[^0-9]"))
            {
                MessageBox.Show("Please enter only numbers.", "Invalid Input", MessageBoxButtons.OK, MessageBoxIcon.Information);
                txtQty.Text = "";
            }
        }
    }
}