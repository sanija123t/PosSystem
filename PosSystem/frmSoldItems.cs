using System;
using System.Data.SQLite;
using System.Windows.Forms;
using System.Runtime.InteropServices;

namespace PosSystem
{
    public partial class frmSoldItems : Form
    {
        SQLiteConnection cn;
        SQLiteCommand cm;
        SQLiteDataReader dr;
        public string suser;

        [DllImport("user32.dll")]
        public static extern bool ReleaseCapture();

        [DllImport("user32.dll")]
        public static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);

        const int WM_NCLBUTTONDOWN = 0xA1;
        const int HT_CAPTION = 0x2;

        public frmSoldItems()
        {
            InitializeComponent();
            cn = new SQLiteConnection(DBConnection.MyConnection());
            dateTimePicker1.Value = DateTime.Now;
            dateTimePicker2.Value = DateTime.Now;
            LoadCashier();
            LocalRecord();
        }

        private void frmSoldItems_Load(object sender, EventArgs e)
        {
            LocalRecord();
        }

        public void LocalRecord()
        {
            try
            {
                int i = 0;
                double _total = 0;
                dataGridView1.Rows.Clear();
                cn.Open();

                string sql = "SELECT c.id, c.transno, c.pcode, p.pdesc, c.price, c.qty, c.disc, (c.qty * c.price) - c.disc as total " +
                             "FROM tblCart1 as c INNER JOIN TblProduct1 as p ON c.pcode = p.pcode " +
                             "WHERE status LIKE 'sold' AND sdate BETWEEN @date1 AND @date2";

                if (cbCashier.Text != "All Cashier" && !string.IsNullOrEmpty(cbCashier.Text))
                {
                    sql += " AND cashier LIKE @cashier";
                }

                cm = new SQLiteCommand(sql, cn);
                cm.Parameters.AddWithValue("@date1", dateTimePicker1.Value.ToString("yyyy-MM-dd"));
                cm.Parameters.AddWithValue("@date2", dateTimePicker2.Value.ToString("yyyy-MM-dd"));

                if (cbCashier.Text != "All Cashier" && !string.IsNullOrEmpty(cbCashier.Text))
                {
                    cm.Parameters.AddWithValue("@cashier", cbCashier.Text);
                }

                dr = cm.ExecuteReader();
                while (dr.Read())
                {
                    i++;
                    double total = dr["total"] != DBNull.Value ? Convert.ToDouble(dr["total"]) : 0;
                    _total += total;
                    dataGridView1.Rows.Add(i, dr["id"].ToString(), dr["transno"].ToString(), dr["pcode"].ToString(), dr["pdesc"].ToString(), dr["price"].ToString(), dr["qty"].ToString(), dr["disc"].ToString(), total.ToString("#,##0.00"));
                }
                dr.Close();
                cn.Close();
                lblTotal1.Text = _total.ToString("#,##0.00");
            }
            catch (Exception ex)
            {
                if (cn.State == System.Data.ConnectionState.Open) cn.Close();
                MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void pictureBox2_Click(object sender, EventArgs e)
        {
            this.Dispose();
        }

        private void dataGridView1_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;

            string colName = dataGridView1.Columns[e.ColumnIndex].Name;
            if (colName == "ColCancel")
            {
                frmCancelDetails f = new frmCancelDetails(this);
                f.txtID.Text = dataGridView1.Rows[e.RowIndex].Cells[1].Value.ToString();
                f.txtTransno.Text = dataGridView1.Rows[e.RowIndex].Cells[2].Value.ToString();
                f.txtPcode.Text = dataGridView1.Rows[e.RowIndex].Cells[3].Value.ToString();
                f.txtDesc.Text = dataGridView1.Rows[e.RowIndex].Cells[4].Value.ToString();
                f.txtPrice.Text = dataGridView1.Rows[e.RowIndex].Cells[5].Value.ToString();
                f.txtQty.Text = dataGridView1.Rows[e.RowIndex].Cells[6].Value.ToString();
                f.txtDiscount.Text = dataGridView1.Rows[e.RowIndex].Cells[7].Value.ToString();
                f.txtTotal.Text = dataGridView1.Rows[e.RowIndex].Cells[8].Value.ToString();
                f.txtCancelled.Text = suser;
                f.ShowDialog();
            }
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            frmReportSold frm = new frmReportSold(this);
            frm.LoadReport();
            frm.ShowDialog();
        }

        private void cbCashier_KeyPress(object sender, KeyPressEventArgs e)
        {
            e.Handled = true;
        }

        public void LoadCashier()
        {
            try
            {
                cbCashier.Items.Clear();
                cbCashier.Items.Add("All Cashier");
                cn.Open();
                cm = new SQLiteCommand("SELECT username FROM tblUser WHERE role LIKE 'Cashier'", cn);
                dr = cm.ExecuteReader();
                while (dr.Read())
                {
                    cbCashier.Items.Add(dr["username"].ToString());
                }
                dr.Close();
                cn.Close();
                cbCashier.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                if (cn.State == System.Data.ConnectionState.Open) cn.Close();
                MessageBox.Show(ex.Message);
            }
        }

        private void cbCashier_SelectedIndexChanged_1(object sender, EventArgs e)
        {
            LocalRecord();
        }

        private void dateTimePicker1_ValueChanged(object sender, EventArgs e)
        {
            LocalRecord();
        }

        private void dateTimePicker2_ValueChanged(object sender, EventArgs e)
        {
            LocalRecord();
        }

        private void panel1_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                ReleaseCapture();
                SendMessage(Handle, WM_NCLBUTTONDOWN, HT_CAPTION, 0);
            }
        }

        private void panel1_Paint(object sender, PaintEventArgs e)
        {

        }
    }
}