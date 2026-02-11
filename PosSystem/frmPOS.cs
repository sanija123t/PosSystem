using System;
using System.Data;
using System.Data.SQLite;
using System.Windows.Forms;
using Tulpep.NotificationWindow;
using System.Drawing;
using System.Drawing.Printing;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Threading;

namespace PosSystem
{
    public partial class frmPOS : Form
    {
        string stitle = "POS System v1.0";
        frmUserLogin f;
        PrintDocument pd = new PrintDocument();
        List<ReceiptItem> printItems = new List<ReceiptItem>();

        int itemsPrinted = 0;
        decimal receiptTotal = 0m;

        string logFile = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "PosSystem",
            "pos_error.log"
        );

        public frmPOS()
        {
            InitializeComponent();
        }

        public frmPOS(frmUserLogin frm)
        {
            InitializeComponent();
            f = frm;
            this.KeyPreview = true;
            pd.PrintPage += PrintReceiptPage;

            timer1.Interval = 1000;
            timer1.Enabled = true;

            EnsureLogDirectory();
        }

        private void frmPOS_Load(object sender, EventArgs e)
        {
            LoadStoreInfo();
            GetTransNo();
            LoadCart();
            _ = Task.Run(() => NotifyCriticalItems());
        }

        #region CORE POS LOGIC

        public void LoadCart()
        {
            dataGridView1.Rows.Clear();
            decimal total = 0;
            try
            {
                using (SQLiteConnection cn = new SQLiteConnection(DBConnection.MyConnection()))
                {
                    cn.Open();
                    using (SQLiteCommand cmd = new SQLiteCommand(
                        "SELECT * FROM tblCart1 WHERE transno = @transno AND status = 'Pending'", cn))
                    {
                        cmd.Parameters.AddWithValue("@transno", lblTransno.Text);
                        using (SQLiteDataReader dr = cmd.ExecuteReader())
                        {
                            while (dr.Read())
                            {
                                decimal rowTotal = 0;
                                if (!decimal.TryParse(dr["total"]?.ToString(), out rowTotal))
                                    rowTotal = 0;

                                total += rowTotal;

                                dataGridView1.Rows.Add(
                                    dr["id"], dr["transno"], dr["pcode"], dr["pdesc"],
                                    dr["price"], dr["qty"], dr["discount"], dr["total"]
                                );
                            }
                        }
                    }
                }
                UpdateTotals(total);
                // Auto-scroll to last item for better UX
                if (dataGridView1.Rows.Count > 0)
                    dataGridView1.FirstDisplayedScrollingRowIndex = dataGridView1.Rows.Count - 1;
            }
            catch (Exception ex) { LogError("LoadCart", ex); }
        }

        private void UpdateTotals(decimal total)
        {
            lblTotal.Text = total.ToString("#,##0.00");
            lblDisplayTotal.Text = total.ToString("#,##0.00");

            decimal vatable = Math.Round(total / 1.12m, 2);
            decimal vat = Math.Round(total - vatable, 2);

            lblVatable.Text = vatable.ToString("#,##0.00");
            lblVat.Text = vat.ToString("#,##0.00");

            receiptTotal = total;
        }

        public async void SettlePayment()
        {
            if (dataGridView1.Rows.Count == 0 || (dataGridView1.Rows.Count == 1 && dataGridView1.Rows[0].IsNewRow))
                return;

            btnSattle.Enabled = false;
            bool success = false;

            // Collect items safely
            printItems = new List<ReceiptItem>();
            foreach (DataGridViewRow r in dataGridView1.Rows)
            {
                if (r.IsNewRow || r.Cells[2].Value == null) continue;

                decimal rowTotal = 0m;
                int rowQty = 1;

                if (!decimal.TryParse(r.Cells[7].Value?.ToString(), out rowTotal)) continue;
                if (!int.TryParse(r.Cells[5].Value?.ToString(), out rowQty)) rowQty = 1;
                rowQty = Math.Max(1, rowQty);

                decimal rowVatable = Math.Round(rowTotal / 1.12m, 2);

                printItems.Add(new ReceiptItem
                {
                    PCode = r.Cells[2].Value.ToString(),
                    Description = r.Cells[3].Value?.ToString() ?? "Unknown",
                    Qty = rowQty,
                    Total = rowTotal,
                    Vatable = rowVatable,
                    VatAmount = Math.Round(rowTotal - rowVatable, 2)
                });
            }

            if (printItems.Count == 0)
            {
                btnSattle.Enabled = true;
                return;
            }

            receiptTotal = printItems.Sum(x => x.Total);

            await Task.Run(() =>
            {
                try
                {
                    using (SQLiteConnection cn = new SQLiteConnection(DBConnection.MyConnection()))
                    {
                        cn.Open();
                        using (SQLiteCommand cmdMode = new SQLiteCommand("PRAGMA journal_mode=WAL;", cn))
                            cmdMode.ExecuteNonQuery();

                        using (var transaction = cn.BeginTransaction())
                        {
                            try
                            {
                                foreach (var item in printItems)
                                {
                                    // Check current stock first
                                    int currentQty = 0;
                                    using (SQLiteCommand cmCheck = new SQLiteCommand(
                                        "SELECT qty FROM tblProduct1 WHERE pcode = @pcode", cn))
                                    {
                                        cmCheck.Parameters.AddWithValue("@pcode", item.PCode);
                                        object res = cmCheck.ExecuteScalar();
                                        currentQty = res != null ? Convert.ToInt32(res) : 0;
                                    }

                                    if (currentQty < item.Qty)
                                        throw new Exception($"Stock error: {item.Description} has only {currentQty} left.");

                                    // Update stock safely
                                    using (SQLiteCommand cmUpdate = new SQLiteCommand(
                                        "UPDATE tblProduct1 SET qty = qty - @qty WHERE pcode = @pcode", cn))
                                    {
                                        cmUpdate.Parameters.AddWithValue("@qty", item.Qty);
                                        cmUpdate.Parameters.AddWithValue("@pcode", item.PCode);
                                        cmUpdate.ExecuteNonQuery();
                                    }
                                }

                                using (SQLiteCommand cmCart = new SQLiteCommand(
                                    "UPDATE tblCart1 SET status = 'Sold' WHERE transno = @transno", cn))
                                {
                                    cmCart.Parameters.AddWithValue("@transno", lblTransno.Text);
                                    cmCart.ExecuteNonQuery();
                                }

                                transaction.Commit();
                                success = true;
                            }
                            catch (Exception ex)
                            {
                                transaction.Rollback();
                                LogError("Settle Transaction", ex);

                                if (this.IsHandleCreated)
                                {
                                    this.Invoke(new Action(() =>
                                        MessageBox.Show(ex.ToString(), stitle, MessageBoxButtons.OK, MessageBoxIcon.Error)));
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    LogError("Settle Transaction", ex);
                }
            });

            if (success)
            {
                if (this.IsHandleCreated && MessageBox.Show(
                    "Transaction Complete. Print Receipt?", stitle,
                    MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    itemsPrinted = 0;
                    try { pd.Print(); }
                    catch (Exception ex)
                    {
                        LogError("Printer Error", ex);
                        if (this.IsHandleCreated)
                            MessageBox.Show("Printer error: " + ex.Message, stitle, MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }

                GetTransNo();
                LoadCart();

                // Reset focus for faster workflow
                if (dataGridView1.Rows.Count > 0)
                    dataGridView1.Focus();
            }

            btnSattle.Enabled = true;
        }

        #endregion

        #region PRINTING

        private void PrintReceiptPage(object sender, PrintPageEventArgs e)
        {
            int y = 10;
            int pageHeight = e.MarginBounds.Height;

            using (Font header = new Font("Courier New", 12, FontStyle.Bold))
            using (Font body = new Font("Courier New", 10))
            {
                string storeName = string.IsNullOrWhiteSpace(lblSname.Text) ? "My Store" : lblSname.Text;
                string storeAddress = string.IsNullOrWhiteSpace(lblAddress.Text) ? "Address" : lblAddress.Text;

                e.Graphics.DrawString(storeName, header, Brushes.Black, 10, y); y += 20;
                e.Graphics.DrawString(storeAddress, body, Brushes.Black, 10, y); y += 20;
                e.Graphics.DrawString("Trans #: " + lblTransno.Text, body, Brushes.Black, 10, y); y += 25;

                int itemsPerPage = (pageHeight - y - 60) / 20;
                int count = Math.Min(itemsPerPage, printItems.Count - itemsPrinted);

                for (int i = 0; i < count; i++)
                {
                    var it = printItems[itemsPrinted + i];
                    string desc = it.Description.Length > 15 ? it.Description.Substring(0, 15) : it.Description;
                    e.Graphics.DrawString($"{desc} x{it.Qty} {it.Total:N2}", body, Brushes.Black, 10, y);
                    y += 20;
                }

                itemsPrinted += count;
                e.HasMorePages = itemsPrinted < printItems.Count;
            }
        }

        #endregion

        #region HELPERS

        private void LoadStoreInfo()
        {
            try
            {
                using (SQLiteConnection cn = new SQLiteConnection(DBConnection.MyConnection()))
                {
                    cn.Open();
                    using (SQLiteCommand cmd = new SQLiteCommand("SELECT * FROM tblStore LIMIT 1", cn))
                    using (SQLiteDataReader dr = cmd.ExecuteReader())
                    {
                        if (dr.Read())
                        {
                            lblSname.Text = dr["store"].ToString();
                            lblAddress.Text = dr["address"].ToString();
                            lblPhone.Text = dr["phone"].ToString();
                        }
                    }
                }
            }
            catch (Exception ex) { LogError("LoadStoreInfo", ex); }
        }

        public void GetTransNo()
        {
            try
            {
                using (SQLiteConnection cn = new SQLiteConnection(DBConnection.MyConnection()))
                {
                    cn.Open();
                    using (SQLiteCommand cmd = new SQLiteCommand(
                        "SELECT IFNULL(MAX(id),0)+1 FROM tblCart1", cn))
                    {
                        lblTransno.Text =
                            DateTime.Now.ToString("yyyyMMdd") +
                            Convert.ToInt32(cmd.ExecuteScalar()).ToString("D4");
                    }
                }
            }
            catch { lblTransno.Text = DateTime.Now.ToString("yyyyMMdd0001"); }
        }

        private void NotifyCriticalItems()
        {
            try
            {
                using (SQLiteConnection cn = new SQLiteConnection(DBConnection.MyConnection()))
                {
                    cn.Open();
                    using (SQLiteCommand cmd = new SQLiteCommand("SELECT * FROM vwCriticalItems", cn))
                    using (SQLiteDataReader dr = cmd.ExecuteReader())
                    {
                        while (dr.Read())
                        {
                            ShowPopupSafe($"{dr["pdesc"]} has only {dr["qty"]} left!");
                            Thread.Sleep(200); // small delay to batch notifications
                        }
                    }
                }
            }
            catch { }
        }

        private void ShowPopupSafe(string msg)
        {
            if (InvokeRequired) BeginInvoke(new Action(() => ShowPopup(msg)));
            else ShowPopup(msg);
        }

        private void ShowPopup(string msg)
        {
            new PopupNotifier
            {
                TitleText = "Low Stock Alert",
                ContentText = msg,
                Delay = 5000
            }.Popup();
        }

        private void LogError(string ctx, Exception ex)
        {
            try
            {
                File.AppendAllText(logFile,
                    $"{DateTime.Now} [{ctx}] {ex.ToString()}{Environment.NewLine}");
            }
            catch { }
        }

        private void EnsureLogDirectory()
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(logFile));
            }
            catch { }
        }

        #endregion

        #region DESIGNER EVENTS

        private void timer1_Tick(object sender, EventArgs e)
        {
            lblDate.Text = DateTime.Now.ToString("MMMM dd, yyyy | hh:mm:ss tt");
        }

        private void btnClose_Click(object sender, EventArgs e) => Dispose();
        private void btnSattle_Click(object sender, EventArgs e) => SettlePayment();

        #endregion

        private void label14_Click(object sender, EventArgs e) { }
        private void LblUser_Click(object sender, EventArgs e) { }
        private void lblName_Click(object sender, EventArgs e) { }
        private void label7_Click(object sender, EventArgs e) { }
        private void label2_Click(object sender, EventArgs e) { }
        private void dataGridView1_CellContentClick(object sender, DataGridViewCellEventArgs e) { }
        private void dataGridView1_SelectionChanged(object sender, EventArgs e) { }
        private void lblSname_Click(object sender, EventArgs e) { }

        private void btnSearch_Click(object sender, EventArgs e)
        {

        }

        private void btnTrans_Click(object sender, EventArgs e)
        {

        }

        private void btnDiscount_Click(object sender, EventArgs e)
        {

        }

        private void btnCancel_Click(object sender, EventArgs e)
        {

        }

        private void btnSales_Click(object sender, EventArgs e)
        {

        }

        private void label6_Click(object sender, EventArgs e)
        {

        }
    }

    public class ReceiptItem
    {
        public string PCode { get; set; }
        public string Description { get; set; }
        public int Qty { get; set; }
        public decimal Total { get; set; }
        public decimal Vatable { get; set; }
        public decimal VatAmount { get; set; }
    }
}