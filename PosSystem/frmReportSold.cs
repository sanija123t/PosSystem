using System;
using System.Data;
using System.Data.SQLite;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Reporting.WinForms;

namespace PosSystem
{
    public partial class frmReportSold : Form
    {
        private readonly frmSoldItems f;
        private const string STATUS_SOLD = "sold";

        public frmReportSold(frmSoldItems frm)
        {
            InitializeComponent();
            f = frm;
        }

        #region Form Events
        private void frmReportSold_Load(object sender, EventArgs e)
        {
            // Report is triggered manually by f.LoadReport() from the parent form
        }

        private void pictureBox2_Click(object sender, EventArgs e)
        {
            this.Dispose();
        }

        private void reportViewer1_Load_1(object sender, EventArgs e) { }
        #endregion

        #region Report Generation
        public void LoadReport()
        {
            // Fire and forget the async task
            _ = LoadReportAsync();
        }
        private void panel1_Paint(object sender, PaintEventArgs e)
        {
            // This can stay empty. It just stops the CS1061 error.
        }
        public async Task LoadReportAsync()
        {
            try
            {
                DataSet1 ds = new DataSet1();
                ReportDataSource rptDS;

                // Path to the RDLC file in the Debug/Release folder
                string reportPath = Path.Combine(Application.StartupPath, "Bill", "Report2.rdlc");

                if (!File.Exists(reportPath))
                {
                    MessageBox.Show($"Report file not found at:\n{reportPath}\n\nPlease ensure the 'Bill' folder exists in your project output.", "File Missing", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                reportViewer1.LocalReport.ReportPath = reportPath;
                reportViewer1.LocalReport.DataSources.Clear();

                using (var cn = new SQLiteConnection(DBConnection.MyConnection()))
                {
                    await cn.OpenAsync();

                    // IMPROVED SQL: Explicitly casting numbers to ensure the RDLC can calculate 'total'
                    string sql = @"
                        SELECT 
                            c.id, 
                            c.transno, 
                            c.pcode, 
                            p.pdesc, 
                            CAST(c.price AS DOUBLE) as price, 
                            CAST(c.qty AS INTEGER) as qty, 
                            CAST(c.disc AS DOUBLE) AS discount,
                            (CAST(c.price AS DOUBLE) * CAST(c.qty AS INTEGER)) - CAST(c.disc AS DOUBLE) AS total
                        FROM tblCart1 AS c
                        INNER JOIN TblProduct1 AS p ON c.pcode = p.pcode
                        INNER JOIN tblTransaction AS t ON c.transno = t.transno
                        WHERE c.status = @status
                          AND t.sdate BETWEEN @dateFrom AND @dateTo
                          AND (@cashier IS NULL OR c.cashier = @cashier)";

                    using (var cmd = new SQLiteCommand(sql, cn))
                    {
                        cmd.Parameters.AddWithValue("@status", STATUS_SOLD);
                        cmd.Parameters.AddWithValue("@dateFrom", f.dateTimePicker1.Value.ToString("yyyy-MM-dd"));
                        cmd.Parameters.AddWithValue("@dateTo", f.dateTimePicker2.Value.ToString("yyyy-MM-dd"));

                        // Handles the "All Cashier" filter logic
                        cmd.Parameters.AddWithValue("@cashier", (f.cbCashier.Text == "All Cashier" || string.IsNullOrEmpty(f.cbCashier.Text)) ? DBNull.Value : f.cbCashier.Text);

                        using (var da = new SQLiteDataAdapter(cmd))
                        {
                            // Runs the data fill on a background thread to keep UI responsive
                            await Task.Run(() => da.Fill(ds.Tables["dtSoldReport"]));
                        }
                    }
                }

                // Map the C# values to the RDLC Parameters
                ReportParameter pDate = new ReportParameter("pDate", $"Range: {f.dateTimePicker1.Value:dd/MM/yyyy} - {f.dateTimePicker2.Value:dd/MM/yyyy}");
                ReportParameter pCashier = new ReportParameter("pCashier", $"Cashier: {f.cbCashier.Text}");
                ReportParameter pHeader = new ReportParameter("pHeader", "DAILY SALES REPORT");

                reportViewer1.LocalReport.SetParameters(new ReportParameter[] { pDate, pCashier, pHeader });

                // Link the processed data table to the RDLC's "DataSet1"
                rptDS = new ReportDataSource("DataSet1", ds.Tables["dtSoldReport"]);
                reportViewer1.LocalReport.DataSources.Add(rptDS);

                // Set up the view mode
                reportViewer1.SetDisplayMode(DisplayMode.PrintLayout);
                reportViewer1.ZoomMode = ZoomMode.Percent;
                reportViewer1.ZoomPercent = 100;

                reportViewer1.RefreshReport();
            }
            catch (Exception ex)
            {
                MessageBox.Show("An error occurred while generating the report: " + ex.Message, "Report Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }


        }
        #endregion
    }
}