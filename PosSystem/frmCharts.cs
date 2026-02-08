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
        // REMOVED: DBConnection db = new DBConnection(); // You cannot instantiate a static class

        public frmCharts()
        {
            InitializeComponent();
            // FIXED: Access the static method directly using the Class Name
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
                SQLiteDataAdapter da;
                cn.Open();
                da = new SQLiteDataAdapter(sql, cn);
                DataSet ds = new DataSet();
                da.Fill(ds, "SOLD");

                chart1.DataSource = ds.Tables["SOLD"];
                Series series = chart1.Series[0];
                series.ChartType = SeriesChartType.Pie;

                series.Name = "Sold Items";
                chart1.Series[0].XValueMember = "pdesc";
                chart1.Series[0].YValueMembers = "total";
                chart1.Series[0].LabelFormat = "#,##0.00";
                chart1.Series[0].IsValueShownAsLabel = true;

                chart1.DataBind();
                cn.Close();
            }
            catch (Exception ex)
            {
                if (cn.State == ConnectionState.Open) cn.Close();
                MessageBox.Show(ex.Message, "Chart Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void chart1_Click(object sender, EventArgs e)
        {
        }
    }
}