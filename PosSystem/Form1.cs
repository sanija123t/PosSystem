using System;
using System.Data;
using System.Data.SQLite;
using System.Drawing;
using System.Windows.Forms;
using Tulpep.NotificationWindow;

namespace PosSystem
{
    public partial class Form1 : Form
    {
        // Assigned during login
        public string _pass;
        public string _user;

        public Form1()
        {
            InitializeComponent();
            NotifyCriticalItems();
            MyDashbord();
        }

        private void OpenChildForm(Form childForm)
        {
            // Dispose of existing controls to prevent memory leaks/StackOverflow
            if (panel3.Controls.Count > 0)
            {
                panel3.Controls[0].Dispose();
            }
            panel3.Controls.Clear();

            childForm.TopLevel = false;
            childForm.FormBorderStyle = FormBorderStyle.None;
            childForm.Dock = DockStyle.Fill;

            panel3.Controls.Add(childForm);
            panel3.Tag = childForm;
            childForm.BringToFront();
            childForm.Show();
        }

        // ================= CRITICAL ITEM NOTIFICATION =================
        public void NotifyCriticalItems()
        {
            string criticalList = "";
            int count = 0;

            try
            {
                using (SQLiteConnection cn = new SQLiteConnection(DBConnection.MyConnection()))
                {
                    cn.Open();
                    string query = "SELECT pdesc FROM TblProduct1 WHERE qty <= reorder";
                    using (SQLiteCommand cm = new SQLiteCommand(query, cn))
                    using (SQLiteDataReader dr = cm.ExecuteReader())
                    {
                        while (dr.Read())
                        {
                            count++;
                            criticalList += $"{count}. {dr["pdesc"]}{Environment.NewLine}";
                        }
                    }
                }

                if (count > 0)
                {
                    PopupNotifier popup = new PopupNotifier
                    {
                        Image = SystemIcons.Warning.ToBitmap(),
                        TitleText = $"{count} Critical Item(s)",
                        ContentText = criticalList,
                        Delay = 3000
                    };
                    popup.Popup();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Notification Error: " + ex.Message);
            }
        }

        // ================= DASHBOARD =================
        public void MyDashbord()
        {
            try
            {
                // Clean up previous controls first
                if (panel3.Controls.Count > 0)
                {
                    panel3.Controls[0].Dispose();
                }
                panel3.Controls.Clear();

                frmDashbord f = new frmDashbord
                {
                    TopLevel = false,
                    Dock = DockStyle.Fill
                };

                panel3.Controls.Add(f);

                // Load data safely
                f.lblDailySales.Text = DBConnection.DailySales().ToString("#,##0.00");
                f.lblProduct.Text = DBConnection.ProductLine().ToString("#,##0");
                f.lblStock.Text = DBConnection.StockOnHand().ToString("#,##0");
                f.lblCriticalItems.Text = DBConnection.CraticalItems().ToString("#,##0");

                f.BringToFront();
                f.Show();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading dashboard: " + ex.Message, "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        // ================= BUTTON EVENTS =================
        private void btnBrand_Click(object sender, EventArgs e)
            => OpenChildForm(new frmBrandList());

        private void button4_Click(object sender, EventArgs e)
        {
            frmCategoryList frm = new frmCategoryList();
            frm.LoadCategory();
            OpenChildForm(frm);
        }

        private void btnProduct_Click(object sender, EventArgs e)
        {
            frmProduct_List frm = new frmProduct_List();
            frm.LoadRecords();
            OpenChildForm(frm);
        }

        private void btnStock_Click(object sender, EventArgs e)
            => OpenChildForm(new frmStockin());

        private void btnVendor_Click(object sender, EventArgs e)
        {
            frm_VendorList f = new frm_VendorList();
            f.LoadRecords();
            OpenChildForm(f);
        }

        private void button8_Click(object sender, EventArgs e)
        {
            frmUserAccount frm = new frmUserAccount(this);
            frm.txtU.Text = lblUser.Text;
            OpenChildForm(frm);
        }

        private void button6_Click(object sender, EventArgs e)
        {
            frmRecords frm = new frmRecords();
            frm.LoadCriticalItems();
            frm.LoadInventory();
            frm.LoadStockHistory();
            frm.CancelledOrder();
            OpenChildForm(frm);
        }

        private void btnSalesHistory_Click(object sender, EventArgs e)
        {
            frmSoldItems frm = new frmSoldItems();
            frm.ShowDialog();
        }

        private void button7_Click(object sender, EventArgs e)
        {
            frmStoreSetting frm = new frmStoreSetting();
            frm.LoadRecord();
            frm.ShowDialog();
        }

        private void button1_Click(object sender, EventArgs e)
            => MyDashbord();

        private void button2_Click(object sender, EventArgs e)
        {
            frmAdjustment f = new frmAdjustment(this);
            f.LoadRecords();
            f.txtUser.Text = lblUser.Text;
            f.referenceNo();
            f.ShowDialog();
        }

        private void button9_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("Logout Application?", "Logout",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                this.Hide();
                frmUserLogin login = new frmUserLogin();
                login.Show();
            }
        }

        // ================= PUBLIC METHODS FOR SUB-FORMS =================

        public void LoadCart()
        {
            try
            {
                if (dataGridView1 == null) return;

                dataGridView1.Rows.Clear();
                int i = 0;
                double total = 0;
                double discount = 0;

                using (SQLiteConnection cn = new SQLiteConnection(DBConnection.MyConnection()))
                {
                    cn.Open();
                    string query = "SELECT c.id, c.pcode, p.pdesc, c.price, c.qty, c.disc, c.total " +
                                   "FROM tblCart1 AS c INNER JOIN TblProduct1 AS p ON c.pcode = p.pcode " +
                                   "WHERE status = 'Pending' AND transno = @transno";

                    using (SQLiteCommand cm = new SQLiteCommand(query, cn))
                    {
                        cm.Parameters.AddWithValue("@transno", lblTransno.Text);
                        using (SQLiteDataReader dr = cm.ExecuteReader())
                        {
                            while (dr.Read())
                            {
                                i++;
                                // Using Convert.ToDouble is safer than Parse for database values
                                double rowTotal = Convert.ToDouble(dr["total"]);
                                double rowDisc = Convert.ToDouble(dr["disc"]);

                                total += rowTotal;
                                discount += rowDisc;

                                dataGridView1.Rows.Add(i, dr["id"].ToString(), dr["pcode"].ToString(),
                                    dr["pdesc"].ToString(), dr["price"].ToString(), dr["qty"].ToString(),
                                    dr["disc"].ToString(), dr["total"].ToString());
                            }
                        }
                    }
                }
                lblTotal.Text = total.ToString("#,##0.00");
                lblDisplayTotal.Text = total.ToString("#,##0.00");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        public void GetTransNo()
        {
            try
            {
                string sdate = DateTime.Now.ToString("yyyyMMdd");
                string transno;
                int count;

                using (SQLiteConnection cn = new SQLiteConnection(DBConnection.MyConnection()))
                {
                    cn.Open();
                    string sql = "SELECT transno FROM tblCart1 WHERE transno LIKE @sdate ORDER BY id DESC LIMIT 1";
                    using (SQLiteCommand cm = new SQLiteCommand(sql, cn))
                    {
                        cm.Parameters.AddWithValue("@sdate", sdate + "%");
                        object result = cm.ExecuteScalar();

                        if (result != null && result != DBNull.Value)
                        {
                            transno = result.ToString();
                            // Fix: Ensure we don't crash if string is too short
                            if (transno.Length >= 12)
                            {
                                count = int.Parse(transno.Substring(8, 4));
                                lblTransno.Text = sdate + (count + 1).ToString("D4");
                            }
                            else
                            {
                                // Fallback if format is weird
                                lblTransno.Text = sdate + "1001";
                            }
                        }
                        else
                        {
                            lblTransno.Text = sdate + "1001";
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        // ================= MISC =================
        private void guna2ControlBox1_Click(object sender, EventArgs e) => Application.Exit();

        private void Form1_Load(object sender, EventArgs e) { }
        private void panel2_Paint(object sender, PaintEventArgs e) { }
        private void panel3_Paint(object sender, PaintEventArgs e) { }
        private void lblRole_Click(object sender, EventArgs e) { }
    }
}