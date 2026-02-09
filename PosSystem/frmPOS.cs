using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using Tulpep.NotificationWindow;
using System.Data.SQLite;
using MetroFramework.Controls;

namespace PosSystem
{
    public partial class frmPOS : Form
    {
        SQLiteConnection cn;
        SQLiteCommand cm;
        SQLiteDataReader dr;
        string stitle = "POS System";
        frmUserLogin f;

        public frmPOS(frmUserLogin frm)
        {
            InitializeComponent();
            cn = new SQLiteConnection(DBConnection.MyConnection());
            f = frm;
            NotifyCriticalItems();
            this.KeyPreview = true;
        }

        // --- FIXED ACCESSORS (Matching your Designer Names) ---
        public Label GetlblTransno { get { return lblTransno; } }

        // Match the capital 'L' in LblUser from your Designer
        public Label GetlblUser { get { return LblUser; } }

        public MetroTextBox GettxtSearch { get { return txtSearch; } }

        private void frmPOS_Load(object sender, EventArgs e)
        {
            timer1.Start();
            LoadStoreInfo();
        }

        public void LoadStoreInfo()
        {
            try
            {
                cn.Open();
                cm = new SQLiteCommand("SELECT * FROM tblStore", cn);
                dr = cm.ExecuteReader();
                if (dr.Read())
                {
                    lblAddress.Text = dr["address"].ToString();
                    lblSname.Text = dr["store"].ToString();
                    lblPhone.Text = dr["phone"].ToString();
                }
                dr.Close();
            }
            catch (Exception ex) { MessageBox.Show(ex.Message, stitle, MessageBoxButtons.OK, MessageBoxIcon.Error); }
            finally { cn.Close(); }
        }

        public void LoadCart()
        {
            try
            {
                dataGridView1.Rows.Clear();
                int i = 0;
                double total = 0;
                double discount = 0;
                cn.Open();
                cm = new SQLiteCommand("SELECT c.id, c.pcode, p.pdesc, c.price, c.qty, c.disc, (c.price * c.qty) - c.disc as total FROM tblCart1 AS c INNER JOIN TblProduct1 AS p ON c.pcode = p.pcode WHERE transno LIKE @transno AND status LIKE 'Pending'", cn);
                cm.Parameters.AddWithValue("@transno", lblTransno.Text);
                dr = cm.ExecuteReader();
                while (dr.Read())
                {
                    i++;
                    total += Double.Parse(dr["total"].ToString());
                    discount += Double.Parse(dr["disc"].ToString());
                    dataGridView1.Rows.Add(i, dr["id"].ToString(), dr["pcode"].ToString(), dr["pdesc"].ToString(), dr["price"].ToString(), dr["qty"].ToString(), dr["disc"].ToString(), Double.Parse(dr["total"].ToString()).ToString("#,##0.00"));
                }
                dr.Close();
                cn.Close();
                lblTotal.Text = total.ToString("#,##0.00");
                lblDiscount.Text = discount.ToString("#,##0.00");
                GetCartTotal();
            }
            catch (Exception ex) { cn.Close(); MessageBox.Show(ex.Message, stitle); }
        }

        public void GetCartTotal()
        {
            double discount = Double.Parse(lblDiscount.Text);
            double sales = Double.Parse(lblTotal.Text) - discount;
            double vat = sales * DBConnection.GetVal();
            double vatable = sales - vat;

            lblDisplayTotal.Text = sales.ToString("#,##0.00");
            lblVat.Text = vat.ToString("#,##0.00");
            lblVatable.Text = vatable.ToString("#,##0.00");
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            label2.Text = DateTime.Now.ToLongTimeString();
            label6.Text = DateTime.Now.ToLongDateString();
        }

        public void GetTransNo()
        {
            try
            {
                string sdate = DateTime.Now.ToString("yyyyMMdd");
                string transno;
                int count;
                cn.Open();
                cm = new SQLiteCommand("SELECT transno FROM tblCart1 WHERE transno LIKE '" + sdate + "%' ORDER BY id DESC LIMIT 1", cn);
                dr = cm.ExecuteReader();
                dr.Read();
                if (dr.HasRows)
                {
                    transno = dr[0].ToString();
                    count = int.Parse(transno.Substring(8, 4));
                    lblTransno.Text = sdate + (count + 1);
                }
                else
                {
                    transno = sdate + "1001";
                    lblTransno.Text = transno;
                }
                dr.Close();
                cn.Close();
            }
            catch (Exception ex) { cn.Close(); MessageBox.Show(ex.Message, stitle); }
        }

        private void btnClose_Click(object sender, EventArgs e)
        {
            if (dataGridView1.Rows.Count > 0)
            {
                MessageBox.Show("Unable to close. Please cancel the transaction first.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            this.Dispose();
        }

        private void btnSattle_Click(object sender, EventArgs e)
        {
            frmSettel frm = new frmSettel(this);
            frm.txtSale.Text = lblDisplayTotal.Text;
            frm.ShowDialog();
        }

        public void NotifyCriticalItems()
        {
            try
            {
                cn.Open();
                cm = new SQLiteCommand("SELECT * FROM vwCriticalItems", cn);
                dr = cm.ExecuteReader();
                while (dr.Read())
                {
                    PopupNotifier popup = new PopupNotifier();
                    popup.TitleText = "Critical Item Warning";
                    popup.ContentText = dr["pdesc"].ToString() + " is currently low on stock (" + dr["qty"].ToString() + ")";
                    popup.Popup();
                }
                dr.Close();
                cn.Close();
            }
            catch { cn.Close(); }
        }

        private void txtSearch_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                if (txtSearch.Text == string.Empty) { return; }

                try
                {
                    cn.Open();
                    cm = new SQLiteCommand("SELECT * FROM TblProduct1 WHERE barcode LIKE @barcode", cn);
                    cm.Parameters.AddWithValue("@barcode", txtSearch.Text);
                    dr = cm.ExecuteReader();
                    dr.Read();
                    if (dr.HasRows)
                    {
                        string pcode = dr["pcode"].ToString();
                        double price = double.Parse(dr["price"].ToString());
                        int qty = int.Parse(dr["reorder"].ToString()); // Getting default qty/reorder
                        dr.Close();
                        cn.Close();

                        frmQty frm = new frmQty(this);
                        // FIXED: Added missing 4th parameter 'qty' to match frmQty.ProductDetails signature
                        frm.ProductDetails(pcode, price, lblTransno.Text, qty);
                        frm.ShowDialog();
                    }
                    else
                    {
                        dr.Close();
                        cn.Close();
                    }
                    txtSearch.Clear();
                }
                catch (Exception ex)
                {
                    cn.Close();
                    MessageBox.Show(ex.Message, stitle, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
        }

        private void btnNew_Click(object sender, EventArgs e)
        {
            if (dataGridView1.Rows.Count > 0) { return; }
            GetTransNo();
            txtSearch.Focus();
        }

        // Empty stubs to prevent designer errors
        private void label14_Click(object sender, EventArgs e) { }
        private void LblUser_Click(object sender, EventArgs e) { }
        private void lblName_Click(object sender, EventArgs e) { }
        private void label7_Click(object sender, EventArgs e) { }
        private void label2_Click(object sender, EventArgs e) { }
        private void dataGridView1_CellContentClick(object sender, DataGridViewCellEventArgs e) { }
        private void dataGridView1_SelectionChanged(object sender, EventArgs e) { }
    }
}