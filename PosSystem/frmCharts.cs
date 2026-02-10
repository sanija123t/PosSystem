using System;
using System.Data;
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
        }

        private void pictureBox2_Click(object sender, EventArgs e)
        {
            Dispose();
        }

        // =========================
        // 🔹 ELITE-LEVEL ASYNC CHART LOAD
        // =========================
        public async Task LoadCardSoldAsync(string sql)
        {
            chart1.Series.Clear();
            chart1.Titles.Clear();

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
                            LabelFormat = "C2", // ✅ Local currency format
                            ["PieLabelStyle"] = "Outside",
                            BorderColor = System.Drawing.Color.White,
                            BorderWidth = 1
                        };

                        chart1.Series.Add(series);

                        if (dt.Rows.Count > 0)
                        {
                            chart1.DataSource = dt;
                            series.XValueMember = "pdesc";
                            series.YValueMembers = "total";

                            // Highlight top-selling item (fixed Exploded property)
                            decimal maxValue = 0;
                            int maxIndex = -1;
                            for (int i = 0; i < dt.Rows.Count; i++)
                            {
                                if (decimal.TryParse(dt.Rows[i]["total"].ToString(), out decimal val))
                                {
                                    if (val > maxValue)
                                    {
                                        maxValue = val;
                                        maxIndex = i;
                                    }
                                }
                            }

                            if (maxIndex >= 0)
                                series.Points[maxIndex]["Exploded"] = "true"; // ✅ Correct way

                            chart1.Titles.Add("Top Selling Items (Revenue)");
                        }
                        else
                        {
                            // No data → show dummy point
                            dt.Rows.Add("No Data", 0);
                            chart1.DataSource = dt;
                            series.XValueMember = "pdesc";
                            series.YValueMembers = "total";
                            chart1.Titles.Add("No Data Available for Selected Range");
                        }

                        chart1.DataBind();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Chart Error: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void chart1_Click(object sender, EventArgs e)
        {
        }
    }
}