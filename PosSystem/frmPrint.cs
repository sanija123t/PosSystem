using Microsoft.Reporting.WinForms;
using System;
using System.Data;
using System.IO; // Added for Path.Combine
using System.Windows.Forms;

namespace PosSystem
{
    public partial class frmPrint : Form
    {
        // Use the specific DataTable type from your DataSet
        DataSet1.dtBarcodeDataTable _barcode;

        public frmPrint(DataSet1.dtBarcodeDataTable barcode)
        {
            InitializeComponent();
            this._barcode = barcode;
        }

        private void frmPrint_Load(object sender, EventArgs e)
        {
            try
            {
                // 1. Build the path safely
                string reportPath = Path.Combine(Application.StartupPath, "Bill", "ReportBarcode.rdlc");

                // 2. Check if the file actually exists to prevent a crash
                if (!File.Exists(reportPath))
                {
                    MessageBox.Show("Report file not found at: " + reportPath, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                // 3. Set report path and allow external images
                this.reportViewer1.LocalReport.ReportPath = reportPath;
                this.reportViewer1.LocalReport.EnableExternalImages = true;

                // 4. Clear and add the data source
                ReportDataSource rds = new ReportDataSource("DataSet1", (DataTable)_barcode);
                this.reportViewer1.LocalReport.DataSources.Clear();
                this.reportViewer1.LocalReport.DataSources.Add(rds);

                // 5. Configure display and toolbar
                this.reportViewer1.SetDisplayMode(DisplayMode.PrintLayout); // Optional: page layout
                this.reportViewer1.ZoomMode = ZoomMode.Percent;
                this.reportViewer1.ZoomPercent = 100;
                this.reportViewer1.ShowToolBar = true; // <-- enable toolbar for print/export

                // 6. Refresh report
                this.reportViewer1.RefreshReport();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Printing Error: " + ex.Message, "POS System", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
        private void reportViewer1_Load(object sender, EventArgs e)
        {
            // You can leave this empty if not needed
        }

        private void pictureBox1_Click(object sender, EventArgs e)
        {
            this.Dispose();
        }
    }
}