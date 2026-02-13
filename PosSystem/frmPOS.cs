using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.IO;
using System.Drawing;
using System.Drawing.Printing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using Tulpep.NotificationWindow;

namespace PosSystem
{
    public partial class frmPOS : Form
    {
        string stitle = "POS System v1.0 - ELITE";
        frmUserLogin f;
        PrintDocument pd = new PrintDocument();
        System.Windows.Forms.Timer barcodeDelayTimer = new System.Windows.Forms.Timer();

        string logFile = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "PosSystem", "pos_error.log");

        public frmPOS(frmUserLogin frm)
        {
            InitializeComponent();
            f = frm;
            this.KeyPreview = true;
            barcodeDelayTimer.Interval = 400;
            barcodeDelayTimer.Tick += BarcodeDelayTimer_Tick;
            EnsureLogDirectory();
        }

        private void frmPOS_Load(object sender, EventArgs e)
        {
            LoadStoreInfo();
            GetTransNo();
            LoadCart();
            txtSearch.Focus();
        }

        #region BARCODE LOGIC
        private void txtSearch_TextChanged(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtSearch.Text)) return;
            barcodeDelayTimer.Stop();
            barcodeDelayTimer.Start();
        }

        private void BarcodeDelayTimer_Tick(object sender, EventArgs e)
        {
            barcodeDelayTimer.Stop();
            ProcessBarcode(txtSearch.Text.Trim());
            txtSearch.Clear();
            txtSearch.Focus();
        }

        private void ProcessBarcode(string barcode)
        {
            try
            {
                using (SQLiteConnection cn = new SQLiteConnection(DBConnection.MyConnection()))
                {
                    cn.Open();
                    string sql = "SELECT * FROM TblProduct1 WHERE (barcode = @barcode OR pcode = @barcode) AND isactive = 1";
                    using (SQLiteCommand cmd = new SQLiteCommand(sql, cn))
                    {
                        cmd.Parameters.AddWithValue("@barcode", barcode);
                        using (SQLiteDataReader dr = cmd.ExecuteReader())
                        {
                            if (dr.Read())
                            {
                                string pcode = dr["pcode"].ToString();
                                double price = Convert.ToDouble(dr["price"]);
                                int stock = Convert.ToInt32(dr["qty"]);
                                using (frmQty qtyForm = new frmQty(this))
                                {
                                    qtyForm.ProductDetails(pcode, price, lblTransno.Text, stock);
                                    qtyForm.ShowDialog();
                                }
                                LoadCart();
                            }
                        }
                    }
                }
            }
            catch (Exception ex) { LogError("Barcode", ex); }
        }
        #endregion

        #region BUTTONS
        private void btnTrans_Click(object sender, EventArgs e)
        {
            if (dataGridView1.Rows.Count > 0)
            {
                if (MessageBox.Show("Discard current cart?", stitle, MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.No) return;
            }
            GetTransNo();
            LoadCart();
            txtSearch.Focus();
        }

        private void btnSearch_Click(object sender, EventArgs e)
        {
            using (frmLookUp lookup = new frmLookUp())
            {
                if (lookup.ShowDialog() == DialogResult.OK)
                {
                    using (frmQty qtyForm = new frmQty(this))
                    {
                        qtyForm.ProductDetails(lookup.SelectedPCode, (double)lookup.SelectedPrice, lblTransno.Text, lookup.SelectedStock);
                        qtyForm.ShowDialog();
                    }
                    LoadCart();
                }
            }
        }

        private void btnDiscount_Click(object sender, EventArgs e)
        {
            using (frmDiscount frm = new frmDiscount(this)) { frm.ShowDialog(); }
            LoadCart();
        }

        private void btnSattle_Click(object sender, EventArgs e)
        {
            if (dataGridView1.Rows.Count == 0) return;

            using (frmSettel frm = new frmSettel(this))
            {
                // Mapping the Display Total to your txtSale field in the Settle form
                frm.txtSale.Text = lblDisplayTotal.Text;
                if (frm.ShowDialog() == DialogResult.OK)
                {
                    GetTransNo();
                    LoadCart();
                }
            }
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            using (frmConfirmPaasword frm = new frmConfirmPaasword())
            {
                if (frm.ShowDialog() == DialogResult.OK)
                {
                    using (frmCancelDetails cancelFrm = new frmCancelDetails(null, 0))
                    {
                        cancelFrm.ShowDialog();
                    }
                    LoadCart();
                }
            }
        }

        private void btnSales_Click(object sender, EventArgs e)
        {
            using (frmReportSold frm = new frmReportSold(null))
            {
                frm.ShowDialog();
            }
        }

        private void btnClose_Click(object sender, EventArgs e) { Application.Exit(); }
        #endregion

        #region CORE FUNCTIONS
        public void LoadCart()
        {
            dataGridView1.Rows.Clear();
            decimal total = 0; decimal discount = 0;
            try
            {
                using (SQLiteConnection cn = new SQLiteConnection(DBConnection.MyConnection()))
                {
                    cn.Open();
                    string sql = @"SELECT c.*, p.pdesc FROM tblCart1 c INNER JOIN TblProduct1 p ON c.pcode = p.pcode WHERE c.transno = @transno AND c.status = 'Pending'";
                    using (SQLiteCommand cmd = new SQLiteCommand(sql, cn))
                    {
                        cmd.Parameters.AddWithValue("@transno", lblTransno.Text);
                        using (SQLiteDataReader dr = cmd.ExecuteReader())
                        {
                            while (dr.Read())
                            {
                                total += Convert.ToDecimal(dr["total"]);
                                discount += Convert.ToDecimal(dr["disc"]);
                                dataGridView1.Rows.Add(dr["id"], dr["transno"], dr["pcode"], dr["pdesc"], dr["price"], dr["qty"], dr["disc"], dr["total"]);
                            }
                        }
                    }
                }
                lblDisplayTotal.Text = total.ToString("#,##0.00");
                lblTotal.Text = total.ToString("#,##0.00");
                lblDiscount.Text = discount.ToString("#,##0.00");
            }
            catch (Exception ex) { LogError("LoadCart", ex); }
        }

        public void GetTransNo()
        {
            try
            {
                using (SQLiteConnection cn = new SQLiteConnection(DBConnection.MyConnection()))
                {
                    cn.Open();
                    using (SQLiteCommand cmd = new SQLiteCommand("SELECT transno FROM tblCart1 ORDER BY id DESC LIMIT 1", cn))
                    {
                        object obj = cmd.ExecuteScalar();
                        string sdate = DateTime.Now.ToString("yyyyMMdd");
                        if (obj != null)
                        {
                            string lastTrans = obj.ToString();
                            long count = long.Parse(lastTrans.Substring(8)) + 1;
                            lblTransno.Text = sdate + count.ToString("D4");
                        }
                        else { lblTransno.Text = sdate + "0001"; }
                    }
                }
            }
            catch { lblTransno.Text = DateTime.Now.ToString("yyyyMMdd0001"); }
        }

        private void LoadStoreInfo()
        {
            try
            {
                using (SQLiteConnection cn = new SQLiteConnection(DBConnection.MyConnection()))
                {
                    cn.Open();
                    using (SQLiteCommand cmd = new SQLiteCommand("SELECT store FROM tblStore LIMIT 1", cn))
                    {
                        lblSname.Text = cmd.ExecuteScalar()?.ToString() ?? "ELITE POS SYSTEM";
                    }
                }
            }
            catch { lblSname.Text = "ELITE POS SYSTEM"; }
        }

        private void LogError(string ctx, Exception ex) => File.AppendAllText(logFile, $"{DateTime.Now} [{ctx}] {ex}\n");
        private void EnsureLogDirectory() => Directory.CreateDirectory(Path.GetDirectoryName(logFile));
        private void timer1_Tick(object sender, EventArgs e) => lblDate.Text = DateTime.Now.ToString("MMMM dd, yyyy | hh:mm:ss tt");

        private void frmPOS_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.F1) btnTrans_Click(sender, e);
            else if (e.KeyCode == Keys.F2) btnSearch_Click(sender, e);
            else if (e.KeyCode == Keys.F3) btnDiscount_Click(sender, e);
            else if (e.KeyCode == Keys.F4) btnSattle_Click(sender, e);
            else if (e.KeyCode == Keys.F5) btnCancel_Click(sender, e);
            else if (e.KeyCode == Keys.F6) btnSales_Click(sender, e);
            else if (e.KeyCode == Keys.F10) btnClose_Click(sender, e);
        }
        #endregion

        #region DESIGNER GHOST FIXES (STOPS CS1061 ERRORS)
        private void label2_Click(object sender, EventArgs e) { }
        private void panel6_Paint(object sender, PaintEventArgs e) { }
        private void label14_Click(object sender, EventArgs e) { }
        private void lblSname_Click(object sender, EventArgs e) { }
        private void LblUser_Click(object sender, EventArgs e) { }
        private void lblName_Click(object sender, EventArgs e) { }
        private void label7_Click(object sender, EventArgs e) { }
        private void dataGridView1_CellContentClick(object sender, DataGridViewCellEventArgs e) { }
        private void dataGridView1_SelectionChanged(object sender, EventArgs e) { }
        private void label6_Click(object sender, EventArgs e) { }
        #endregion

        private void lblAddress_Click(object sender, EventArgs e)
        {

        }

        private void lblPhone_Click(object sender, EventArgs e)
        {

        }
    }
}