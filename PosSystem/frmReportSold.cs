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
            // Optionally, load report on form load
            // LoadReportAsync(); // can uncomment if needed
        }

        private void pictureBox2_Click(object sender, EventArgs e)
        {
            Dispose();
        }

        private void panel1_Paint(object sender, PaintEventArgs e)
        {
            // required by Designer
        }

        // Fix for CS1061: Added missing event handler referenced in Designer
        private void reportViewer1_Load_1(object sender, EventArgs e)
        {
        }

        #endregion

        #region Report Generation

        // Fix for CS1061: Added LoadReport wrapper to satisfy external calls from frmSoldItems.cs
        public void LoadReport()
        {
            _ = LoadReportAsync();
        }

        public async Task LoadReportAsync()
        {
            try
            {
                DataSet1 ds = new DataSet1();
                ReportDataSource rptDS;

                string reportPath = Path.Combine(Application.StartupPath, "Bill", "Report2.rdlc");
                if (!File.Exists(reportPath))
                {
                    MessageBox.Show($"Report file not found:\n{reportPath}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                reportViewer1.LocalReport.ReportPath = reportPath;
                reportViewer1.LocalReport.DataSources.Clear();

                // ✅ Using statement ensures connection closes automatically
                using (var cn = new SQLiteConnection(DBConnection.MyConnection()))
                {
                    await cn.OpenAsync();

                    string sql =
                        @"SELECT c.id, c.transno, c.pcode, p.pdesc, c.price, c.qty, c.disc AS discount,
                                  (c.price * c.qty) - c.disc AS total
                           FROM tblCart1 AS c
                           INNER JOIN TblProduct1 AS p ON c.pcode = p.pcode
                           WHERE c.status = @status
                             AND c.sdate BETWEEN @dateFrom AND @dateTo
                             AND (@cashier IS NULL OR c.cashier = @cashier)"; // ✅ SQL simplification

                    using (var cmd = new SQLiteCommand(sql, cn))
                    {
                        cmd.Parameters.AddWithValue("@status", STATUS_SOLD);
                        cmd.Parameters.AddWithValue("@dateFrom", f.dateTimePicker1.Value.Date);
                        cmd.Parameters.AddWithValue("@dateTo", f.dateTimePicker2.Value.Date.AddDays(1).AddSeconds(-1));

                        // Pass null if "All Cashier" is selected
                        cmd.Parameters.AddWithValue("@cashier", f.cbCashier.Text == "All Cashier" ? DBNull.Value : f.cbCashier.Text);

                        using (var da = new SQLiteDataAdapter(cmd))
                        {
                            // ✅ Async fill using Task.Run to prevent UI freezing
                            await Task.Run(() => da.Fill(ds.Tables["dtSoldReport"]));
                        }
                    }
                }

                // Report parameters
                var pDate = new ReportParameter("pDate", $"Date From: {f.dateTimePicker1.Value:dd/MM/yyyy} To {f.dateTimePicker2.Value:dd/MM/yyyy}");
                var pCashier = new ReportParameter("pCashier", $"Cashier: {f.cbCashier.Text}");
                var pHeader = new ReportParameter("pHeader", "SALES REPORT");

                reportViewer1.LocalReport.SetParameters(new ReportParameter[] { pDate, pCashier, pHeader });

                rptDS = new ReportDataSource("DataSet1", ds.Tables["dtSoldReport"]);
                reportViewer1.LocalReport.DataSources.Add(rptDS);

                reportViewer1.SetDisplayMode(DisplayMode.PrintLayout);
                reportViewer1.ZoomMode = ZoomMode.Percent;
                reportViewer1.ZoomPercent = 100;
                reportViewer1.RefreshReport();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Report Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        #endregion
    }
}