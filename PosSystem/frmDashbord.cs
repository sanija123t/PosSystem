using System;
using System.Data;
using System.Data.SQLite;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;

namespace PosSystem
{
    public partial class frmDashbord : Form
    {
        public frmDashbord()
        {
            InitializeComponent();
            LoadChart();
        }

        private void frmDashbord_Resize(object sender, EventArgs e)
        {
            panel1.Left = (this.ClientSize.Width - panel1.Width) / 2;
        }

        private void panel1_Paint(object sender, PaintEventArgs e) { }

        private void panel2_Paint(object sender, PaintEventArgs e) { }

        private void label3_Click(object sender, EventArgs e) { }

        private void label11_Click(object sender, EventArgs e) { }

        private void chart1_Click(object sender, EventArgs e) { }

        public void LoadChart()
        {
            try
            {
                using (SQLiteConnection cn = new SQLiteConnection(DBConnection.MyConnection()))
                {
                    cn.Open();

                    // Get current year dynamically
                    string currentYear = DateTime.Now.Year.ToString();

                    // SQLite: Grouping by month and filtering by current year
                    string query = $@"SELECT 
                             CASE strftime('%m', sdate) 
                                WHEN '01' THEN 'Jan' WHEN '02' THEN 'Feb' WHEN '03' THEN 'Mar' 
                                WHEN '04' THEN 'Apr' WHEN '05' THEN 'May' WHEN '06' THEN 'Jun'
                                WHEN '07' THEN 'Jul' WHEN '08' THEN 'Aug' WHEN '09' THEN 'Sep'
                                WHEN '10' THEN 'Oct' WHEN '11' THEN 'Nov' WHEN '12' THEN 'Dec' 
                             END AS MonthName,
                             SUM(total) AS total 
                             FROM tblCart1 
                             WHERE status LIKE 'Sold' AND strftime('%Y', sdate) = @year
                             GROUP BY MonthName 
                             ORDER BY strftime('%m', sdate) ASC";

                    using (SQLiteCommand cmd = new SQLiteCommand(query, cn))
                    {
                        cmd.Parameters.AddWithValue("@year", currentYear);
                        SQLiteDataAdapter da = new SQLiteDataAdapter(cmd);
                        DataTable dt = new DataTable();
                        da.Fill(dt);

                        if (dt.Rows.Count == 0)
                        {
                            // Optional: Hide chart or show a message if there's no data
                            // chart1.Visible = false; 
                            return;
                        }
                        chart1.Visible = true;

                        chart1.DataSource = dt;
                        chart1.Series.Clear(); // Clear old data

                        Series series1 = chart1.Series.Add("SALES");
                        series1.ChartType = SeriesChartType.Doughnut;

                        series1.XValueMember = "MonthName";
                        series1.YValueMembers = "total";

                        series1.IsValueShownAsLabel = true;
                        series1.LabelFormat = "#,##0.00";

                        // Doughnut styling - safer access
                        series1["PieLabelStyle"] = "Outside";
                        chart1.ChartAreas[0].Area3DStyle.Enable3D = true;

                        chart1.DataBind();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Chart Error: " + ex.Message);
            }
        }

        // Refresh dashboard stats on load
        private void frmDashbord_Load(object sender, EventArgs e)
        {
            // Example: lblDailySales.Text = DBConnection.DailySales().ToString("C");
        }
    }
}