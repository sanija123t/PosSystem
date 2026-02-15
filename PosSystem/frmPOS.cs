using System;
using System.Data;
using System.Data.SQLite;
using System.IO;
using System.Drawing.Printing;
using System.Runtime.InteropServices; // Added for draggable logic
using System.Windows.Forms;
using Tulpep.NotificationWindow;

namespace PosSystem
{
    public partial class frmPOS : Form
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

        string stitle = "POS System v1.0 - ELITE";
        Form1 f;
        PrintDocument pd = new PrintDocument();
        Timer barcodeDelayTimer = new Timer();

        string logFile = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "PosSystem", "pos_error.log");

        public frmPOS(Form1 frm)
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
            txtSearch?.Focus();

            if (f != null)
            {
                LblUser.Text = f._user;
                lblName.Text = f._name;
            }
        }

        #region FINALIZE TRANSACTION
        public void Elite_FinalizeTransaction(string transno)
        {
            try
            {
                using (SQLiteConnection cn = new SQLiteConnection(DBConnection.MyConnection()))
                {
                    cn.Open();
                    using (var transaction = cn.BeginTransaction())
                    {
                        string sql = @"
                        UPDATE TblProduct1
                        SET qty = qty - IFNULL(
                            (SELECT SUM(qty) FROM tblCart1 
                             WHERE pcode = TblProduct1.pcode 
                             AND transno = @transno 
                             AND status='Pending'),0)
                        WHERE pcode IN (
                            SELECT pcode FROM tblCart1 
                            WHERE transno=@transno AND status='Pending'
                        );

                        UPDATE tblCart1
                        SET status='Sold'
                        WHERE transno=@transno AND status='Pending';
                        ";

                        using (SQLiteCommand cmd = new SQLiteCommand(sql, cn, transaction))
                        {
                            cmd.Parameters.AddWithValue("@transno", transno);
                            cmd.ExecuteNonQuery();
                        }

                        transaction.Commit();
                    }
                }
            }
            catch (Exception ex)
            {
                LogError("FinalizeTransaction", ex);
                MessageBox.Show("Transaction Failed: " + ex.Message, stitle);
            }
        }
        #endregion

        #region BARCODE
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

        private void ProcessBarcode(string code)
        {
            try
            {
                using (SQLiteConnection cn = new SQLiteConnection(DBConnection.MyConnection()))
                {
                    cn.Open();
                    string sql = "SELECT * FROM TblProduct1 WHERE pcode=@code AND isactive=1";

                    using (SQLiteCommand cmd = new SQLiteCommand(sql, cn))
                    {
                        cmd.Parameters.AddWithValue("@code", code);

                        using (SQLiteDataReader dr = cmd.ExecuteReader())
                        {
                            if (dr.Read())
                            {
                                string pcode = dr["pcode"].ToString();
                                double price = Convert.ToDouble(dr["price"]);
                                int stock = Convert.ToInt32(dr["qty"]);

                                // Changed to Show() and assigned owner to allow swapping and remove ding sound
                                frmQty qty = new frmQty(this);
                                qty.ProductDetails(pcode, price, lblTransno.Text, stock);
                                qty.Show(this);

                                LoadCart();
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogError("Barcode", ex);
            }
        }
        #endregion

        #region BUTTONS
        private void btnTrans_Click(object sender, EventArgs e)
        {
            if (dataGridView1.Rows.Count > 0)
            {
                if (MessageBox.Show("Discard current cart?", stitle,
                    MessageBoxButtons.YesNo) == DialogResult.No)
                    return;
            }

            GetTransNo();
            LoadCart();
            txtSearch.Focus();
        }

        private void btnSearch_Click(object sender, EventArgs e)
        {
            // Changed to Show() to allow swapping between forms without dinging
            frmLookUp lookup = new frmLookUp();
            lookup.Show(this);
        }

        private void btnDiscount_Click(object sender, EventArgs e)
        {
            // Changed to Show() to allow swapping between forms
            frmDiscount frm = new frmDiscount(this);
            frm.Show(this);
        }

        private void btnSattle_Click(object sender, EventArgs e)
        {
            if (dataGridView1.Rows.Count == 0) return;

            // Settlement usually needs to stay Modal (ShowDialog) to ensure payment is finished,
            // but if you want to swap, use Show(this). 
            frmSettel frm = new frmSettel(this);
            frm.txtSale.Text = lblDisplayTotal.Text;
            frm.Show(this);
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            frmConfirmPaasword fcp = new frmConfirmPaasword();
            fcp.Show(this);
        }

        private void btnSales_Click(object sender, EventArgs e)
        {
            frmReportSold frm = new frmReportSold(null);
            frm.Show(this);
        }

        private void btnClose_Click(object sender, EventArgs e)
        {
            this.Dispose();
        }
        #endregion

        #region LOAD CART
        public void LoadCart()
        {
            dataGridView1.Rows.Clear();

            decimal total = 0;
            decimal discount = 0;

            try
            {
                using (SQLiteConnection cn = new SQLiteConnection(DBConnection.MyConnection()))
                {
                    cn.Open();

                    string sql = @"SELECT c.*, p.pdesc 
                                   FROM tblCart1 c
                                   INNER JOIN TblProduct1 p ON c.pcode=p.pcode
                                   WHERE c.transno=@transno AND c.status='Pending'";

                    using (SQLiteCommand cmd = new SQLiteCommand(sql, cn))
                    {
                        cmd.Parameters.AddWithValue("@transno", lblTransno.Text);

                        using (SQLiteDataReader dr = cmd.ExecuteReader())
                        {
                            while (dr.Read())
                            {
                                total += Convert.ToDecimal(dr["total"]);
                                discount += Convert.ToDecimal(dr["disc"]);

                                dataGridView1.Rows.Add(
                                    dr["id"],
                                    dr["transno"],
                                    dr["pcode"],
                                    dr["pdesc"],
                                    dr["price"],
                                    dr["qty"],
                                    dr["disc"],
                                    dr["total"]);
                            }
                        }
                    }
                }

                lblDisplayTotal.Text = total.ToString("#,##0.00");
                lblTotal.Text = total.ToString("#,##0.00");
                lblDiscount.Text = discount.ToString("#,##0.00");
            }
            catch (Exception ex)
            {
                LogError("LoadCart", ex);
            }
        }
        #endregion

        #region TRANSNO
        public void GetTransNo()
        {
            try
            {
                string date = DateTime.Now.ToString("yyyyMMdd");

                using (SQLiteConnection cn = new SQLiteConnection(DBConnection.MyConnection()))
                {
                    cn.Open();

                    string sql = @"SELECT transno 
                                   FROM tblTransaction
                                   WHERE transno LIKE @d
                                   ORDER BY transno DESC
                                   LIMIT 1";

                    using (SQLiteCommand cmd = new SQLiteCommand(sql, cn))
                    {
                        cmd.Parameters.AddWithValue("@d", date + "%");

                        object obj = cmd.ExecuteScalar();

                        if (obj != null)
                        {
                            string last = obj.ToString();
                            long num = long.Parse(last.Substring(8)) + 1;
                            lblTransno.Text = date + num.ToString("D4");
                        }
                        else
                            lblTransno.Text = date + "0001";
                    }
                }
            }
            catch
            {
                lblTransno.Text = DateTime.Now.ToString("yyyyMMdd0001");
            }
        }
        #endregion

        #region STORE
        private void LoadStoreInfo()
        {
            try
            {
                using (SQLiteConnection cn = new SQLiteConnection(DBConnection.MyConnection()))
                {
                    cn.Open();
                    using (SQLiteCommand cmd = new SQLiteCommand("SELECT store FROM tblStore LIMIT 1", cn))
                        lblSname.Text = cmd.ExecuteScalar()?.ToString() ?? "ELITE POS";
                }
            }
            catch
            {
                lblSname.Text = "ELITE POS";
            }
        }
        #endregion

        #region SYSTEM
        private void LogError(string ctx, Exception ex)
        {
            if (!Directory.Exists(Path.GetDirectoryName(logFile))) EnsureLogDirectory();
            File.AppendAllText(logFile, $"{DateTime.Now} [{ctx}] {ex}\n");
        }

        private void EnsureLogDirectory()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(logFile));
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            lblDate.Text = DateTime.Now.ToString("MMMM dd, yyyy | hh:mm:ss tt");
        }

        private void frmPOS_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.F1) btnTrans.PerformClick();
            else if (e.KeyCode == Keys.F2) btnSearch.PerformClick();
            else if (e.KeyCode == Keys.F3) btnDiscount.PerformClick();
            else if (e.KeyCode == Keys.F4) btnSattle.PerformClick();
            else if (e.KeyCode == Keys.F5) btnCancel.PerformClick();
            else if (e.KeyCode == Keys.F6) btnSales.PerformClick();
            else if (e.KeyCode == Keys.F10) btnClose.PerformClick();
        }
        #endregion

        #region DESIGNER REQUIRED EVENTS (DO NOT REMOVE)
        private void panel6_Paint(object sender, PaintEventArgs e) { }
        private void label14_Click(object sender, EventArgs e) { }
        private void LblUser_Click(object sender, EventArgs e) { }
        private void lblName_Click(object sender, EventArgs e) { }
        private void label7_Click(object sender, EventArgs e) { }
        private void dataGridView1_CellContentClick(object sender, DataGridViewCellEventArgs e) { }
        private void dataGridView1_SelectionChanged(object sender, EventArgs e) { }
        private void label6_Click(object sender, EventArgs e) { }
        private void label2_Click(object sender, EventArgs e) { }
        private void lblAddress_Click(object sender, EventArgs e) { }
        private void lblSname_Click(object sender, EventArgs e) { }
        private void lblPhone_Click(object sender, EventArgs e) { }
        #endregion

        private void lblTransno_Click(object sender, EventArgs e)
        {

        }

        private void lblDate_Click(object sender, EventArgs e)
        {

        }
    }
}