using System;
using System.Data;
using System.Data.SQLite;
using System.IO;
using System.Drawing;
using System.Drawing.Printing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace PosSystem
{
    public partial class frmPOS : Form
    {
        #region WINAPI FOR DRAGGING
        [DllImport("user32.dll")] private static extern bool ReleaseCapture();
        [DllImport("user32.dll")] private static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);
        private const int WM_NCLBUTTONDOWN = 0xA1;
        private const int HT_CAPTION = 0x2;

        private void panel1_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left) { ReleaseCapture(); SendMessage(Handle, WM_NCLBUTTONDOWN, HT_CAPTION, 0); }
        }
        #endregion

        string stitle = "POS System v1.0 - ELITE";
        Form1 f;
        bool isTransactionStarted = false;
        string logFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "PosSystem", "pos_error.log");
        private string lastPrintedTransNo = "";

        public frmPOS(Form1 frm)
        {
            InitializeComponent();
            f = frm;
            this.KeyPreview = true;
            timer1.Start();
            LoadPrinterList();
            ConfigureGrid();
        }

        private void frmPOS_Load(object sender, EventArgs e)
        {
            LoadStoreInfo();
            lblTransno.Text = "000000000000";
            lblDate.Text = DateTime.Now.ToShortDateString();

            if (f != null)
            {
                LblUser.Text = f._user;
                lblName.Text = f._name;
            }
        }

        private void ConfigureGrid()
        {
            System.Reflection.PropertyInfo propertyInfo = typeof(DataGridView).GetProperty("DoubleBuffered", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            propertyInfo.SetValue(dataGridView1, true, null);

            if (dataGridView1.Columns.Contains("Column2")) dataGridView1.Columns["Column2"].Visible = false;
            if (dataGridView1.Columns.Contains("Column8")) dataGridView1.Columns["Column8"].Visible = false;
            if (dataGridView1.Columns.Contains("Column1")) dataGridView1.Columns["Column1"].Width = 40;
            if (dataGridView1.Columns.Contains("Column3")) dataGridView1.Columns["Column3"].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;

            dataGridView1.ScrollBars = ScrollBars.Both;
        }

        private void LoadPrinterList()
        {
            comboBoxprinter.Items.Clear();
            comboBoxprinter.Items.Add("Auto");
            comboBoxprinter.Items.Add("80mm Thermal");
            comboBoxprinter.Items.Add("58mm Thermal");

            var installedPrinters = PrinterSettings.InstalledPrinters.Cast<string>().ToList();
            foreach (string printer in installedPrinters)
                comboBoxprinter.Items.Add(printer);

            string savedPrinter = "Auto";
            try { savedPrinter = Properties.Settings.Default.DefaultPrinter; } catch { }

            comboBoxprinter.Text = (!string.IsNullOrEmpty(savedPrinter) && (savedPrinter == "Auto" || installedPrinters.Contains(savedPrinter)))
                                   ? savedPrinter : "Auto";
        }

        #region CORE LOGIC
        private bool CheckTransaction()
        {
            if (!isTransactionStarted || string.IsNullOrWhiteSpace(lblTransno.Text) || lblTransno.Text.Length != 12)
            {
                MessageBox.Show("Please click 'New Transaction' [F1] first!", stitle, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }
            return true;
        }

        public void GetTransNo()
        {
            try
            {
                string date = DateTime.Now.ToString("yyyyMMdd");
                using (SQLiteConnection cn = new SQLiteConnection(DBConnection.MyConnection()))
                {
                    cn.Open();
                    string sql = "SELECT transno FROM tblTransaction WHERE transno LIKE @date ORDER BY id DESC LIMIT 1";
                    using (SQLiteCommand cmd = new SQLiteCommand(sql, cn))
                    {
                        cmd.Parameters.AddWithValue("@date", date + "%");
                        object obj = cmd.ExecuteScalar();

                        if (obj != null && obj.ToString().Length >= 12)
                        {
                            string lastNum = obj.ToString().Substring(8);
                            if (!long.TryParse(lastNum, out long num)) num = 0;
                            lblTransno.Text = date + (num + 1).ToString("D4");
                        }
                        else
                        {
                            lblTransno.Text = date + "0001";
                        }
                    }
                }
                isTransactionStarted = true;
                lblDate.Text = DateTime.Now.ToShortDateString();
            }
            catch (Exception ex) { LogError("GetTransNo", ex); }
        }

        public void LoadCart()
        {
            dataGridView1.Rows.Clear();
            double total = 0, discount = 0;
            int itemsCount = 0;

            try
            {
                using (SQLiteConnection cn = new SQLiteConnection(DBConnection.MyConnection()))
                {
                    cn.Open();
                    string sql = "SELECT c.*, p.pdesc FROM tblCart1 c JOIN TblProduct1 p ON c.pcode=p.pcode WHERE transno=@t AND status='Pending'";
                    using (SQLiteCommand cmd = new SQLiteCommand(sql, cn))
                    {
                        cmd.Parameters.AddWithValue("@t", lblTransno.Text);
                        using (SQLiteDataReader dr = cmd.ExecuteReader())
                        {
                            while (dr.Read())
                            {
                                itemsCount++;
                                total += Convert.ToDouble(dr["total"]);
                                discount += Convert.ToDouble(dr["disc"]);
                                dataGridView1.Rows.Add(itemsCount, dr["id"], dr["pcode"], dr["pdesc"], dr["qty"], dr["price"], dr["disc"], dr["total"]);
                            }
                        }
                    }
                }
                lblTotal.Text = total.ToString("#,##0.00");
                lblDiscount.Text = discount.ToString("#,##0.00");
                double vat = (total / 1.12) * 0.12;
                lblVat.Text = vat.ToString("#,##0.00");
                lblVatable.Text = (total - vat).ToString("#,##0.00");
                lblDisplayTotal.Text = total.ToString("#,##0.00");
                label6.Text = "Items In Cart: " + itemsCount;
            }
            catch (Exception ex) { LogError("LoadCart", ex); }
        }

        private void ExecuteCartAction(string action, string pcode, string cartId = "")
        {
            using (SQLiteConnection cn = new SQLiteConnection(DBConnection.MyConnection()))
            {
                cn.Open();
                using (SQLiteTransaction transaction = cn.BeginTransaction())
                {
                    try
                    {
                        if (action == "ADD_NEW" || action == "INCREMENT")
                        {
                            int stock = 0;
                            using (SQLiteCommand cmd = new SQLiteCommand("SELECT qty FROM TblProduct1 WHERE pcode=@p", cn))
                            {
                                cmd.Parameters.AddWithValue("@p", pcode);
                                stock = Convert.ToInt32(cmd.ExecuteScalar());
                            }

                            int currentInCart = 0;
                            if (!string.IsNullOrEmpty(cartId))
                            {
                                using (SQLiteCommand cmd = new SQLiteCommand("SELECT qty FROM tblCart1 WHERE id=@id", cn))
                                {
                                    cmd.Parameters.AddWithValue("@id", cartId);
                                    currentInCart = Convert.ToInt32(cmd.ExecuteScalar());
                                }
                            }

                            if (currentInCart >= stock)
                            {
                                MessageBox.Show($"Stock limit reached! Only {stock} items available.", stitle, MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                                return;
                            }

                            string sql = string.IsNullOrEmpty(cartId)
                                ? "INSERT INTO tblCart1 (transno, pcode, price, qty, sdate, status) SELECT @tn, pcode, price, 1, @dt, 'Pending' FROM TblProduct1 WHERE pcode=@pc"
                                : "UPDATE tblCart1 SET qty = qty + 1, total = (qty + 1) * price WHERE id=@id";

                            using (SQLiteCommand cmd = new SQLiteCommand(sql, cn))
                            {
                                cmd.Parameters.AddWithValue("@tn", lblTransno.Text);
                                cmd.Parameters.AddWithValue("@pc", pcode);
                                cmd.Parameters.AddWithValue("@id", cartId);
                                cmd.Parameters.AddWithValue("@dt", DateTime.Now.ToString("yyyy-MM-dd"));
                                cmd.ExecuteNonQuery();
                            }
                        }
                        else if (action == "DECREMENT")
                        {
                            using (SQLiteCommand cmd = new SQLiteCommand("UPDATE tblCart1 SET qty = qty - 1, total = (qty - 1) * price WHERE id=@id AND qty > 1", cn))
                            {
                                cmd.Parameters.AddWithValue("@id", cartId);
                                if (cmd.ExecuteNonQuery() == 0)
                                {
                                    using (SQLiteCommand delCmd = new SQLiteCommand("DELETE FROM tblCart1 WHERE id=@id", cn))
                                    {
                                        delCmd.Parameters.AddWithValue("@id", cartId);
                                        delCmd.ExecuteNonQuery();
                                    }
                                }
                            }
                        }
                        else if (action == "DELETE")
                        {
                            using (SQLiteCommand cmd = new SQLiteCommand("DELETE FROM tblCart1 WHERE id=@id", cn))
                            {
                                cmd.Parameters.AddWithValue("@id", cartId);
                                cmd.ExecuteNonQuery();
                            }
                        }

                        transaction.Commit();
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        LogError("ExecuteCartAction:" + action, ex);
                        throw;
                    }
                }
            }
            LoadCart();
        }

        public void AddToCart(string pcode)
        {
            if (!CheckTransaction()) return;
            try
            {
                using (SQLiteConnection cn = new SQLiteConnection(DBConnection.MyConnection()))
                {
                    cn.Open();
                    string foundPcode = "";
                    using (SQLiteCommand cmd = new SQLiteCommand("SELECT pcode FROM TblProduct1 WHERE (pcode=@p OR barcode=@p) AND isactive=1", cn))
                    {
                        cmd.Parameters.AddWithValue("@p", pcode);
                        foundPcode = cmd.ExecuteScalar()?.ToString();
                    }

                    if (string.IsNullOrEmpty(foundPcode))
                    {
                        MessageBox.Show("Item not found!", stitle, MessageBoxButtons.OK, MessageBoxIcon.Information);
                        return;
                    }

                    string cartId = "";
                    using (SQLiteCommand cmd = new SQLiteCommand("SELECT id FROM tblCart1 WHERE pcode=@pc AND transno=@tn AND status='Pending'", cn))
                    {
                        cmd.Parameters.AddWithValue("@pc", foundPcode);
                        cmd.Parameters.AddWithValue("@tn", lblTransno.Text);
                        cartId = cmd.ExecuteScalar()?.ToString() ?? "";
                    }

                    ExecuteCartAction(string.IsNullOrEmpty(cartId) ? "ADD_NEW" : "INCREMENT", foundPcode, cartId);
                }
            }
            catch (Exception ex) { LogError("AddToCart", ex); }
        }
        #endregion

        #region INTERFACE EVENTS
        private void frmPOS_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.F1) btnTrans.PerformClick();
            else if (e.KeyCode == Keys.F2) { if (CheckTransaction()) btnSearch.PerformClick(); }
            else if (e.KeyCode == Keys.F3) { if (CheckTransaction()) btnDiscount.PerformClick(); }
            else if (e.KeyCode == Keys.F4 || e.KeyCode == Keys.Enter) btnSattle.PerformClick();
            else if (e.KeyCode == Keys.F5) PrintThermalBill(lastPrintedTransNo);
            else if (e.KeyCode == Keys.F6) btnSales.PerformClick();
            else if (e.KeyCode == Keys.F8) btnscanbarcode.PerformClick();
            else if (e.KeyCode == Keys.F10) btnClose.PerformClick();
        }

        private void btnTrans_Click(object sender, EventArgs e)
        {
            GetTransNo();
            LoadCart();
            textBoxbarcode.Focus();
        }

        private void btnSattle_Click(object sender, EventArgs e)
        {
            if (!CheckTransaction() || dataGridView1.Rows.Count == 0) return;
            frmSettel frm = new frmSettel(this);
            frm.txtSale.Text = lblDisplayTotal.Text;
            if (frm.ShowDialog() == DialogResult.OK)
            {
                lastPrintedTransNo = lblTransno.Text;
                KickDrawer();
                PrintThermalBill(lastPrintedTransNo);
            }
        }

        private void dataGridView1_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
            string colName = dataGridView1.Columns[e.ColumnIndex].Name;
            string id = dataGridView1.Rows[e.RowIndex].Cells[1].Value?.ToString();
            string pcode = dataGridView1.Rows[e.RowIndex].Cells[2].Value?.ToString();
            if (id == null || pcode == null) return;

            if (colName == "colAdd") ExecuteCartAction("INCREMENT", pcode, id);
            else if (colName == "colRemove") ExecuteCartAction("DECREMENT", pcode, id);
            else if (colName == "colDelete")
            {
                if (MessageBox.Show("Remove this item?", stitle, MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                    ExecuteCartAction("DELETE", pcode, id);
            }
        }

        private void btnSalesHistory_Click(object sender, EventArgs e)
        {
            frmSoldItems frm = new frmSoldItems();
            try
            {
                frm.GetType().GetProperty("dt1")?.SetValue(frm, DateTime.Now);
                frm.GetType().GetProperty("dt2")?.SetValue(frm, DateTime.Now);
                frm.GetType().GetField("_user")?.SetValue(frm, LblUser.Text);
            }
            catch { }
            frm.ShowDialog();
        }
        #endregion

        #region MISSING HANDLERS (Fixes CS1061 Designer Errors)
        private void button1_Click(object sender, EventArgs e)
        {
            if (!CheckTransaction()) return;
            AddToCart(textBoxbarcode.Text.Trim());
            textBoxbarcode.Clear();
            textBoxbarcode.Focus();
        }
        private void btnCancel_Click(object sender, EventArgs e) { if (MessageBox.Show("Clear cart?", stitle, MessageBoxButtons.YesNo) == DialogResult.Yes) { /* Clear logic */ } }
        private void btnDiscount_Click(object sender, EventArgs e) { /* Add discount logic */ }
        private void btnSales_Click(object sender, EventArgs e) { btnSalesHistory_Click(sender, e); }
        private void btnscanbarcode_Click(object sender, EventArgs e) { textBoxbarcode.Focus(); }
        private void btnSearch_Click(object sender, EventArgs e) { /* Search Logic */ }
        private void dataGridView1_SelectionChanged(object sender, EventArgs e) { }
        private void label14_Click(object sender, EventArgs e) { }
        private void label2_Click(object sender, EventArgs e) { }
        private void label6_Click(object sender, EventArgs e) { }
        private void label7_Click(object sender, EventArgs e) { }
        private void lblAddress_Click(object sender, EventArgs e) { }
        private void lblDate_Click(object sender, EventArgs e) { }
        private void lblDiscount_Click(object sender, EventArgs e) { }
        private void lblName_Click(object sender, EventArgs e) { }
        private void lblPhone_Click(object sender, EventArgs e) { }
        private void lblSname_Click(object sender, EventArgs e) { }
        private void lblTotal_Click(object sender, EventArgs e) { }
        private void lblTransno_Click(object sender, EventArgs e) { }
        private void LblUser_Click(object sender, EventArgs e) { }
        private void lblVatable_Click(object sender, EventArgs e) { }
        private void lblVat_Click(object sender, EventArgs e) { }
        private void panel6_Paint(object sender, PaintEventArgs e) { }
        private void textBoxbarcode_TextChanged(object sender, EventArgs e)
        {
            if (textBoxbarcode.Text.Length > 0 && textBoxbarcode.Text.EndsWith("\n")) AddToCart(textBoxbarcode.Text.Trim());
        }
        #endregion

        #region PRINTING
        private void PrintThermalBill(string transNo)
        {
            if (string.IsNullOrEmpty(transNo)) return;
            PrintDocument pd = new PrintDocument();
            string pName = comboBoxprinter.Text == "Auto" ? new PrinterSettings().PrinterName : comboBoxprinter.Text;
            pd.PrinterSettings.PrinterName = pName;

            pd.PrintPage += (s, ev) => {
                Graphics g = ev.Graphics;
                float paperWidth = (comboBoxprinter.Text == "58mm Thermal") ? 200 : 280;
                Font fRegular = new Font("Courier New", 9);
                Font fBold = new Font("Courier New", 10, FontStyle.Bold);
                float y = 10;
                StringFormat sCenter = new StringFormat { Alignment = StringAlignment.Center };
                StringFormat sRight = new StringFormat { Alignment = StringAlignment.Far };

                g.DrawString(lblSname.Text, fBold, Brushes.Black, new RectangleF(0, y, paperWidth, 20), sCenter);
                y += 20;
                g.DrawString("TRANS: " + transNo, fRegular, Brushes.Black, 5, y); y += 15;
                g.DrawString(new string('-', 30), fRegular, Brushes.Black, 5, y); y += 15;

                using (SQLiteConnection cn = new SQLiteConnection(DBConnection.MyConnection()))
                {
                    cn.Open();
                    using (SQLiteCommand cmd = new SQLiteCommand("SELECT c.*, p.pdesc FROM tblCart1 c JOIN TblProduct1 p ON c.pcode=p.pcode WHERE transno=@t", cn))
                    {
                        cmd.Parameters.AddWithValue("@t", transNo);
                        using (SQLiteDataReader dr = cmd.ExecuteReader())
                        {
                            while (dr.Read())
                            {
                                g.DrawString(dr["pdesc"].ToString(), fRegular, Brushes.Black, 5, y); y += 15;
                                g.DrawString($"{dr["qty"]} x {dr["price"]}", fRegular, Brushes.Black, 15, y);
                                g.DrawString(Convert.ToDouble(dr["total"]).ToString("N2"), fRegular, Brushes.Black, paperWidth - 5, y, sRight); y += 18;
                            }
                        }
                    }
                }
                y += 10;
                g.DrawString("TOTAL: " + lblDisplayTotal.Text, fBold, Brushes.Black, paperWidth - 5, y, sRight);
            };

            try { pd.Print(); } catch (Exception ex) { LogError("Print", ex); }
        }

        public void KickDrawer()
        {
            try
            {
                string pName = comboBoxprinter.Text == "Auto" ? new PrinterSettings().PrinterName : comboBoxprinter.Text;
                byte[] code = new byte[] { 27, 112, 0, 25, 250 };
                RawPrinterHelper.SendBytesToPrinter(pName, code);
            }
            catch (Exception ex) { LogError("KickDrawer", ex); }
        }
        #endregion

        #region UTILS
        private void timer1_Tick(object sender, EventArgs e) { label2.Text = DateTime.Now.ToString("hh:mm:ss tt"); }
        private void btnClose_Click(object sender, EventArgs e) => this.Close();

        private void LoadStoreInfo()
        {
            try
            {
                using (SQLiteConnection cn = new SQLiteConnection(DBConnection.MyConnection()))
                {
                    cn.Open();
                    lblSname.Text = new SQLiteCommand("SELECT store FROM tblStore LIMIT 1", cn).ExecuteScalar()?.ToString() ?? "ELITE POS";
                }
            }
            catch { lblSname.Text = "ELITE POS"; }
        }

        private void LogError(string ctx, Exception ex)
        {
            try
            {
                string dir = Path.GetDirectoryName(logFile);
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
                File.AppendAllText(logFile, $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} [{ctx}] {ex.Message}{Environment.NewLine}");
            }
            catch { }
        }

        private void comboBoxprinter_SelectedIndexChanged(object sender, EventArgs e)
        {
            try
            {
                Properties.Settings.Default.DefaultPrinter = comboBoxprinter.Text;
                Properties.Settings.Default.Save();
            }
            catch { }
        }
        #endregion
    }

    public class RawPrinterHelper
    {
        [DllImport("winspool.Drv", EntryPoint = "OpenPrinterA", SetLastError = true, CharSet = CharSet.Ansi, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
        public static extern bool OpenPrinter([MarshalAs(UnmanagedType.LPStr)] string szPrinter, out IntPtr hPrinter, IntPtr pd);

        [DllImport("winspool.Drv", EntryPoint = "ClosePrinter", SetLastError = true, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
        public static extern bool ClosePrinter(IntPtr hPrinter);

        [DllImport("winspool.Drv", EntryPoint = "StartDocPrinterA", SetLastError = true, CharSet = CharSet.Ansi, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
        public static extern bool StartDocPrinter(IntPtr hPrinter, Int32 level, [In, MarshalAs(UnmanagedType.LPStruct)] DOCINFOA di);

        [DllImport("winspool.Drv", EntryPoint = "EndDocPrinter", SetLastError = true, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
        public static extern bool EndDocPrinter(IntPtr hPrinter);

        [DllImport("winspool.Drv", EntryPoint = "StartPagePrinter", SetLastError = true, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
        public static extern bool StartPagePrinter(IntPtr hPrinter);

        [DllImport("winspool.Drv", EntryPoint = "EndPagePrinter", SetLastError = true, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
        public static extern bool EndPagePrinter(IntPtr hPrinter);

        [DllImport("winspool.Drv", EntryPoint = "WritePrinter", SetLastError = true, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
        public static extern bool WritePrinter(IntPtr hPrinter, IntPtr pBytes, Int32 dwCount, out Int32 dwWritten);

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
        public class DOCINFOA
        {
            [MarshalAs(UnmanagedType.LPStr)] public string pDocName;
            [MarshalAs(UnmanagedType.LPStr)] public string pOutputFile;
            [MarshalAs(UnmanagedType.LPStr)] public string pDatatype;
        }

        public static bool SendBytesToPrinter(string szPrinterName, byte[] pBytes)
        {
            IntPtr hPrinter = new IntPtr(0);
            DOCINFOA di = new DOCINFOA();
            bool bSuccess = false;
            di.pDocName = "POS Receipt";
            di.pDatatype = "RAW";

            if (OpenPrinter(szPrinterName.Normalize(), out hPrinter, IntPtr.Zero))
            {
                if (StartDocPrinter(hPrinter, 1, di))
                {
                    if (StartPagePrinter(hPrinter))
                    {
                        IntPtr pUnmanagedBytes = Marshal.AllocCoTaskMem(pBytes.Length);
                        Marshal.Copy(pBytes, 0, pUnmanagedBytes, pBytes.Length);
                        bSuccess = WritePrinter(hPrinter, pUnmanagedBytes, pBytes.Length, out _);
                        EndPagePrinter(hPrinter);
                        Marshal.FreeCoTaskMem(pUnmanagedBytes);
                    }
                    EndDocPrinter(hPrinter);
                }
                ClosePrinter(hPrinter);
            }
            return bSuccess;
        }
    }
}