using System;
using System.Data.SQLite;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.Threading;
using System.Drawing;

namespace PosSystem
{
    public partial class frmAdjustment : Form
    {
        #region Private Members & Win32 Elite UI
        private Form1 _f;
        public string suser;
        private CancellationTokenSource searchCTS;

        // Pagination Members
        private int currentPage = 0;
        private const int pageSize = 50;
        private int totalRecords = 0; // ELITE: Track total for label

        [DllImport("user32.dll")]
        private static extern bool LockWindowUpdate(IntPtr hWndLock);

        [DllImport("user32.dll")]
        public static extern bool ReleaseCapture();

        [DllImport("user32.dll")]
        public static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);

        private const int WM_NCLBUTTONDOWN = 0xA1;
        private const int HT_CAPTION = 0x2;
        private readonly string _dbPath = DBConnection.MyConnection();
        #endregion

        public frmAdjustment()
        {
            InitializeComponent();
            SetupEliteUI();
            SetupPaginationButtons();
        }

        public frmAdjustment(Form1 frm) : this()
        {
            _f = frm;
            suser = frm._user;
        }

        private void SetupEliteUI()
        {
            txtSearch.TextChanged += TxtSearch_TextChanged;
            panel1.MouseDown += panel1_MouseDown;

            // Double buffering for buttery smooth scrolling
            typeof(DataGridView).GetProperty("DoubleBuffered",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(dataGridView1, true, null);
        }

        private void SetupPaginationButtons()
        {
            btnPrePage.Size = new Size(20, 20);
            btnNextPage.Size = new Size(20, 20);
            btnPrePage.Cursor = Cursors.Hand;
            btnNextPage.Cursor = Cursors.Hand;

            btnPrePage.Click += async (s, e) => {
                if (currentPage > 0)
                {
                    currentPage--;
                    await LoadRecordsAsync();
                }
            };
            btnNextPage.Click += async (s, e) => {
                // ELITE: Only go next if there are more records to show
                if ((currentPage + 1) * pageSize < totalRecords)
                {
                    currentPage++;
                    await LoadRecordsAsync();
                }
            };

            btnPrePage.Paint += (s, e) => DrawArrow(e.Graphics, false, currentPage > 0);
            btnNextPage.Paint += (s, e) => DrawArrow(e.Graphics, true, (currentPage + 1) * pageSize < totalRecords);
        }

        private void DrawArrow(Graphics g, bool right, bool enabled)
        {
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            using (Brush b = new SolidBrush(enabled ? Color.FromArgb(20, 158, 132) : Color.LightGray))
            {
                Point[] points = right ?
                    new Point[] { new Point(6, 4), new Point(14, 10), new Point(6, 16) } :
                    new Point[] { new Point(14, 4), new Point(6, 10), new Point(14, 16) };
                g.FillPolygon(b, points);
            }
        }

        private void frmAdjustment_Load(object sender, EventArgs e)
        {
            txtUser.Text = suser;
            referenceNo();
            _ = LoadRecordsAsync();
        }

        public void referenceNo()
        {
            txtRef.Text = $"ADJ-{DateTime.Now:yyyyMMdd}-{Guid.NewGuid().ToString("N").Substring(0, 5).ToUpper()}";
        }

        #region High-Performance Pagination & Auto-Search
        private async void TxtSearch_TextChanged(object sender, EventArgs e)
        {
            searchCTS?.Cancel();
            searchCTS = new CancellationTokenSource();
            var token = searchCTS.Token;

            try
            {
                await Task.Delay(300, token);
                currentPage = 0;
                await LoadRecordsAsync();
            }
            catch (TaskCanceledException) { }
        }

        public async Task LoadRecordsAsync()
        {
            if (IsDisposed) return;

            try
            {
                LockWindowUpdate(dataGridView1.Handle);
                dataGridView1.Rows.Clear();

                string searchText = $"%{txtSearch.Text.Trim()}%";
                int offset = currentPage * pageSize;

                using (var cn = new SQLiteConnection(_dbPath))
                {
                    await cn.OpenAsync();

                    // ELITE: Get Total Count for UI control in the same session
                    using (var cmdCount = new SQLiteCommand("SELECT COUNT(*) FROM TblProduct1 WHERE (pdesc LIKE @search OR pcode LIKE @search) AND isactive=1", cn))
                    {
                        cmdCount.Parameters.AddWithValue("@search", searchText);
                        totalRecords = Convert.ToInt32(await cmdCount.ExecuteScalarAsync());
                    }

                    string query = @"SELECT pcode, barcode, pdesc, brand, category, price, qty 
                                     FROM TblProduct1 
                                     WHERE (pdesc LIKE @search OR pcode LIKE @search OR barcode LIKE @search)
                                     AND isactive = 1
                                     ORDER BY pdesc ASC 
                                     LIMIT @limit OFFSET @offset";

                    using (var cmd = new SQLiteCommand(query, cn))
                    {
                        cmd.Parameters.AddWithValue("@search", searchText);
                        cmd.Parameters.AddWithValue("@limit", pageSize);
                        cmd.Parameters.AddWithValue("@offset", offset);

                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            int i = offset;
                            while (await reader.ReadAsync())
                            {
                                dataGridView1.Rows.Add(++i,
                                    reader["pcode"], reader["barcode"], reader["pdesc"],
                                    reader["brand"], reader["category"], reader["price"],
                                    reader["qty"], "Select");
                            }
                        }
                    }
                }
                btnPrePage.Invalidate(); // Refresh arrow colors
                btnNextPage.Invalidate();
            }
            catch (Exception ex) { Console.WriteLine(@"Load Error: " + ex.Message); }
            finally { LockWindowUpdate(IntPtr.Zero); }
        }
        #endregion

        #region Transaction-Safe Save Logic
        private async void btnSave_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(cbCommands.Text)) { MessageBox.Show(@"Select Action Type.", @"POS", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
            if (string.IsNullOrWhiteSpace(txtPcode.Text)) { MessageBox.Show(@"Select a product first.", @"POS", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
            if (!int.TryParse(txtQty.Text.Trim(), out int adjQty) || adjQty <= 0) { MessageBox.Show(@"Enter a valid positive quantity.", @"POS", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }

            btnSave.Enabled = false;

            try
            {
                using (var cn = new SQLiteConnection(_dbPath))
                {
                    await cn.OpenAsync();
                    using (var tran = cn.BeginTransaction())
                    {
                        // 1. Precise Stock Check
                        int currentQty = 0;
                        using (var get = new SQLiteCommand("SELECT qty FROM TblProduct1 WHERE pcode=@p", cn, tran))
                        {
                            get.Parameters.AddWithValue("@p", txtPcode.Text);
                            currentQty = Convert.ToInt32(await get.ExecuteScalarAsync() ?? 0);
                        }

                        // 2. Add/Remove Logic (Check exact string from your mapping)
                        int newQty = (cbCommands.Text == "Remove from Inventory") ? (currentQty - adjQty) : (currentQty + adjQty);

                        if (newQty < 0)
                        {
                            MessageBox.Show(@"Action results in negative stock.", @"Inventory Error", MessageBoxButtons.OK, MessageBoxIcon.Stop);
                            return;
                        }

                        // 3. Update TblProduct1
                        using (var upd = new SQLiteCommand("UPDATE TblProduct1 SET qty=@q WHERE pcode=@p", cn, tran))
                        {
                            upd.Parameters.AddWithValue("@q", newQty);
                            upd.Parameters.AddWithValue("@p", txtPcode.Text);
                            await upd.ExecuteNonQueryAsync();
                        }

                        // 4. Log to tblAdjustment
                        using (var ins = new SQLiteCommand(@"INSERT INTO tblAdjustment 
                            (referenceno, pcode, qty, action, remarks, sdate, [user]) 
                            VALUES (@ref, @p, @qty, @act, @rem, @date, @u)", cn, tran))
                        {
                            ins.Parameters.AddWithValue("@ref", txtRef.Text);
                            ins.Parameters.AddWithValue("@p", txtPcode.Text);
                            ins.Parameters.AddWithValue("@qty", adjQty);
                            ins.Parameters.AddWithValue("@act", cbCommands.Text);
                            ins.Parameters.AddWithValue("@rem", txtRemarks.Text);
                            ins.Parameters.AddWithValue("@date", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                            ins.Parameters.AddWithValue("@u", suser);
                            await ins.ExecuteNonQueryAsync();
                        }

                        tran.Commit();
                        MessageBox.Show(@"Adjustment saved successfully.", @"POS Elite", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        ClearFields();
                        await LoadRecordsAsync();
                    }
                }
            }
            catch (Exception ex) { MessageBox.Show(@"Error: " + ex.Message); }
            finally { btnSave.Enabled = true; }
        }
        #endregion

        private void dataGridView1_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;

            // Using the new unique name fixed in designer
            if (dataGridView1.Columns[e.ColumnIndex].Name == "colSelect")
            {
                txtPcode.Text = dataGridView1.Rows[e.RowIndex].Cells[1].Value?.ToString();
                txtdesc.Text = dataGridView1.Rows[e.RowIndex].Cells[3].Value?.ToString();

                txtQty.Focus();
                txtQty.SelectAll();
            }
        }

        private void ClearFields()
        {
            referenceNo();
            txtQty.Clear();
            txtPcode.Clear();
            txtdesc.Clear();
            txtRemarks.Clear();
        }

        private void pictureBox2_Click(object sender, EventArgs e) => this.Dispose();

        private void panel1_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                ReleaseCapture();
                SendMessage(Handle, WM_NCLBUTTONDOWN, HT_CAPTION, 0);
            }
        }

        #region Junk Designer Stubs (Kept to prevent errors)
        private void txtSearch_KeyPress(object sender, KeyPressEventArgs e) { }
        private void txtUser_TextChanged(object sender, EventArgs e) { }
        private void txtRemarks_TextChanged(object sender, EventArgs e) { }
        private void cbCommands_SelectedIndexChanged(object sender, EventArgs e) { }
        private void txtRef_TextChanged(object sender, EventArgs e) { }
        private void txtPcode_TextChanged(object sender, EventArgs e) { }
        private void txtdesc_TextChanged(object sender, EventArgs e) { }
        private void txtQty_TextChanged(object sender, EventArgs e) { }
        private void txtSearch_Click(object sender, EventArgs e) { }
        #endregion
    }
}