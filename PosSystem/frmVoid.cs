using System;
using System.Data.SQLite;
using System.Runtime.InteropServices; // Added for draggable logic
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

        // ===== Public properties to be set by frmCancelDetails =====
        public string CancelAction { get; set; }      // "Yes" or "No"
        public int CancelQty { get; set; }            // number of items to cancel
        public string CancelReason { get; set; }      // reason for cancellation
        public string ProductCode { get; set; }       // product code
        public string CartId { get; set; }            // cart ID
        public decimal Price { get; set; }            // unit price
        public string CancelledBy { get; set; }       // name of person who cancelled
        public frmSoldItems SoldItemForm { get; set; } // optional link to refresh sold items

        public frmVoid()
        {
            InitializeComponent();
        }

        private void frmVoid_Load(object sender, EventArgs e)
        {
            // Optional: initialization code
        }

        private void pictureBox2_Click(object sender, EventArgs e)
        {
            this.Dispose();
        }

        private async void btnSave_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtUser.Text) || string.IsNullOrWhiteSpace(txtPass.Text))
                return;

            using (SQLiteConnection cn = new SQLiteConnection(DBConnection.MyConnection()))
            {
                cn.Open();
                SQLiteTransaction tx = cn.BeginTransaction();

                try
                {
                    string user = AuthenticateUser(cn, tx);
                    if (user == null)
                    {
                        tx.Rollback();
                        MessageBox.Show("Invalid password!", "Warning",
                            MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }

                    // 1️⃣ Record cancellation
                    SaveCancelOrder(cn, tx, user);

                    // 2️⃣ Restore product stock if action is "Yes"
                    if (CancelAction == "Yes")
                    {
                        UpdateProductQty(cn, tx, ProductCode, CancelQty);
                    }

                    // 3️⃣ Reduce cart quantity
                    UpdateCartQty(cn, tx, CartId, CancelQty);

                    // ✅ Commit transaction
                    tx.Commit();

                    MessageBox.Show("Order transaction successfully cancelled!",
                        "Cancel Order", MessageBoxButtons.OK, MessageBoxIcon.Information);

                    // Refresh parent sold items form
                    if (SoldItemForm != null)
                        await SoldItemForm.LoadSoldItemsAsync();

                    this.Dispose();
                }
                catch (Exception ex)
                {
                    tx.Rollback();
                    MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        // 🔐 Authenticate the user for void
        private string AuthenticateUser(SQLiteConnection cn, SQLiteTransaction tx)
        {
            using (SQLiteCommand cm = new SQLiteCommand(
                "SELECT username FROM tblUser WHERE username=@u AND password=@p",
                cn, tx))
            {
                cm.Parameters.AddWithValue("@u", txtUser.Text);
                cm.Parameters.AddWithValue("@p", txtPass.Text);
                object result = cm.ExecuteScalar();
                return result?.ToString();
            }
        }

        // 🔹 Save cancellation to tblCancel
        private void SaveCancelOrder(SQLiteConnection cn, SQLiteTransaction tx, string user)
        {
            using (SQLiteCommand cm = new SQLiteCommand(
                @"INSERT INTO tblCancel
                  (transno, pcode, price, qty, sdate, voidby, cancelledby, reason, action)
                  VALUES
                  (@transno, @pcode, @price, @qty, @sdate, @voidby, @cancelledby, @reason, @action)",
                cn, tx))
            {
                cm.Parameters.AddWithValue("@transno", CartId);   // use CartId as transaction reference
                cm.Parameters.AddWithValue("@pcode", ProductCode);
                cm.Parameters.AddWithValue("@price", Price);
                cm.Parameters.AddWithValue("@qty", CancelQty);
                cm.Parameters.AddWithValue("@sdate", DateTime.Now);
                cm.Parameters.AddWithValue("@voidby", user);
                cm.Parameters.AddWithValue("@cancelledby", CancelledBy);
                cm.Parameters.AddWithValue("@reason", CancelReason);
                cm.Parameters.AddWithValue("@action", CancelAction);
                cm.ExecuteNonQuery();
            }
        }

        // 🔹 Restore product stock
        private void UpdateProductQty(SQLiteConnection cn, SQLiteTransaction tx, string pcode, int qty)
        {
            using (SQLiteCommand cm = new SQLiteCommand(
                "UPDATE TblProduct1 SET qty = qty + @qty WHERE pcode = @pcode",
                cn, tx))
            {
                cm.Parameters.AddWithValue("@qty", qty);
                cm.Parameters.AddWithValue("@pcode", pcode);
                cm.ExecuteNonQuery();
            }
        }

        // 🔹 Reduce cart quantity
        private void UpdateCartQty(SQLiteConnection cn, SQLiteTransaction tx, string cartId, int qty)
        {
            using (SQLiteCommand cm = new SQLiteCommand(
                "UPDATE tblCart1 SET qty = qty - @qty WHERE id = @id",
                cn, tx))
            {
                cm.Parameters.AddWithValue("@qty", qty);
                cm.Parameters.AddWithValue("@id", cartId);
                cm.ExecuteNonQuery();
            }
        }
    }
}