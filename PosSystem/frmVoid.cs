using System;
using System.Data.SQLite;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PosSystem
{
    public partial class frmVoid : Form
    {
        #region Win32 API for Draggable Form
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

        // ===== Public properties set by frmCancelDetails =====
        public string CancelAction { get; set; }      // "Yes" or "No"
        public int CancelQty { get; set; }            // number of items to cancel
        public string CancelReason { get; set; }      // reason for cancellation
        public string ProductCode { get; set; }       // product code
        public string CartId { get; set; }            // Primary Key of tblCart1
        public decimal Price { get; set; }            // unit price
        public string CancelledBy { get; set; }       // name of person who cancelled
        public frmSoldItems SoldItemForm { get; set; }

        public bool Approved { get; private set; } = false;
        public bool SaveSuccess { get; private set; } = false;

        public frmVoid()
        {
            InitializeComponent();
        }

        // ✅ Fix for designer error: empty Load handler
        private void frmVoid_Load(object sender, EventArgs e) { }

        private void pictureBox2_Click(object sender, EventArgs e) => this.Dispose();

        private async void btnSave_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtUser.Text) || string.IsNullOrWhiteSpace(txtPass.Text))
            {
                MessageBox.Show("Please enter administrator credentials.", "Void Authorization", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            using (SQLiteConnection cn = new SQLiteConnection(DBConnection.MyConnection()))
            {
                cn.Open();
                using (SQLiteTransaction tx = cn.BeginTransaction())
                {
                    try
                    {
                        // Authenticate Admin/Authorized User
                        string authUser = AuthenticateUser(cn, tx);
                        if (authUser == null)
                        {
                            tx.Rollback();
                            MessageBox.Show("Invalid administrator username or password!", "Access Denied",
                                MessageBoxButtons.OK, MessageBoxIcon.Error);
                            txtPass.Clear();
                            txtPass.Focus();
                            return;
                        }

                        Approved = true;

                        // 1️⃣ Record the cancellation in tblCancel
                        SaveCancelOrder(cn, tx, authUser);

                        // 2️⃣ Restore product stock if action is "Yes" (Returned to Inventory)
                        if (CancelAction?.Trim().ToUpper() == "YES")
                        {
                            UpdateProductQty(cn, tx, ProductCode, CancelQty);
                        }

                        // 3️⃣ Reduce quantity in tblCart1
                        UpdateCartQty(cn, tx, CartId, CancelQty);

                        // ✅ Finalize Database Changes
                        tx.Commit();
                        SaveSuccess = true;

                        MessageBox.Show("Transaction has been successfully voided.",
                            "Void Success", MessageBoxButtons.OK, MessageBoxIcon.Information);

                        if (SoldItemForm != null)
                            await SoldItemForm.LoadSoldItemsAsync();

                        this.Dispose();
                    }
                    catch (Exception ex)
                    {
                        tx.Rollback();
                        SaveSuccess = false;
                        MessageBox.Show("Critical Database Error: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private string AuthenticateUser(SQLiteConnection cn, SQLiteTransaction tx)
        {
            // Targeting tblUser based on your remembered schema
            using (SQLiteCommand cm = new SQLiteCommand(
                "SELECT username FROM tblUser WHERE username=@u AND password=@p AND role='Administrator'",
                cn, tx))
            {
                cm.Parameters.AddWithValue("@u", txtUser.Text);
                cm.Parameters.AddWithValue("@p", txtPass.Text);
                object result = cm.ExecuteScalar();
                return result?.ToString();
            }
        }

        private void SaveCancelOrder(SQLiteConnection cn, SQLiteTransaction tx, string adminUser)
        {
            using (SQLiteCommand cm = new SQLiteCommand(
                @"INSERT INTO tblCancel 
                  (transno, pcode, price, qty, sdate, voidby, cancelledby, reason, action) 
                  VALUES 
                  (@transno, @pcode, @price, @qty, @sdate, @voidby, @cancelledby, @reason, @action)",
                cn, tx))
            {
                // Note: We use CartId as the reference transno for line-item tracking
                cm.Parameters.AddWithValue("@transno", CartId);
                cm.Parameters.AddWithValue("@pcode", ProductCode);
                cm.Parameters.AddWithValue("@price", Price);
                cm.Parameters.AddWithValue("@qty", CancelQty);
                cm.Parameters.AddWithValue("@sdate", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                cm.Parameters.AddWithValue("@voidby", adminUser);
                cm.Parameters.AddWithValue("@cancelledby", CancelledBy);
                cm.Parameters.AddWithValue("@reason", CancelReason);
                cm.Parameters.AddWithValue("@action", CancelAction);
                cm.ExecuteNonQuery();
            }
        }

        private void UpdateProductQty(SQLiteConnection cn, SQLiteTransaction tx, string pcode, int qty)
        {
            // Updates TblProduct1 - this will trigger trg_product_update
            using (SQLiteCommand cm = new SQLiteCommand(
                "UPDATE TblProduct1 SET qty = qty + @qty WHERE pcode = @pcode", cn, tx))
            {
                cm.Parameters.AddWithValue("@qty", qty);
                cm.Parameters.AddWithValue("@pcode", pcode);
                cm.ExecuteNonQuery();
            }
        }

        private void UpdateCartQty(SQLiteConnection cn, SQLiteTransaction tx, string cartId, int qty)
        {
            // Reduces qty in tblCart1. Note: If qty becomes 0, item remains but with 0 qty. 
            // Most POS systems prefer this for historical audit integrity.
            using (SQLiteCommand cm = new SQLiteCommand(
                "UPDATE tblCart1 SET qty = qty - @qty WHERE id = @id", cn, tx))
            {
                cm.Parameters.AddWithValue("@qty", qty);
                cm.Parameters.AddWithValue("@id", cartId);
                cm.ExecuteNonQuery();
            }
        }

        #region Junk Designer Stubs
        private void txtUser_TextChanged(object sender, EventArgs e) { }
        private void txtPass_TextChanged(object sender, EventArgs e) { }
        #endregion
    }
}