using Microsoft.Reporting.WinForms;
using System;
using System.Data;
using System.IO;
using System.Windows.Forms;

namespace PosSystem
{
    public partial class frmPrint : Form
    {
        private DataSet1.dtBarcodeDataTable _barcode;

        public frmPrint(DataSet1.dtBarcodeDataTable barcode)
        {
            InitializeComponent();
            _barcode = barcode;
        }

        private void frmPrint_Load(object sender, EventArgs e)
        {
            try
            {
                // Build report path safely
                string reportPath = Path.Combine(Application.StartupPath, "Bill", "ReportBarcode.rdlc");

                // Check if the RDLC file exists
                if (!File.Exists(reportPath))
                {
                    MessageBox.Show("Report file not found at: " + reportPath, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                // Set report path
                reportViewer1.LocalReport.ReportPath = reportPath;

                // Clear existing data sources
                reportViewer1.LocalReport.DataSources.Clear();

                // Add the barcode data table as the report data source
                ReportDataSource rds = new ReportDataSource("DataSet1", (DataTable)_barcode);
                reportViewer1.LocalReport.DataSources.Add(rds);

                // Configure viewer display
                reportViewer1.SetDisplayMode(DisplayMode.PrintLayout);
                reportViewer1.ZoomMode = ZoomMode.Percent;
                reportViewer1.ZoomPercent = 100;
                reportViewer1.ShowToolBar = true;

                // Refresh to render the report
                reportViewer1.RefreshReport();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Printing Error: " + ex.Message, "POS System", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void reportViewer1_Load(object sender, EventArgs e)
        {
            // Not required for V1, safe to leave empty
        }

        private void pictureBox1_Click(object sender, EventArgs e)
        {
            this.Dispose();
        }
    }
}