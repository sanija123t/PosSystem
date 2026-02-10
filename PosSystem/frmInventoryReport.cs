using System;
using System.Data.SQLite;
using System.Windows.Forms;
using Microsoft.Reporting.WinForms;

namespace PosSystem
{
    public partial class frmInventoryReport : Form
    {
        public frmInventoryReport()
        {
            InitializeComponent();
        }

        private void pictureBox2_Click(object sender, EventArgs e)
        {
            Dispose();
        }

        private void frmInventoryReport_Load(object sender, EventArgs e) { }
        private void reportViewer1_Load(object sender, EventArgs e) { }

        // ================================
        // PUBLIC REPORT METHODS
        // ================================

        public void LoadSoldItems(string sql, string param)
        {
            var ds = new DataSet1();
            FillDataSet(ds.Tables["dtSoldItem"], sql);

            SetReport("Bill\\ReportSold.rdlc", ds.Tables["dtSoldItem"],
                      new ReportParameter("pDate", param));
        }

        public void LoadTopSelling(string sql, string param, string header)
        {
            var ds = new DataSet1();
            FillDataSet(ds.Tables["dtTopSelling"], sql);

            SetReport("Bill\\ReportTop.rdlc", ds.Tables["dtTopSelling"],
                      new ReportParameter("pDate", param),
                      new ReportParameter("pHeader", header));
        }

        public void LoadReport()
        {
            var ds = new DataSet1();
            string query = @"
                SELECT p.pcode, p.barcode, p.pdesc, b.brand, c.category, p.price, p.qty, p.reorder
                FROM TblProduct1 AS p
                INNER JOIN BrandTbl AS b ON p.bid = b.id
                INNER JOIN TblCatecory AS c ON p.cid = c.id";

            FillDataSet(ds.Tables["dtInventory"], query);
            SetReport("Bill\\Report3.rdlc", ds.Tables["dtInventory"]);
        }

        public void LoadStockReport(string sql, string param)
        {
            var ds = new DataSet1();
            FillDataSet(ds.Tables["dtStockin"], sql);

            SetReport("Bill\\ReportStock.rdlc", ds.Tables["dtStockin"],
                      new ReportParameter("pDate", param));
        }

        public void LoadCancelReport(string sql, string param)
        {
            var ds = new DataSet1();
            FillDataSet(ds.Tables["dtCancel"], sql);

            SetReport("Bill\\ReportCancel.rdlc", ds.Tables["dtCancel"],
                      new ReportParameter("pDate", param));
        }

        // ================================
        // PRIVATE HELPERS
        // ================================

        private void FillDataSet(System.Data.DataTable table, string sql)
        {
            try
            {
                using (var cn = new SQLiteConnection(DBConnection.MyConnection()))
                using (var da = new SQLiteDataAdapter(sql, cn))
                {
                    cn.Open();
                    table.Clear();
                    da.Fill(table);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void SetReport(string rdlcPath, System.Data.DataTable table, params ReportParameter[] parameters)
        {
            try
            {
                reportViewer1.LocalReport.ReportPath = Application.StartupPath + "\\" + rdlcPath;
                reportViewer1.LocalReport.DataSources.Clear();
                reportViewer1.LocalReport.DataSources.Add(new ReportDataSource("DataSet1", table));

                if (parameters != null)
                    reportViewer1.LocalReport.SetParameters(parameters);

                reportViewer1.SetDisplayMode(DisplayMode.PrintLayout);
                reportViewer1.ZoomMode = ZoomMode.Percent;
                reportViewer1.ZoomPercent = 100;
                reportViewer1.RefreshReport();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
    }
}