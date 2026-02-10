using System;
using System.Data;
using System.Data.SQLite;
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
            KeyPreview = true;
        }

        private async void frmDashbord_Load(object sender, EventArgs e)
        {
            await LoadChartAsync();
            CenterPanel();
            // Example: lblDailySales.Text = await DBConnection.GetDailySalesAsync();
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

        private void panel1_Paint(object sender, PaintEventArgs e) { }
        private void panel2_Paint(object sender, PaintEventArgs e) { }
        private void label3_Click(object sender, EventArgs e) { }
        private void label11_Click(object sender, EventArgs e) { }
        private void chart1_Click(object sender, EventArgs e) { }

        // =========================
        // 🔹 ELITE-LEVEL CHART LOAD
        // =========================
        public async Task LoadChartAsync()
        {
            try
            {
                using (var cn = new SQLiteConnection(DBConnection.MyConnection()))
                {
                    await cn.OpenAsync();

                    DateTime startOfYear = new DateTime(DateTime.Now.Year, 1, 1);
                    DateTime endOfYear = startOfYear.AddYears(1).AddSeconds(-1);

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
                            chart1.DataSource = null;

                            var series = chart1.Series.Add("Sales");
                            series.ChartType = SeriesChartType.Doughnut;
                            series.XValueMember = "MonthName";
                            series.YValueMembers = "total";
                            series.IsValueShownAsLabel = true;
                            series.LabelFormat = "C2"; // ✅ Currency format
                            series["PieLabelStyle"] = "Outside";

                            if (chart1.ChartAreas.Count > 0)
                                chart1.ChartAreas[0].Area3DStyle.Enable3D = true;

                            // Handle no data gracefully
                            if (dt.Rows.Count == 0)
                            {
                                dt.Rows.Add("No Sales", 0);
                                chart1.DataSource = dt;
                                series.YValueMembers = "total";
                            }
                            else
                            {
                                chart1.DataSource = dt;
                            }

                            chart1.Visible = true;
                            chart1.DataBind();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Dashboard Chart Error: " + ex.Message, "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
    }
}