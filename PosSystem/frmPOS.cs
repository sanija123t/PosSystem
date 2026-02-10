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

        // ✅ REQUIRED FOR DESIGNER
        public frmPOS()
        {
            InitializeComponent();
        }

        // ✅ YOUR ORIGINAL CONSTRUCTOR (UNCHANGED)
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
                                total += Convert.ToDecimal(dr["total"]);
                                dataGridView1.Rows.Add(
                                    dr["id"], dr["transno"], dr["pcode"], dr["pdesc"],
                                    dr["price"], dr["qty"], dr["discount"], dr["total"]
                                );
                            }
                        }
                    }
                }
                UpdateTotals(total);
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
            if (dataGridView1.Rows.Count == 0 ||
                (dataGridView1.Rows.Count == 1 && dataGridView1.Rows[0].IsNewRow))
                return;

            btnSattle.Enabled = false;
            bool success = false;

            try
            {
                printItems = dataGridView1.Rows.Cast<DataGridViewRow>()
                    .Where(r => !r.IsNewRow && r.Cells[2].Value != null)
                    .Select(r =>
                    {
                        decimal rowTotal = Convert.ToDecimal(r.Cells[7].Value);
                        decimal rowVatable = Math.Round(rowTotal / 1.12m, 2);
                        return new ReceiptItem
                        {
                            PCode = r.Cells[2].Value.ToString(),
                            Description = r.Cells[3].Value?.ToString() ?? "Unknown",
                            Qty = Math.Max(1, Convert.ToInt32(r.Cells[5].Value)),
                            Total = rowTotal,
                            Vatable = rowVatable,
                            VatAmount = Math.Round(rowTotal - rowVatable, 2)
                        };
                    }).ToList();

                receiptTotal = printItems.Sum(x => x.Total);

                await Task.Run(() =>
                {
                    using (SQLiteConnection cn = new SQLiteConnection(DBConnection.MyConnection()))
                    {
                        cn.Open();
                        using (SQLiteCommand cmdMode =
                            new SQLiteCommand("PRAGMA journal_mode=WAL;", cn))
                        {
                            cmdMode.ExecuteNonQuery();
                        }

                        using (var transaction = cn.BeginTransaction())
                        {
                            try
                            {
                                foreach (var item in printItems)
                                {
                                    using (SQLiteCommand cm = new SQLiteCommand(
                                        "UPDATE tblProduct1 SET qty = qty - @qty WHERE pcode = @pcode AND qty >= @qty", cn))
                                    {
                                        cm.Parameters.AddWithValue("@qty", item.Qty);
                                        cm.Parameters.AddWithValue("@pcode", item.PCode);

                                        if (cm.ExecuteNonQuery() == 0)
                                            throw new Exception($"Stock error: {item.Description} is out of stock.");
                                    }
                                }

                                using (SQLiteCommand cm = new SQLiteCommand(
                                    "UPDATE tblCart1 SET status = 'Sold' WHERE transno = @transno", cn))
                                {
                                    cm.Parameters.AddWithValue("@transno", lblTransno.Text);
                                    cm.ExecuteNonQuery();
                                }

                                transaction.Commit();
                                success = true;
                            }
                            catch (Exception ex)
                            {
                                transaction.Rollback();
                                LogError("Settle Transaction", ex);
                                this.Invoke(new Action(() =>
                                    MessageBox.Show(ex.Message, stitle,
                                        MessageBoxButtons.OK, MessageBoxIcon.Error)));
                            }
                        }
                    }
                });

                if (success)
                {
                    if (MessageBox.Show("Transaction Complete. Print Receipt?",
                        stitle, MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                    {
                        itemsPrinted = 0;
                        try { pd.Print(); }
                        catch (Exception ex)
                        {
                            LogError("Printer Error", ex);
                            MessageBox.Show("Printer error: " + ex.Message);
                        }
                    }

                    GetTransNo();
                    LoadCart();
                }
            }
            catch (Exception ex) { LogError("Settle", ex); }
            finally { btnSattle.Enabled = true; }
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
                            ShowPopupSafe($"{dr["pdesc"]} has only {dr["qty"]} left!");
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
                    $"{DateTime.Now} [{ctx}] {ex.Message}{Environment.NewLine}");
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
        // ===== DESIGNER REQUIRED EVENT HANDLERS (DO NOT DELETE) =====

        private void label14_Click(object sender, EventArgs e) { }

        private void LblUser_Click(object sender, EventArgs e) { }

        private void lblName_Click(object sender, EventArgs e) { }

        private void label7_Click(object sender, EventArgs e) { }

        private void label2_Click(object sender, EventArgs e) { }

        private void dataGridView1_CellContentClick(object sender, DataGridViewCellEventArgs e) { }

        private void dataGridView1_SelectionChanged(object sender, EventArgs e) { }
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