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
        Form1 f;
        public string suser;
        private CancellationTokenSource searchCTS;

        // ELITE: Win32 API to freeze painting during heavy updates (Prevents Flickering)
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
        }

        public frmAdjustment(Form1 frm) : this()
        {
            f = frm;
            suser = frm._user;
        }

        private void SetupEliteUI()
        {
            // ELITE: Wiring events programmatically to ensure they aren't lost by the designer
            txtSearch.TextChanged += TxtSearch_TextChanged;
            panel1.MouseDown += panel1_MouseDown;

            // ELITE: Double buffering the grid for smooth scrolling
            typeof(DataGridView).GetProperty("DoubleBuffered",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(dataGridView1, true, null);
        }

        private void frmAdjustment_Load(object sender, EventArgs e)
        {
            txtUser.Text = suser;
            referenceNo();
            _ = LoadRecordsAsync();
        }

        public void referenceNo()
        {
            // ELITE: Professional Audit-Ready Reference ID
            txtRef.Text = $"ADJ-{DateTime.Now:yyyyMMdd}-{Guid.NewGuid().ToString("N").Substring(0, 5).ToUpper()}";
        }

        #region High-Performance Data Loading
        private async void TxtSearch_TextChanged(object sender, EventArgs e)
        {
            // ELITE: Debouncing pattern to prevent database hammering while typing
            searchCTS?.Cancel();
            searchCTS = new CancellationTokenSource();
            var token = searchCTS.Token;

            try
            {
                await Task.Delay(250, token);
                await LoadRecordsAsync();
            }
            catch (TaskCanceledException) { /* Ignored on purpose */ }
        }

        public async Task LoadRecordsAsync()
        {
            if (IsDisposed) return;

            try
            {
                // ELITE: Visual feedback during async operation
                LockWindowUpdate(dataGridView1.Handle);
                dataGridView1.Rows.Clear();

                string searchText = txtSearch.Text.Trim();

                using (var cn = new SQLiteConnection(_dbPath))
                {
                    await cn.OpenAsync();
                    string query = @"SELECT pcode, pdesc, qty 
                                     FROM TblProduct1 
                                     WHERE pdesc LIKE @search OR pcode LIKE @search
                                     ORDER BY pdesc ASC LIMIT 500"; // Limit for performance

                    using (var cmd = new SQLiteCommand(query, cn))
                    {
                        cmd.Parameters.AddWithValue("@search", $"%{searchText}%");

                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            int i = 0;
                            while (await reader.ReadAsync())
                            {
                                if (IsDisposed) return;
                                dataGridView1.Rows.Add(++i,
                                    reader["pcode"],
                                    reader["pdesc"],
                                    reader["qty"]);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(@"Critical Load Error: " + ex.Message);
            }
            finally
            {
                LockWindowUpdate(IntPtr.Zero);
            }
        }
        #endregion

        #region Transaction-Safe Save Logic
        private async void btnSave_Click(object sender, EventArgs e)
        {
            // ELITE: Guard Clauses for cleaner logic
            if (string.IsNullOrWhiteSpace(cbCommands.Text)) { MessageBox.Show("Select Action Type.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
            if (string.IsNullOrWhiteSpace(txtPcode.Text)) { MessageBox.Show("Please select a product.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
            if (!int.TryParse(txtQty.Text.Trim(), out int adjQty) || adjQty <= 0) { MessageBox.Show("Invalid Quantity.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }

            string command = cbCommands.Text.Trim().ToLower();
            btnSave.Enabled = false;

            try
            {
                using (var cn = new SQLiteConnection(_dbPath))
                {
                    await cn.OpenAsync();
                    using (var tran = cn.BeginTransaction())
                    {
                        try
                        {
                            // 1. Precise Stock Check
                            int currentQty = 0;
                            using (var get = new SQLiteCommand("SELECT qty FROM TblProduct1 WHERE pcode=@p", cn, tran))
                            {
                                get.Parameters.AddWithValue("@p", txtPcode.Text);
                                object val = await get.ExecuteScalarAsync();
                                if (val == null) throw new Exception("Product code not found in registry.");
                                currentQty = Convert.ToInt32(val);
                            }

                            // 2. Business Logic Validation
                            int newQty = (command == "add") ? (currentQty + adjQty) : (currentQty - adjQty);
                            if (newQty < 0)
                            {
                                MessageBox.Show($"Insufficient Stock. Current: {currentQty}", "Inventory Breach", MessageBoxButtons.OK, MessageBoxIcon.Hand);
                                return;
                            }

                            // 3. Atomic Updates
                            using (var upd = new SQLiteCommand("UPDATE TblProduct1 SET qty=@q WHERE pcode=@p", cn, tran))
                            {
                                upd.Parameters.AddWithValue("@q", newQty);
                                upd.Parameters.AddWithValue("@p", txtPcode.Text);
                                await upd.ExecuteNonQueryAsync();
                            }

                            // 4. Persistence of Audit Log
                            using (var ins = new SQLiteCommand(@"INSERT INTO tblAdjustment 
                                (referenceno, pcode, qty, action, remarks, sdate, [user]) 
                                VALUES (@ref, @p, @qty, @act, @rem, @date, @u)", cn, tran))
                            {
                                ins.Parameters.AddWithValue("@ref", txtRef.Text);
                                ins.Parameters.AddWithValue("@p", txtPcode.Text);
                                ins.Parameters.AddWithValue("@qty", adjQty);
                                ins.Parameters.AddWithValue("@act", command);
                                ins.Parameters.AddWithValue("@rem", txtRemarks.Text);
                                ins.Parameters.AddWithValue("@date", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                                ins.Parameters.AddWithValue("@u", suser);
                                await ins.ExecuteNonQueryAsync();
                            }

                            tran.Commit();
                            MessageBox.Show("Stock synchronized successfully.", "POS Elite", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            ClearFields();
                            await LoadRecordsAsync();
                        }
                        catch (Exception)
                        {
                            tran.Rollback();
                            throw;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(@"Elite Error Tracker: " + ex.Message, "System Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                btnSave.Enabled = true;
            }
        }
        #endregion

        private void dataGridView1_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
            string colName = dataGridView1.Columns[e.ColumnIndex].Name;

            // ELITE: Logical detection of the selection action
            if (colName.ToLower().Contains("select") || e.ColumnIndex == dataGridView1.ColumnCount - 1)
            {
                txtPcode.Text = dataGridView1.Rows[e.RowIndex].Cells[1].Value?.ToString();
                if (txtdesc != null) txtdesc.Text = dataGridView1.Rows[e.RowIndex].Cells[2].Value?.ToString();
                txtQty.Focus();
            }
        }

        private void ClearFields()
        {
            referenceNo();
            txtQty.Clear();
            txtPcode.Clear();
            if (txtdesc != null) txtdesc.Clear();
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

        // KEEPING DESIGNER STUBS TO PREVENT COMPILATION ERRORS
        private void txtSearch_KeyPress(object sender, KeyPressEventArgs e) { if (e.KeyChar == (char)Keys.Enter) _ = LoadRecordsAsync(); }
        private void txtUser_TextChanged(object sender, EventArgs e) { }
        private void txtRemarks_TextChanged(object sender, EventArgs e) { }
        private void cbCommands_SelectedIndexChanged(object sender, EventArgs e) { }
        private void txtRef_TextChanged(object sender, EventArgs e) { }
        private void txtPcode_TextChanged(object sender, EventArgs e) { }
        private void txtdesc_TextChanged(object sender, EventArgs e) { }
        private void txtQty_TextChanged(object sender, EventArgs e) { }
        private void txtSearch_Click(object sender, EventArgs e) { }
    }
}