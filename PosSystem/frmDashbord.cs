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
            KeyPreview = true;
        }

        private async void frmDashbord_Load(object sender, EventArgs e)
        {
            // Initial load of the dashboard chart
            await LoadChartAsync();
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

        // ============================================================
        // 🔹 ELITE-LEVEL DASHBOARD CHART ENGINE
        // ============================================================
        public async Task LoadChartAsync()
        {
            try
            {
                using (var cn = new SQLiteConnection(DBConnection.MyConnection()))
                {
                    await cn.OpenAsync();

                    // Elite Date Handling: Ensures precise filtering for the current year
                    DateTime startOfYear = new DateTime(DateTime.Now.Year, 1, 1);
                    DateTime endOfYear = new DateTime(DateTime.Now.Year, 12, 31, 23, 59, 59);

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
                            // Offload data filling to background thread to keep UI smooth
                            await Task.Run(() => da.Fill(dt));

                            // 1. Reset Chart to clean state
                            chart1.Series.Clear();
                            chart1.Titles.Clear();
                            chart1.Legends.Clear();
                            chart1.DataSource = null;

                            // 2. Elite Legend Configuration
                            Legend leg = new Legend("MainLegend")
                            {
                                Docking = Docking.Right,
                                Alignment = StringAlignment.Center,
                                BackColor = Color.Transparent,
                                Font = new Font("Segoe UI", 9, FontStyle.Regular),
                                ForeColor = Color.FromArgb(64, 64, 64)
                            };
                            chart1.Legends.Add(leg);

                            // 3. Series Creation & Styling
                            var series = chart1.Series.Add("Yearly Sales");
                            series.ChartType = SeriesChartType.Doughnut;
                            series.IsValueShownAsLabel = true;
                            series.LabelFormat = "{0:C0}"; // Formats as Currency without decimals
                            series.Font = new Font("Segoe UI", 8, FontStyle.Bold);
                            series.BorderColor = Color.White;
                            series.BorderWidth = 2;

                            // Advanced Doughnut Attributes
                            series["PieLabelStyle"] = "Outside";
                            series["DoughnutRadius"] = "60"; // Size of the center hole
                            series["PieLineColor"] = "Black";
                            series.Palette = ChartColorPalette.BrightPastel;

                            // 4. Chart Area Styling
                            if (chart1.ChartAreas.Count > 0)
                            {
                                var area = chart1.ChartAreas[0];
                                area.Area3DStyle.Enable3D = true;
                                area.Area3DStyle.Inclination = 15;
                                area.Area3DStyle.Rotation = 10;
                                area.BackColor = Color.Transparent;
                            }

                            // 5. Data Binding & Handling
                            if (dt.Rows.Count > 0)
                            {
                                chart1.DataSource = dt;
                                series.XValueMember = "MonthName";
                                series.YValueMembers = "total";

                                // Dynamic Legend Text showing Category and % of total
                                series.LegendText = "#VALX (#PERCENT)";

                                var title = chart1.Titles.Add("ANNUAL SALES PERFORMANCE (" + DateTime.Now.Year + ")");
                                title.Font = new Font("Segoe UI", 12, FontStyle.Bold);
                                title.ForeColor = Color.FromArgb(45, 45, 45);
                            }
                            else
                            {
                                // 🔹 PRO-GRADE EMPTY STATE (Gray-Out)
                                chart1.Legends["MainLegend"].Enabled = false;
                                series.Points.AddXY("No Data Available", 1);
                                series.Points[0].Color = Color.Gainsboro;
                                series.IsValueShownAsLabel = false;

                                var t = chart1.Titles.Add("NO REVENUE DATA RECORDED FOR " + DateTime.Now.Year);
                                t.ForeColor = Color.DimGray;
                                t.Font = new Font("Segoe UI", 10, FontStyle.Italic);
                            }

                            chart1.DataBind();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Dashboard Load Error: " + ex.Message, "System Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        // ============================================================
        // 🔹 PRESERVED DESIGNER JUNK LINES (Do Not Delete)
        // ============================================================
        private void panel1_Paint(object sender, PaintEventArgs e) { }
        private void panel2_Paint(object sender, PaintEventArgs e) { }
        private void label3_Click(object sender, EventArgs e) { }
        private void label11_Click(object sender, EventArgs e) { }
        private void chart1_Click(object sender, EventArgs e) { }
    }
}