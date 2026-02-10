using System;
using System.Data;
using System.Data.SQLite;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Reporting.WinForms;

namespace PosSystem
{
    public partial class frmResipt : Form
    {
        private readonly Form1 f1;
        private readonly frmPOS fPOS;

        // Constructor
        public frmResipt(Form frm)
        {
            InitializeComponent();

            if (frm is Form1 form1)
                f1 = form1;
            else if (frm is frmPOS formPOS)
                fPOS = formPOS;
            else
                throw new ArgumentException("Unsupported form type passed to frmResipt.");
        }

        private void frmResipt_Load(object sender, EventArgs e)
        {
            reportViewer1.RefreshReport();
        }

        /// <summary>
        /// Load report (async-safe for WinForms)
        /// </summary>
        /// <param name="pcash">Cash amount paid</param>
        /// <param name="pchange">Change returned</param>
        public async void LoadReport(string pcash = "", string pchange = "")
        {
            await LoadReportAsync(pcash, pchange);
        }

        /// <summary>
        /// Async method to load receipt data and report
        /// </summary>
        public async Task LoadReportAsync(string pcash, string pchange)
        {
            try
            {
                // Ensure the report folder exists in project -> set "Copy to Output Directory" = Copy Always
                string reportFolder = Path.Combine(Application.StartupPath, "Bill");
                string reportFile = Path.Combine(reportFolder, "Report1.rdlc");

                if (!File.Exists(reportFile))
                    throw new FileNotFoundException("Report file not found: " + reportFile);

                reportViewer1.LocalReport.ReportPath = reportFile;
                reportViewer1.LocalReport.DataSources.Clear();

                DataSet1 ds = new DataSet1();
                string transno = fPOS != null ? fPOS.lblTransno.Text : f1.lblTransno.Text;

                // Fetch data from SQLite off the UI thread
                await Task.Run(() =>
                {
                    using (var cn = new SQLiteConnection(DBConnection.MyConnection()))
                    {
                        cn.Open();

                        // ⚡ Optimization: create index in SQLite for faster queries:
                        // CREATE INDEX idx_transno ON tblCart1(transno);

                        string sql = @"SELECT c.id, c.transno, c.pcode, c.price, c.qty, c.disc, 
                                              (c.price * c.qty) as total, c.sdate, c.status, p.pdesc
                                       FROM tblCart1 AS c
                                       INNER JOIN TblProduct1 AS p ON p.pcode = c.pcode
                                       WHERE c.transno = @transno";

                        using (var cmd = new SQLiteCommand(sql, cn))
                        {
                            cmd.Parameters.AddWithValue("@transno", transno);

                            using (var da = new SQLiteDataAdapter(cmd))
                            {
                                da.Fill(ds.Tables["dtSold"]);
                            }
                        }
                    }
                });

                // Set report parameters safely on UI thread
                var parameters = new[]
                {
                    new ReportParameter("pPhone", fPOS != null ? fPOS.lblPhone.Text : f1.lblPhone.Text),
                    new ReportParameter("pTotal", fPOS != null ? fPOS.lblTotal.Text : f1.lblTotal.Text),
                    new ReportParameter("pCash", pcash),
                    new ReportParameter("pDiscount", fPOS != null ? fPOS.lblDiscount.Text : f1.lblDiscount.Text),
                    new ReportParameter("pChange", pchange),
                    new ReportParameter("pStore", fPOS != null ? fPOS.lblSname.Text : f1.lblSname.Text),
                    new ReportParameter("pAddress", fPOS != null ? fPOS.lblAddress.Text : f1.lblAddress.Text),
                    new ReportParameter("pTransaction", "Invoice #: " + transno),
                    new ReportParameter("pCashier", fPOS != null ? fPOS.LblUser.Text : f1.lblUserName.Text)
                };

                reportViewer1.LocalReport.SetParameters(parameters);

                var rds = new ReportDataSource("DataSet1", ds.Tables["dtSold"]);
                reportViewer1.LocalReport.DataSources.Add(rds);

                reportViewer1.SetDisplayMode(DisplayMode.PrintLayout);
                reportViewer1.ZoomMode = ZoomMode.Percent;
                reportViewer1.ZoomPercent = 100;
                reportViewer1.RefreshReport();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to load receipt: " + ex.Message,
                                "Receipt Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void reportViewer1_Load(object sender, EventArgs e)
        {
            // Designer handler (empty)
        }
    }
}