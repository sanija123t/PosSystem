using System;
using System.Data;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;
using System.Data.SQLite;

namespace PosSystem
{
    public partial class frmCharts : Form
    {
        public frmCharts()
        {
            InitializeComponent();
            KeyPreview = true;
            // ELITE: Performance optimization for charting
            chart1.AntiAliasing = AntiAliasingStyles.All;
            chart1.TextAntiAliasingQuality = TextAntiAliasingQuality.High;
        }

        private void pictureBox2_Click(object sender, EventArgs e)
        {
            this.Dispose();
        }

        // ============================================================
        // 🔹 ELITE-LEVEL ASYNC CHART LOAD (Fixed & Optimized)
        // ============================================================
        public async Task LoadCardSoldAsync(string sql)
        {
            // 1. Reset Chart State
            chart1.Series.Clear();
            chart1.Titles.Clear();
            chart1.Legends.Clear(); // Reset legends for enterprise refresh
            chart1.DataSource = null;

            try
            {
                using (var cn = new SQLiteConnection(DBConnection.MyConnection()))
                {
                    await cn.OpenAsync();

                    using (var da = new SQLiteDataAdapter(sql, cn))
                    {
                        DataTable dt = new DataTable();
                        await Task.Run(() => da.Fill(dt));

                        // 2. Create Series with Elite Styling
                        Series series = new Series("Sold Items")
                        {
                            ChartType = SeriesChartType.Pie,
                            IsValueShownAsLabel = true,
                            LabelFormat = "{0:C0}",
                            Font = new Font("Segoe UI", 9, FontStyle.Bold),
                            ["PieLabelStyle"] = "Outside",
                            BorderColor = Color.White,
                            BorderWidth = 2,
                            Palette = ChartColorPalette.BrightPastel
                        };

                        // 3. Configure 3D Area & Legend Layout
                        if (chart1.ChartAreas.Count > 0)
                        {
                            chart1.ChartAreas[0].Area3DStyle.Enable3D = true;
                            chart1.ChartAreas[0].Area3DStyle.Inclination = 45;
                        }

                        // ENTERPRISE: Add Legend to the Right Side
                        Legend leg = new Legend("MainLegend")
                        {
                            Docking = Docking.Right,
                            Alignment = StringAlignment.Center,
                            BackColor = Color.Transparent,
                            Font = new Font("Segoe UI", 9, FontStyle.Regular)
                        };
                        chart1.Legends.Add(leg);

                        chart1.Series.Add(series);

                        if (dt != null && dt.Rows.Count > 0)
                        {
                            chart1.DataSource = dt;
                            series.XValueMember = "pdesc";
                            series.YValueMembers = "total";
                            chart1.DataBind();

                            // 4. Elite Logic: Legend setup & Top Item Explosion
                            double maxValue = 0;
                            DataPoint maxPoint = null;

                            foreach (DataPoint p in series.Points)
                            {
                                // ENTERPRISE: Link descriptions to legend
                                p.LegendText = p.AxisLabel;

                                if (p.YValues[0] > maxValue)
                                {
                                    maxValue = p.YValues[0];
                                    maxPoint = p;
                                }
                            }

                            if (maxPoint != null)
                            {
                                maxPoint["Exploded"] = "true";
                                maxPoint.LabelForeColor = Color.DarkRed;
                            }

                            var mainTitle = chart1.Titles.Add("TOP SELLING ITEMS BY REVENUE");
                            mainTitle.Font = new Font("Segoe UI", 12, FontStyle.Bold);
                        }
                        else
                        {
                            // 5. Handling No Data (Enterprise Safety)
                            // Fully gray-out the chart and hide labels/legends
                            series.Points.AddXY("No Data", 1);
                            series.Points[0].Color = Color.LightGray;
                            series.Points[0].BorderColor = Color.Gray;
                            series.IsValueShownAsLabel = false;
                            chart1.Legends["MainLegend"].Enabled = false; // Hide legend if no data

                            var emptyTitle = chart1.Titles.Add("NO SALES DATA FOUND FOR THIS PERIOD");
                            emptyTitle.ForeColor = Color.Red;
                            emptyTitle.Font = new Font("Segoe UI", 10, FontStyle.Italic);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Business Intelligence Error: " + ex.Message, "System Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void chart1_Click(object sender, EventArgs e)
        {
            // Junk line preserved for designer
        }
    }
}