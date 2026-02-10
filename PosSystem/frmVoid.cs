using System;
using System.Data.SQLite; // Ensure this matches your project (SQLite or SqlClient)
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PosSystem
{
    public partial class frmVoid : Form
    {
        // Use SQLiteConnection if your DB is SQLite, otherwise SqlConnection
        SQLiteConnection cn;
        SQLiteCommand cm;
        SQLiteDataReader dr;
        frmCancelDetails f;

        public frmVoid(frmCancelDetails frm)
        {
            InitializeComponent();
            cn = new SQLiteConnection(DBConnection.MyConnection());
            f = frm;
        }

        private void pictureBox2_Click(object sender, EventArgs e)
        {
            this.Dispose();
        }

        // Added 'async' so we can 'await' the refresh list
        private async void btnSave_Click(object sender, EventArgs e)
        {
            try
            {
                if (!string.IsNullOrEmpty(txtPass.Text))
                {
                    string user;
                    cn.Open();
                    // Parameterized login check
                    cm = new SQLiteCommand("SELECT * FROM tblUser WHERE username = @username AND password = @password", cn);
                    cm.Parameters.AddWithValue("@username", txtUser.Text);
                    cm.Parameters.AddWithValue("@password", txtPass.Text);

                    dr = cm.ExecuteReader();
                    bool hasRows = dr.Read();

                    if (hasRows)
                    {
                        user = dr["username"].ToString();
                        dr.Close();
                        cn.Close();

                        // 1. Record the cancellation
                        SaveCancelOrder(user);

                        // 2. Update Product Inventory if action is 'Yes'
                        if (f.cbAction.Text == "Yes")
                        {
                            UpdateProductQty(f.txtPcode.Text, int.Parse(f.txtCancelQty.Text));
                        }

                        // 3. Update Cart Quantity
                        UpdateCartQty(f.txtID.Text, int.Parse(f.txtCancelQty.Text));

                        MessageBox.Show("Order transaction successfully cancelled!", "Cancel Order", MessageBoxButtons.OK, MessageBoxIcon.Information);

                        // 4. Await the refresh of the parent grid
                        await f.RefreshList();

                        // 5. Close this form
                        this.Dispose();
                    }
                    else
                    {
                        dr.Close();
                        cn.Close();
                        MessageBox.Show("Invalid password!", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                }
            }
            catch (Exception ex)
            {
                if (cn.State == System.Data.ConnectionState.Open) cn.Close();
                MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        public void SaveCancelOrder(string user)
        {
            cn.Open();
            cm = new SQLiteCommand("INSERT INTO tblCancel (transno, pcode, price, qty, sdate, voidby, cancelledby, reason, action) " +
                                   "VALUES (@transno, @pcode, @price, @qty, @sdate, @voidby, @cancelledby, @reason, @action)", cn);
            cm.Parameters.AddWithValue("@transno", f.txtTransno.Text);
            cm.Parameters.AddWithValue("@pcode", f.txtPcode.Text);
            cm.Parameters.AddWithValue("@price", double.Parse(f.txtPrice.Text));
            cm.Parameters.AddWithValue("@qty", int.Parse(f.txtCancelQty.Text));
            cm.Parameters.AddWithValue("@sdate", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            cm.Parameters.AddWithValue("@voidby", user);
            cm.Parameters.AddWithValue("@cancelledby", f.txtCancelled.Text);
            cm.Parameters.AddWithValue("@reason", f.txtReason.Text);
            cm.Parameters.AddWithValue("@action", f.cbAction.Text);
            cm.ExecuteNonQuery();
            cn.Close();
        }

        // ENTERPRISE IMPROVEMENT: Specialized methods with parameters to prevent SQL Injection
        public void UpdateProductQty(string pcode, int qty)
        {
            cn.Open();
            cm = new SQLiteCommand("UPDATE TblProduct1 SET qty = qty + @qty WHERE pcode = @pcode", cn);
            cm.Parameters.AddWithValue("@qty", qty);
            cm.Parameters.AddWithValue("@pcode", pcode);
            cm.ExecuteNonQuery();
            cn.Close();
        }

        public void UpdateCartQty(string cartId, int qty)
        {
            cn.Open();
            cm = new SQLiteCommand("UPDATE tblCart1 SET qty = qty - @qty WHERE id = @id", cn);
            cm.Parameters.AddWithValue("@qty", qty);
            cm.Parameters.AddWithValue("@id", cartId);
            cm.ExecuteNonQuery();
            cn.Close();
        }

        private void frmVoid_Load(object sender, EventArgs e)
        {
        }
    }
}