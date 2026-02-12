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
            // ELITE Performance
            chart1.AntiAliasing = AntiAliasingStyles.All;
            chart1.TextAntiAliasingQuality = TextAntiAliasingQuality.High;
        }

        private void pictureBox2_Click(object sender, EventArgs e)
        {
            this.Dispose();
        }

        public async Task LoadCardSoldAsync(string sql)
        {
            chart1.Series.Clear();
            chart1.Titles.Clear();
            chart1.Legends.Clear();
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

                        // Legend setup for Right-Side descriptions
                        Legend leg = new Legend("MainLegend")
                        {
                            Docking = Docking.Right,
                            Alignment = StringAlignment.Center,
                            BackColor = Color.Transparent,
                            Font = new Font("Segoe UI", 9, FontStyle.Regular),
                            Title = "PRODUCT LIST",
                            TitleFont = new Font("Segoe UI", 9, FontStyle.Bold)
                        };
                        chart1.Legends.Add(leg);

                        if (chart1.ChartAreas.Count > 0)
                        {
                            chart1.ChartAreas[0].Area3DStyle.Enable3D = true;
                            chart1.ChartAreas[0].Area3DStyle.Inclination = 45;
                        }

                        chart1.Series.Add(series);

                        if (dt != null && dt.Rows.Count > 0)
                        {
                            chart1.DataSource = dt;
                            series.XValueMember = "pdesc";
                            series.YValueMembers = "total";
                            chart1.DataBind();

                            foreach (DataPoint p in series.Points)
                            {
                                p.LegendText = "#VALX (#PERCENT{P0})";
                            }

                            var mainTitle = chart1.Titles.Add("TOP SELLING ITEMS");
                            mainTitle.Font = new Font("Segoe UI", 12, FontStyle.Bold);
                        }
                        else
                        {
                            // Enterprise Gray-Out Mode (Mismatch/No Data Safety)
                            chart1.Legends["MainLegend"].Enabled = false;
                            series.Points.AddXY("No Data Available", 1);
                            series.Points[0].Color = Color.LightGray;
                            series.Points[0].BorderColor = Color.DarkGray;
                            series.IsValueShownAsLabel = false;

                            var emptyTitle = chart1.Titles.Add("NO DATA FOR SELECTED PERIOD");
                            emptyTitle.ForeColor = Color.DimGray;
                            emptyTitle.Font = new Font("Segoe UI", 11, FontStyle.Italic);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("BI Engine Error: " + ex.Message, "System Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void chart1_Click(object sender, EventArgs e)
        {
            // Junk line for designer
        }

        private void panel1_Paint(object sender, PaintEventArgs e)
        {
            // Junk line for designer
        }
    }
}