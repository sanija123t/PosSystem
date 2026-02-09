using System;
using System.Data;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;
using System.Data.SQLite;

namespace PosSystem
{
    public partial class frmCharts : Form
    {
        SQLiteConnection cn;

        public frmCharts()
        {
            InitializeComponent();
            cn = new SQLiteConnection(DBConnection.MyConnection());
        }

        private void pictureBox2_Click(object sender, EventArgs e)
        {
            this.Dispose();
        }

        public void LoadCardSold(string sql)
        {
            try
            {
                chart1.Series.Clear();
                chart1.Titles.Clear();
                chart1.Titles.Add("Top Selling Items (Revenue)");

                Series series = new Series("Sold Items");
                chart1.Series.Add(series);

                using (SQLiteDataAdapter da = new SQLiteDataAdapter(sql, cn))
                {
                    DataTable dt = new DataTable();
                    da.Fill(dt);

                    if (dt.Rows.Count > 0)
                    {
                        chart1.DataSource = dt;

                        series.ChartType = SeriesChartType.Pie;
                        series.XValueMember = "pdesc";
                        series.YValueMembers = "total";

                        chart1.Series[0].IsValueShownAsLabel = true;
                        chart1.Series[0].LabelFormat = "#,##0.00";
                        chart1.Series[0]["PieLabelStyle"] = "Outside";
                        chart1.Series[0].BorderColor = System.Drawing.Color.White;
                        chart1.Series[0].BorderWidth = 1;

                        chart1.DataBind();
                    }
                    else
                    {
                        chart1.Titles.Clear();
                        chart1.Titles.Add("No Data Available for Selected Range");
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Chart Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                if (cn.State == ConnectionState.Open) cn.Close();
            }
        }

        private void chart1_Click(object sender, EventArgs e)
        {
        }
    }
}