using System;
using System.Data.SQLite;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using Tulpep.NotificationWindow;

namespace PosSystem
{
    public partial class Form1 : Form
    {
        public string _pass;
        public string _user;
        public string _role;
        public string _name;

        private frmRecords _frmRecords;
        private frmDashbord _frmDashboard;

        public Form1()
        {
            InitializeComponent();
            NotifyCriticalItems();
        }

        private async void Form1_Load(object sender, EventArgs e)
        {
            // async dashboard load after form is fully created
            await MyDashbordAsync();
        }

        #region User Session
        public void SetUserSession(string user, string role, string name)
        {
            _user = user;
            _role = role;
            _name = name;

            lblUserName.Text = _name;
            lblRole.Text = _role;

            bool isAdmin = _role == "Administrator";
            btnManageBrand.Enabled = isAdmin;
            btnManageCategory.Enabled = isAdmin;
            btnManageProduct.Enabled = isAdmin;
            btnVendor.Enabled = isAdmin;
            btnUsersettings.Enabled = isAdmin;
            btnStoreSettings.Enabled = isAdmin;
        }
        #endregion

        #region Child Form Management
        private void OpenChildForm(Form childForm)
        {
            if (panel3.Controls.Count > 0)
                panel3.Controls[0].Dispose();

            panel3.Controls.Clear();
            childForm.TopLevel = false;
            childForm.FormBorderStyle = FormBorderStyle.None;
            childForm.Dock = DockStyle.Fill;
            panel3.Controls.Add(childForm);
            panel3.Tag = childForm;
            childForm.BringToFront();
            childForm.Show();
        }
        #endregion

        #region Dashboard
        public async Task MyDashbordAsync()
        {
            try
            {
                if (_frmDashboard != null)
                    _frmDashboard.Dispose();

                _frmDashboard = new frmDashbord
                {
                    TopLevel = false,
                    Dock = DockStyle.Fill
                };

                panel3.Controls.Clear();
                panel3.Controls.Add(_frmDashboard);

                var dailySalesTask = Task.Run(() => DBConnection.DailySales());
                var productLineTask = Task.Run(() => DBConnection.ProductLine());
                var stockTask = Task.Run(() => DBConnection.StockOnHand());
                var criticalTask = Task.Run(() => DBConnection.CriticalItems());

                await Task.WhenAll(dailySalesTask, productLineTask, stockTask, criticalTask);

                _frmDashboard.lblDailySales.Text = dailySalesTask.Result.ToString("#,##0.00");
                _frmDashboard.lblProduct.Text = productLineTask.Result.ToString("#,##0");
                _frmDashboard.lblStock.Text = stockTask.Result.ToString("#,##0");
                _frmDashboard.lblCriticalItems.Text = criticalTask.Result.ToString("#,##0");

                _frmDashboard.BringToFront();
                _frmDashboard.Show();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading dashboard: " + ex.Message, "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
        #endregion

        #region Critical Item Notification
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
        #endregion

        #region Button Handlers
        private void btnBrand_Click(object sender, EventArgs e) => OpenChildForm(new frmBrandList());

        private void button4_Click(object sender, EventArgs e)
        {
            var frm = new frmCategoryList();
            frm.LoadCategory();
            OpenChildForm(frm);
        }

        private void btnProduct_Click(object sender, EventArgs e)
        {
            var frm = new frmProduct_List();
            frm.LoadRecords();
            OpenChildForm(frm);
        }

        private void btnStock_Click(object sender, EventArgs e)
        {
            var frm = new frmStockin(DBConnection.MyConnection());
            frm.ShowDialog();
        }

        private void btnVendor_Click(object sender, EventArgs e)
        {
            var f = new frm_VendorList();
            f.LoadRecords();
            OpenChildForm(f);
        }

        private void button8_Click(object sender, EventArgs e)
        {
            var frm = new frmUserAccount(this);
            frm.txtU.Text = _user;
            OpenChildForm(frm);
        }

        private void button6_Click(object sender, EventArgs e)
        {
            if (_frmRecords == null || _frmRecords.IsDisposed)
                _frmRecords = new frmRecords();
            OpenChildForm(_frmRecords);
        }

        private void btnSalesHistory_Click(object sender, EventArgs e)
        {
            var frm = new frmSoldItems { suser = _user };
            frm.ShowDialog();
        }

        private void button7_Click(object sender, EventArgs e)
        {
            var frm = new frmStoreSetting();
            frm.LoadRecord();
            frm.ShowDialog();
        }

        private void button1_Click(object sender, EventArgs e) => _ = MyDashbordAsync();

        private async void button2_Click(object sender, EventArgs e)
        {
            // ✅ Correctly create instance and call async method
            var frm = new frmAdjustment(this);
            await frm.LoadRecordsAsync();
            frm.txtUser.Text = _user;
            string refNo = frm.txtRef.Text;
            frm.ShowDialog();
        }

        private void button9_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("Logout Application?", "Logout",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                Hide();
                new frmUserLogin().Show();
            }
        }
        #endregion

        #region Utility Methods
        public void LoadCart()
        {
            try
            {
                if (dataGridView1 == null) return;
                dataGridView1.Rows.Clear();

                int i = 0;
                double total = 0;

                using (SQLiteConnection cn = new SQLiteConnection(DBConnection.MyConnection()))
                {
                    cn.Open();
                    string query = @"SELECT c.id, c.pcode, p.pdesc, c.price, c.qty, c.disc, c.total
                                     FROM tblCart1 c
                                     INNER JOIN TblProduct1 p ON c.pcode = p.pcode
                                     WHERE status='Pending' AND transno=@transno";

                    using (SQLiteCommand cm = new SQLiteCommand(query, cn))
                    {
                        cm.Parameters.AddWithValue("@transno", lblTransno.Text);
                        using (SQLiteDataReader dr = cm.ExecuteReader())
                        {
                            while (dr.Read())
                            {
                                i++;
                                total += Convert.ToDouble(dr["total"]);

                                dataGridView1.Rows.Add(i, dr["id"], dr["pcode"], dr["pdesc"],
                                    dr["price"], dr["qty"], dr["disc"], dr["total"]);
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
                            string transno = result.ToString();
                            int count = int.Parse(transno.Substring(8, 4));
                            lblTransno.Text = sdate + (count + 1).ToString("D4");
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
        #endregion

        #region Form & Panel Events
        private void guna2ControlBox1_Click(object sender, EventArgs e) => Application.Exit();
        private void panel3_Paint(object sender, PaintEventArgs e) { }
        private void lblRole_Click(object sender, EventArgs e) { }
        private void lblName_Click(object sender, EventArgs e) { }
        #endregion

        private void btnStockMain_Click(object sender, EventArgs e)
        {

        }

        private void btnProductMain_Click(object sender, EventArgs e)
        {

        }

        private void btnAuditMain_Click(object sender, EventArgs e)
        {

        }

        private void btnPOSMain_Click(object sender, EventArgs e)
        {

        }

        private void btnSettingsMain_Click(object sender, EventArgs e)
        {

        }

        private void btnMaintenanceMain_Click(object sender, EventArgs e)
        {

        }

        private void panel2_Paint(object sender, PaintEventArgs e)
        {

        }
    }
}