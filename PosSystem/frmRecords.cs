using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Data.SQLite;
using System.Windows.Forms.DataVisualization.Charting;

namespace PosSystem
{
    public partial class frmRecords : Form
    {
        SQLiteConnection cn;
        SQLiteCommand cm;
        SQLiteDataReader dr;
        string stitle = "PosSystem";

        public frmRecords()
        {
            InitializeComponent();
            cn = new SQLiteConnection(DBConnection.MyConnection());
        }

        private void pictureBox2_Click(object sender, EventArgs e)
        {
            this.Dispose();
        }

        private void frmRecords_Load(object sender, EventArgs e)
        {
        }

        public void LoadRecords()
        {
            try
            {
                int i = 0;
                dataGridView1.Rows.Clear();
                cn.Open();

                string query = "";
                if (cdTopSelling.Text == "Short by Qty")
                {
                    query = "SELECT pcode, pdesc, IFNULL(SUM(qty),0) AS qty, IFNULL(SUM(total),0) AS total FROM vwSoldItems WHERE sdate BETWEEN @d1 AND @d2 AND status LIKE 'sold' GROUP BY pcode, pdesc ORDER BY qty DESC LIMIT 10";
                }
                else if (cdTopSelling.Text == "Short by Total Amount")
                {
                    query = "SELECT pcode, pdesc, IFNULL(SUM(qty),0) AS qty, IFNULL(SUM(total),0) AS total FROM vwSoldItems WHERE sdate BETWEEN @d1 AND @d2 AND status LIKE 'sold' GROUP BY pcode, pdesc ORDER BY total DESC LIMIT 10";
                }

                if (!string.IsNullOrEmpty(query))
                {
                    cm = new SQLiteCommand(query, cn);
                    cm.Parameters.AddWithValue("@d1", dateTimePicker1.Value.ToString("yyyy-MM-dd HH:mm:ss"));
                    cm.Parameters.AddWithValue("@d2", dateTimePicker2.Value.ToString("yyyy-MM-dd HH:mm:ss"));
                    dr = cm.ExecuteReader();
                    while (dr.Read())
                    {
                        i++;
                        dataGridView1.Rows.Add(i, dr["pcode"].ToString(), dr["pdesc"].ToString(), dr["qty"].ToString(), Double.Parse(dr["total"].ToString()).ToString("#,##0.00"));
                    }
                    dr.Close();
                }
                cn.Close();
            }
            catch (Exception ex)
            {
                if (cn.State == ConnectionState.Open) cn.Close();
                MessageBox.Show(ex.Message, stitle, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnLoad_Click(object sender, EventArgs e)
        {
            LoadRecords();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            try
            {
                dataGridView2.Rows.Clear();
                int i = 0;
                cn.Open();
                cm = new SQLiteCommand("SELECT c.pcode, p.pdesc, c.price, SUM(c.qty) AS tot_qty, SUM(c.disc) AS tot_disc, SUM(c.total) AS total FROM tblCart1 AS c INNER JOIN TblProduct1 AS p ON c.pcode = p.pcode WHERE status LIKE 'Sold' AND sdate BETWEEN @d1 AND @d2 GROUP BY c.pcode, p.pdesc, c.price", cn);
                cm.Parameters.AddWithValue("@d1", dateTimePicker4.Value.ToString("yyyy-MM-dd HH:mm:ss"));
                cm.Parameters.AddWithValue("@d2", dateTimePicker3.Value.ToString("yyyy-MM-dd HH:mm:ss"));
                dr = cm.ExecuteReader();
                while (dr.Read())
                {
                    i++;
                    dataGridView2.Rows.Add(i, dr["pcode"].ToString(), dr["pdesc"].ToString(), Double.Parse(dr["price"].ToString()).ToString("#,##0.00"), dr["tot_qty"].ToString(), dr["tot_disc"].ToString(), Double.Parse(dr["total"].ToString()).ToString("#,##0.00"));
                }
                dr.Close();
                cn.Close();

                cn.Open();
                cm = new SQLiteCommand("SELECT IFNULL(SUM(total),0) FROM tblCart1 WHERE status LIKE 'Sold' AND sdate BETWEEN @d1 AND @d2", cn);
                cm.Parameters.AddWithValue("@d1", dateTimePicker4.Value.ToString("yyyy-MM-dd HH:mm:ss"));
                cm.Parameters.AddWithValue("@d2", dateTimePicker3.Value.ToString("yyyy-MM-dd HH:mm:ss"));
                lblTotal.Text = Double.Parse(cm.ExecuteScalar().ToString()).ToString("#,##0.00");
                cn.Close();
            }
            catch (Exception ex)
            {
                if (cn.State == ConnectionState.Open) cn.Close();
                MessageBox.Show(ex.Message, stitle, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        public void LoadCriticalItems()
        {
            try
            {
                dataGridView3.Rows.Clear();
                int i = 0;
                cn.Open();
                cm = new SQLiteCommand("SELECT * FROM vwCriticalItems", cn);
                dr = cm.ExecuteReader();
                while (dr.Read())
                {
                    i++;
                    dataGridView3.Rows.Add(i, dr[0].ToString(), dr[1].ToString(), dr[2].ToString(), dr[3].ToString(), dr[4].ToString(), dr[5].ToString(), dr[6].ToString(), dr[7].ToString());
                }
                dr.Close();
                cn.Close();
            }
            catch (Exception ex)
            {
                if (cn.State == ConnectionState.Open) cn.Close();
                MessageBox.Show(ex.Message, stitle, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        public void LoadInventory()
        {
            try
            {
                int i = 0;
                dataGridView4.Rows.Clear();
                cn.Open();
                cm = new SQLiteCommand("SELECT p.pcode, p.barcode, p.pdesc, b.brand, c.category, p.price, p.qty, p.reorder FROM TblProduct1 AS p INNER JOIN BrandTbl AS b ON p.bid=b.id INNER JOIN TblCategory AS c ON p.cid = c.id", cn);
                dr = cm.ExecuteReader();
                while (dr.Read())
                {
                    i++;
                    dataGridView4.Rows.Add(i, dr["pcode"].ToString(), dr["barcode"].ToString(), dr["pdesc"].ToString(), dr["brand"].ToString(), dr["category"].ToString(), dr["price"].ToString(), dr["reorder"].ToString(), dr["qty"].ToString());
                }
                dr.Close();
                cn.Close();
            }
            catch (Exception ex)
            {
                if (cn.State == ConnectionState.Open) cn.Close();
                MessageBox.Show(ex.Message, stitle, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        public void CancelledOrder()
        {
            try
            {
                int i = 0;
                dataGridView5.Rows.Clear();
                cn.Open();
                cm = new SQLiteCommand("SELECT * FROM vwCancelledOrder WHERE sdate BETWEEN @d1 AND @d2", cn);
                cm.Parameters.AddWithValue("@d1", dateTimePicker5.Value.ToString("yyyy-MM-dd HH:mm:ss"));
                cm.Parameters.AddWithValue("@d2", dateTimePicker6.Value.ToString("yyyy-MM-dd HH:mm:ss"));
                dr = cm.ExecuteReader();
                while (dr.Read())
                {
                    i++;
                    dataGridView5.Rows.Add(i, dr["transno"].ToString(), dr["pcode"].ToString(), dr["pdesc"].ToString(), dr["price"].ToString(), dr["qty"].ToString(), dr["total"].ToString(), dr["sdate"].ToString(), dr["voidby"].ToString(), dr["cancelledby"].ToString(), dr["reason"].ToString(), dr["action"].ToString());
                }
                dr.Close();
                cn.Close();
            }
            catch (Exception ex)
            {
                if (cn.State == ConnectionState.Open) cn.Close();
                MessageBox.Show(ex.Message, stitle, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        public void LoadStockHistory()
        {
            try
            {
                int i = 0;
                dataGridView6.Rows.Clear();
                cn.Open();
                cm = new SQLiteCommand("SELECT * FROM tblStockIn WHERE strftime('%Y-%m-%d', sdate) BETWEEN @d1 AND @d2 AND status LIKE 'Done'", cn);
                cm.Parameters.AddWithValue("@d1", dateTimePicker8.Value.ToString("yyyy-MM-dd"));
                cm.Parameters.AddWithValue("@d2", dateTimePicker7.Value.ToString("yyyy-MM-dd"));
                dr = cm.ExecuteReader();
                while (dr.Read())
                {
                    i++;
                    dataGridView6.Rows.Add(i, dr[0].ToString(), dr[1].ToString(), dr[2].ToString(), dr[3].ToString(), dr[4].ToString(), dr[5].ToString(), dr[6].ToString());
                }
                dr.Close();
                cn.Close();
            }
            catch (Exception ex)
            {
                if (cn.State == ConnectionState.Open) cn.Close();
                MessageBox.Show(ex.Message, stitle, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        public void LoadChart()
        {
            try
            {
                DataTable dt = new DataTable();
                cn.Open();
                string sql = "";
                if (cdTopSelling.Text == "Short by Qty")
                {
                    sql = "SELECT pcode, IFNULL(SUM(qty),0) AS qty FROM vwSoldItems WHERE sdate BETWEEN @d1 AND @d2 AND status LIKE 'sold' GROUP BY pcode ORDER BY qty DESC LIMIT 10";
                }
                else if (cdTopSelling.Text == "Short by Total Amount")
                {
                    sql = "SELECT pcode, IFNULL(SUM(total),0) AS total FROM vwSoldItems WHERE sdate BETWEEN @d1 AND @d2 AND status LIKE 'sold' GROUP BY pcode ORDER BY total DESC LIMIT 10";
                }

                if (!string.IsNullOrEmpty(sql))
                {
                    SQLiteDataAdapter da = new SQLiteDataAdapter(sql, cn);
                    da.SelectCommand.Parameters.AddWithValue("@d1", dateTimePicker1.Value.ToString("yyyy-MM-dd HH:mm:ss"));
                    da.SelectCommand.Parameters.AddWithValue("@d2", dateTimePicker2.Value.ToString("yyyy-MM-dd HH:mm:ss"));
                    da.Fill(dt);
                    chart1.DataSource = dt;
                    Series series = chart1.Series[0];
                    series.ChartType = SeriesChartType.Doughnut;
                    series.Name = "TOP SELLING";
                    chart1.Series[0].XValueMember = "pcode";
                    if (cdTopSelling.Text == "Short by Qty") { chart1.Series[0].YValueMembers = "qty"; }
                    if (cdTopSelling.Text == "Short by Total Amount") { chart1.Series[0].YValueMembers = "total"; }
                    chart1.Series[0].IsValueShownAsLabel = true;
                    chart1.Series[0].LabelFormat = "#,##0.00";
                }
                cn.Close();
            }
            catch (Exception ex)
            {
                if (cn.State == ConnectionState.Open) cn.Close();
                MessageBox.Show(ex.Message, stitle, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void linkLabel4_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            frmInventoryReport f = new frmInventoryReport();
            string d1 = dateTimePicker1.Value.ToString("yyyy-MM-dd HH:mm:ss");
            string d2 = dateTimePicker2.Value.ToString("yyyy-MM-dd HH:mm:ss");

            if (cdTopSelling.Text == "Short by Qty")
            {
                f.LoadTopSelling("SELECT pcode, pdesc, SUM(qty) AS qty, IFNULL(SUM(total),0) AS total FROM vwSoldItems WHERE sdate BETWEEN '" + d1 + "' AND '" + d2 + "' AND status LIKE 'sold' GROUP BY pcode, pdesc ORDER BY qty DESC LIMIT 10", "From : " + dateTimePicker1.Value.ToShortDateString() + " To : " + dateTimePicker2.Value.ToShortDateString(), "TOP SELLING ITEMS SHORT BY QTY");
            }
            else if (cdTopSelling.Text == "Short by Total Amount")
            {
                f.LoadTopSelling("SELECT pcode, pdesc, IFNULL(SUM(qty),0) AS qty, IFNULL(SUM(total),0) AS total FROM vwSoldItems WHERE sdate BETWEEN '" + d1 + "' AND '" + d2 + "' AND status LIKE 'Sold' GROUP BY pcode, pdesc ORDER BY total DESC LIMIT 10", "From : " + dateTimePicker1.Value.ToShortDateString() + " To : " + dateTimePicker2.Value.ToShortDateString(), "TOP SELLING ITEMS SHORT BY TOTAL AMOUNT");
            }
            f.ShowDialog();
        }

        private void linkLabel3_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            frmInventoryReport f = new frmInventoryReport();
            f.LoadSoldItems("SELECT c.pcode, p.pdesc, c.price, SUM(c.qty) AS tot_qty, SUM(c.disc) AS tot_disc, SUM(c.total) AS total FROM tblCart1 AS c INNER JOIN TblProduct1 AS p ON c.pcode = p.pcode WHERE status LIKE 'Sold' AND sdate BETWEEN '" + dateTimePicker4.Value.ToString("yyyy-MM-dd HH:mm:ss") + "' AND '" + dateTimePicker3.Value.ToString("yyyy-MM-dd HH:mm:ss") + "' GROUP BY c.pcode, p.pdesc, c.price", " From : " + dateTimePicker4.Value.ToShortDateString() + " To : " + dateTimePicker3.Value.ToShortDateString());
            f.ShowDialog();
        }

        private void linkLabel5_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            if (string.IsNullOrEmpty(cdTopSelling.Text))
            {
                MessageBox.Show("Please select from the dropdown list.", stitle, MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                return;
            }
            LoadRecords();
            LoadChart();
        }

        private void cdTopSelling_KeyPress(object sender, KeyPressEventArgs e)
        {
            e.Handled = true;
        }

        private void linkLabel6_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            button1_Click(sender, e);
        }

        private void linkLabel7_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            frmCharts f = new frmCharts();
            f.lblTitel.Text = "Sold Items [" + dateTimePicker4.Value.ToShortDateString() + " - " + dateTimePicker3.Value.ToShortDateString() + "]";
            f.LoadCardSold("SELECT p.pdesc, SUM(c.total) AS total FROM tblCart1 AS c INNER JOIN TblProduct1 AS p ON c.pcode = p.pcode WHERE status LIKE 'Sold' AND sdate BETWEEN '" + dateTimePicker4.Value.ToString("yyyy-MM-dd HH:mm:ss") + "' AND '" + dateTimePicker3.Value.ToString("yyyy-MM-dd HH:mm:ss") + "' GROUP BY p.pdesc ORDER BY total DESC");
            f.ShowDialog();
        }

        private void linkLabel2_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            frmInventoryReport frm = new frmInventoryReport();
            string param = "Date Covered: " + dateTimePicker8.Value.ToShortDateString() + " - " + dateTimePicker7.Value.ToShortDateString();
            frm.LoadStockReport("SELECT * FROM tblStockIn WHERE strftime('%Y-%m-%d', sdate) BETWEEN '" + dateTimePicker8.Value.ToString("yyyy-MM-dd") + "' AND '" + dateTimePicker7.Value.ToString("yyyy-MM-dd") + "' AND status LIKE 'Done'", param);
            frm.ShowDialog();
        }

        private void linkLabel8_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            LoadStockHistory();
        }

        private void linkLabel9_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            CancelledOrder();
        }

        private void linkLabel10_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            frmInventoryReport f = new frmInventoryReport();
            string param = "Date covered: " + dateTimePicker5.Value.ToShortDateString() + " - " + dateTimePicker6.Value.ToShortDateString();
            f.LoadCancelReport("SELECT * FROM vwCancelledOrder WHERE sdate BETWEEN '" + dateTimePicker5.Value.ToString("yyyy-MM-dd HH:mm:ss") + "' AND '" + dateTimePicker6.Value.ToString("yyyy-MM-dd HH:mm:ss") + "'", param);
            f.ShowDialog();
        }

        private void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            frmInventoryReport frm = new frmInventoryReport();
            frm.LoadReport();
            frm.ShowDialog();
        }

        private void dataGridView3_CellContentClick(object sender, DataGridViewCellEventArgs e) { }
        private void dataGridView4_CellContentClick(object sender, DataGridViewCellEventArgs e) { }
        private void dataGridView5_CellContentClick(object sender, DataGridViewCellEventArgs e) { }
        private void dataGridView1_CellContentClick(object sender, DataGridViewCellEventArgs e) { }
        private void tabControl1_SelectedIndexChanged(object sender, EventArgs e) { }
        private void panel8_Paint(object sender, PaintEventArgs e) { }
        private void panel2_Paint(object sender, PaintEventArgs e) { }
        private void chart1_Click(object sender, EventArgs e) { }
        private void panel1_Paint(object sender, PaintEventArgs e) { }
        private void button2_Click(object sender, EventArgs e) { }
        private void button3_Click(object sender, EventArgs e) { }
        private void tabPage6_Click(object sender, EventArgs e) { }
    }
}