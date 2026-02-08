using System;
using System.Data;
using System.Data.SQLite;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;

namespace PosSystem
{
    public partial class frmDashbord : Form
    {
        private SQLiteConnection cn;
        // REMOVED: DBConnection db = new DBConnection(); // Error: static class

        public frmDashbord()
        {
            InitializeComponent();
            // FIXED: Use the static class name directly
            cn = new SQLiteConnection(DBConnection.MyConnection());
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
                cn.Open();

                // SQLite query grouping by month from the sdate string
                string query = @"SELECT strftime('%m', sdate) AS month, 
                                 IFNULL(SUM(price * qty), 0.0) AS total 
                                 FROM tblCart1 
                                 WHERE status LIKE 'Sold' 
                                 GROUP BY month";

                SQLiteDataAdapter da = new SQLiteDataAdapter(query, cn);
                DataSet ds = new DataSet();
                da.Fill(ds, "Sales");

                chart1.DataSource = ds.Tables["Sales"];

                Series series1;
                // Check if the series already exists to avoid duplicates
                if (chart1.Series.IndexOf("SALES") >= 0)
                {
                    series1 = chart1.Series["SALES"];
                    series1.Points.Clear();
                }
                else
                {
                    series1 = chart1.Series.Add("SALES");
                }

                series1.ChartType = SeriesChartType.Doughnut;
                series1.XValueMember = "month";
                series1.YValueMembers = "total";
                series1.IsValueShownAsLabel = true;

                // Formats the labels as currency/number
                series1.LabelFormat = "{#,##0.00}";

                chart1.DataBind();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error Loading Chart", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            finally
            {
                if (cn.State == ConnectionState.Open)
                    cn.Close();
            }
        }

        // Add this to refresh stats when the dashboard loads
        private void frmDashbord_Load(object sender, EventArgs e)
        {
            // If you have labels for Daily Sales, etc., call them here:
            // lblDailySales.Text = DBConnection.DailySales().ToString("C");
        }
    }
}