using System;
using System.Data.SQLite;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.Threading;

namespace PosSystem
{
    public partial class frmAdjustment : Form
    {
        Form1 f;
        public string suser;

        private Label lblTotal1;

        // debounce search timer
        private CancellationTokenSource searchCTS;

        #region Drag Form Logic
        [DllImport("user32.dll")]
        public static extern bool ReleaseCapture();

        [DllImport("user32.dll")]
        public static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);

        private const int WM_NCLBUTTONDOWN = 0xA1;
        private const int HT_CAPTION = 0x2;
        #endregion

        public frmAdjustment()
        {
            InitializeComponent();

            lblTotal1 = new Label { Visible = false };
            this.Controls.Add(lblTotal1);

            txtSearch.TextChanged += TxtSearch_TextChanged;
            panel1.MouseDown += panel1_MouseDown;
        }

        public frmAdjustment(Form1 frm) : this()
        {
            f = frm;
            suser = frm._user;
        }

        private void frmAdjustment_Load(object sender, EventArgs e)
        {
            txtUser.Text = suser;
            referenceNo();
            _ = LoadRecordsAsync();
        }

        public void referenceNo()
        {
            txtRef.Text = "ADJ-" + Guid.NewGuid().ToString("N").Substring(0, 6).ToUpper();
        }

        public void LoadRecords()
        {
            _ = LoadRecordsAsync();
        }

        #region SMART SEARCH (Debounced)
        private async void TxtSearch_TextChanged(object sender, EventArgs e)
        {
            searchCTS?.Cancel();
            searchCTS = new CancellationTokenSource();
            var token = searchCTS.Token;

            try
            {
                await Task.Delay(300, token); // wait for typing pause
                await LoadRecordsAsync();
            }
            catch (TaskCanceledException) { }
        }
        #endregion

        public async Task LoadRecordsAsync()
        {
            if (IsDisposed) return;

            dataGridView1.Rows.Clear();
            int i = 0;
            string searchText = txtSearch.Text.Trim();

            try
            {
                using (var cn = new SQLiteConnection(DBConnection.MyConnection()))
                {
                    await cn.OpenAsync();

                    string query = @"SELECT pcode,pdesc,qty 
                                     FROM TblProduct1 
                                     WHERE pdesc LIKE @search
                                     ORDER BY pdesc ASC";

                    using (var cmd = new SQLiteCommand(query, cn))
                    {
                        cmd.Parameters.AddWithValue("@search", "%" + searchText + "%");

                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                if (IsDisposed) return;

                                i++;
                                dataGridView1.Rows.Add(
                                    i,
                                    reader["pcode"].ToString(),
                                    reader["pdesc"].ToString(),
                                    reader["qty"].ToString()
                                );
                            }
                        }
                    }
                }

                lblTotal1.Text = i.ToString();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Load Error:\n" + ex.Message, "Database", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void txtSearch_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == (char)Keys.Enter)
            {
                e.Handled = true;
                _ = LoadRecordsAsync();
            }
        }

        private async void btnSave_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(cbCommands.Text) || string.IsNullOrWhiteSpace(txtQty.Text))
            {
                MessageBox.Show("Please select command and enter quantity.", "Validation");
                return;
            }

            if (!int.TryParse(txtQty.Text.Trim(), out int adjQty) || adjQty <= 0)
            {
                MessageBox.Show("Enter a valid quantity greater than zero.", "Validation");
                return;
            }

            string command = cbCommands.Text.Trim().ToLower();
            string pcode = txtPcode.Text.Trim();

            if (string.IsNullOrEmpty(pcode))
            {
                MessageBox.Show("Please select a product first.", "Validation");
                return;
            }

            btnSave.Enabled = false;
            Cursor = Cursors.WaitCursor;

            try
            {
                using (var cn = new SQLiteConnection(DBConnection.MyConnection()))
                {
                    await cn.OpenAsync();

                    using (var tran = cn.BeginTransaction())
                    {
                        int currentQty;

                        // GET CURRENT QTY
                        using (var get = new SQLiteCommand("SELECT qty FROM TblProduct1 WHERE pcode=@p", cn, tran))
                        {
                            get.Parameters.AddWithValue("@p", pcode);
                            object val = await get.ExecuteScalarAsync();

                            if (val == null)
                                throw new Exception("Selected product no longer exists.");

                            currentQty = Convert.ToInt32(val);
                        }

                        int newQty = command == "add"
                            ? currentQty + adjQty
                            : currentQty - adjQty;

                        // FRIENDLY NEGATIVE CHECK
                        if (newQty < 0)
                        {
                            MessageBox.Show(
                                $"Not enough stock.\n\nCurrent Stock: {currentQty}\nAttempted Remove: {adjQty}",
                                "Stock Error",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Warning);

                            return;
                        }

                        // UPDATE STOCK
                        using (var upd = new SQLiteCommand("UPDATE TblProduct1 SET qty=@q WHERE pcode=@p", cn, tran))
                        {
                            upd.Parameters.AddWithValue("@q", newQty);
                            upd.Parameters.AddWithValue("@p", pcode);
                            await upd.ExecuteNonQueryAsync();
                        }

                        // INSERT LOG
                        using (var ins = new SQLiteCommand(
                            @"INSERT INTO tblAdjustment 
                            (referenceno,pcode,qty,action,remarks,sdate,[user])
                            VALUES (@ref,@p,@qty,@act,@rem,@date,@u)", cn, tran))
                        {
                            ins.Parameters.AddWithValue("@ref", txtRef.Text);
                            ins.Parameters.AddWithValue("@p", pcode);
                            ins.Parameters.AddWithValue("@qty", adjQty);
                            ins.Parameters.AddWithValue("@act", command);
                            ins.Parameters.AddWithValue("@rem", "Manual Adjustment");
                            ins.Parameters.AddWithValue("@date", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                            ins.Parameters.AddWithValue("@u", suser);

                            await ins.ExecuteNonQueryAsync();
                        }

                        tran.Commit();
                    }
                }

                MessageBox.Show("Stock adjusted successfully.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);

                referenceNo();
                txtQty.Clear();
                txtPcode.Clear();
                txtdesc.Clear();
                await LoadRecordsAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Adjustment Failed:\n" + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                btnSave.Enabled = true;
                Cursor = Cursors.Default;
            }
        }

        private void dataGridView1_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;

            // SAFE COLUMN CHECK
            if (!dataGridView1.Columns.Contains("ActionSelect"))
                return;

            if (e.ColumnIndex == dataGridView1.Columns["ActionSelect"].Index)
            {
                txtPcode.Text = dataGridView1.Rows[e.RowIndex].Cells[1].Value?.ToString();
                txtdesc.Text = dataGridView1.Rows[e.RowIndex].Cells[2].Value?.ToString();
            }
        }

        private void panel1_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                ReleaseCapture();
                SendMessage(Handle, WM_NCLBUTTONDOWN, HT_CAPTION, 0);
            }
        }

        private void pictureBox2_Click(object sender, EventArgs e)
        {
            this.Dispose();
        }
    }
}