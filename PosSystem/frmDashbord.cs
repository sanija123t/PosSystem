using System;
using System.Data;
using System.Data.SQLite;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;

namespace PosSystem
{
    public partial class frmDashbord : Form
    {
        public frmDashbord()
        {
            InitializeComponent();
            this.KeyPreview = true;
        }

        private async void frmDashbord_Load(object sender, EventArgs e)
        {
            await LoadChartAsync();
            await LoadStatisticsAsync();
            CenterPanel();
        }

        private void frmDashbord_Resize(object sender, EventArgs e)
        {
            CenterPanel();
        }

        private void CenterPanel()
        {
            if (panel1 != null)
                panel1.Left = (this.ClientSize.Width - panel1.Width) / 2;
        }

        public async Task LoadChartAsync()
        {
            try
            {
                using (var cn = new SQLiteConnection(DBConnection.MyConnection()))
                {
                    await cn.OpenAsync();
                    int currentYear = DateTime.Now.Year;
                    DateTime startOfYear = new DateTime(currentYear, 1, 1);
                    DateTime endOfYear = new DateTime(currentYear, 12, 31, 23, 59, 59);

                    string query = @"
                        SELECT 
                            CASE strftime('%m', sdate)
                                WHEN '01' THEN 'Jan' WHEN '02' THEN 'Feb' WHEN '03' THEN 'Mar'
                                WHEN '04' THEN 'Apr' WHEN '05' THEN 'May' WHEN '06' THEN 'Jun'
                                WHEN '07' THEN 'Jul' WHEN '08' THEN 'Aug' WHEN '09' THEN 'Sep'
                                WHEN '10' THEN 'Oct' WHEN '11' THEN 'Nov' WHEN '12' THEN 'Dec'
                            END AS MonthName,
                            SUM(total) AS total
                        FROM tblCart1
                        WHERE status = 'Sold' AND sdate BETWEEN @start AND @end
                        GROUP BY MonthName
                        ORDER BY strftime('%m', sdate) ASC;";

                    using (var cmd = new SQLiteCommand(query, cn))
                    {
                        cmd.Parameters.AddWithValue("@start", startOfYear.ToString("yyyy-MM-dd HH:mm:ss"));
                        cmd.Parameters.AddWithValue("@end", endOfYear.ToString("yyyy-MM-dd HH:mm:ss"));

                        using (var da = new SQLiteDataAdapter(cmd))
                        {
                            var dt = new DataTable();
                            await Task.Run(() => da.Fill(dt));

                            chart1.Series.Clear();
                            chart1.Titles.Clear();
                            chart1.Legends.Clear();

                            Legend leg = new Legend("MainLegend") { BackColor = Color.Transparent };
                            chart1.Legends.Add(leg);

                            var series = chart1.Series.Add("Yearly Sales");
                            series.ChartType = SeriesChartType.Doughnut;
                            series.IsValueShownAsLabel = true;
                            series.LabelFormat = "{0:C0}";

                            if (dt.Rows.Count > 0)
                            {
                                chart1.DataSource = dt;
                                series.XValueMember = "MonthName";
                                series.YValueMembers = "total";
                                chart1.Titles.Add($"ANNUAL SALES PERFORMANCE ({currentYear})");
                            }
                            else
                            {
                                series.Points.AddXY("No Data", 1);
                                series.Points[0].Color = Color.Gainsboro;
                                chart1.Titles.Add($"NO REVENUE DATA RECORDED FOR {currentYear}");
                            }
                            chart1.DataBind();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Chart Error: " + ex.Message);
            }
        }

        public async Task LoadStatisticsAsync()
        {
            try
            {
                using (var cn = new SQLiteConnection(DBConnection.MyConnection()))
                {
                    await cn.OpenAsync();

                    // 1. Daily Sales
                    string sdate = DateTime.Now.ToString("yyyy-MM-dd");
                    using (var cmd = new SQLiteCommand("SELECT IFNULL(SUM(total), 0) FROM tblCart1 WHERE sdate LIKE @sdate AND status = 'Sold'", cn))
                    {
                        cmd.Parameters.AddWithValue("@sdate", sdate + "%");
                        var res = await cmd.ExecuteScalarAsync();
                        if (lblDailySales != null) lblDailySales.Text = Convert.ToDouble(res).ToString("#,##0.00");
                    }

                    // 2. Product Line
                    using (var cmd = new SQLiteCommand("SELECT COUNT(*) FROM tblProduct", cn))
                    {
                        var res = await cmd.ExecuteScalarAsync();
                        if (lblProduct != null) lblProduct.Text = res.ToString();
                    }

                    // 3. Stock on Hand (Updated to lblStock)
                    using (var cmd = new SQLiteCommand("SELECT IFNULL(SUM(qty), 0) FROM tblProduct", cn))
                    {
                        var res = await cmd.ExecuteScalarAsync();
                        if (lblStock != null) lblStock.Text = res.ToString();
                    }

                    // 4. Critical Items
                    using (var cmd = new SQLiteCommand("SELECT COUNT(*) FROM tblProduct WHERE qty <= reorder", cn))
                    {
                        var res = await cmd.ExecuteScalarAsync();
                        if (lblCriticalItems != null) lblCriticalItems.Text = res.ToString();
                    }
                }
            }
            catch { /* Silent fail */ }
        }

        // ============================================================
        // 🔹 REQUIRED BY DESIGNER (Do Not Delete)
        // ============================================================
        private void panel1_Paint(object sender, PaintEventArgs e) { }
        private void panel2_Paint(object sender, PaintEventArgs e) { }
        private void label3_Click(object sender, EventArgs e) { }
        private void label11_Click(object sender, EventArgs e) { }
        private void chart1_Click(object sender, EventArgs e) { }
    }
}