using System;
using System.Data;
using System.Data.SQLite; // Switched to SQLite
using System.Windows.Forms;
using Microsoft.Reporting.WinForms;

namespace PosSystem
{
    public partial class frmInventoryReport : Form
    {
        private SQLiteConnection cn;

        public frmInventoryReport()
        {
            InitializeComponent();
            // Access static method directly from DBConnection class
            cn = new SQLiteConnection(DBConnection.MyConnection());
        }

        private void pictureBox2_Click(object sender, EventArgs e)
        {
            this.Dispose();
        }

        private void frmInventoryReport_Load(object sender, EventArgs e) { }

        private void reportViewer1_Load(object sender, EventArgs e) { }

        // Load sold items report
        public void LoadSoldItems(string sql, string param)
        {
            try
            {
                this.reportViewer1.LocalReport.ReportPath = Application.StartupPath + @"\Bill\ReportSold.rdlc";
                this.reportViewer1.LocalReport.DataSources.Clear();

                DataSet1 ds = new DataSet1();
                using (SQLiteDataAdapter da = new SQLiteDataAdapter(sql, cn))
                {
                    cn.Open();
                    da.Fill(ds.Tables["dtSoldItem"]);
                    cn.Close();
                }

                ReportParameter pDate = new ReportParameter("pDate", param);
                reportViewer1.LocalReport.SetParameters(pDate);

                ReportDataSource rptDS = new ReportDataSource("DataSet1", ds.Tables["dtSoldItem"]);
                reportViewer1.LocalReport.DataSources.Add(rptDS);
                reportViewer1.SetDisplayMode(DisplayMode.PrintLayout);
                reportViewer1.ZoomMode = ZoomMode.Percent;
                reportViewer1.ZoomPercent = 100;
                reportViewer1.RefreshReport();
            }
            catch (Exception ex)
            {
                if (cn.State == ConnectionState.Open) cn.Close();
                MessageBox.Show(ex.Message, "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        // Load top-selling report
        public void LoadTopSelling(string sql, string param, string header)
        {
            try
            {
                this.reportViewer1.LocalReport.ReportPath = Application.StartupPath + @"\Bill\ReportTop.rdlc";
                this.reportViewer1.LocalReport.DataSources.Clear();

                DataSet1 ds = new DataSet1();
                using (SQLiteDataAdapter da = new SQLiteDataAdapter(sql, cn))
                {
                    cn.Open();
                    da.Fill(ds.Tables["dtTopSelling"]);
                    cn.Close();
                }

                reportViewer1.LocalReport.SetParameters(new ReportParameter("pDate", param));
                reportViewer1.LocalReport.SetParameters(new ReportParameter("pHeader", header));

                ReportDataSource rptDS = new ReportDataSource("DataSet1", ds.Tables["dtTopSelling"]);
                reportViewer1.LocalReport.DataSources.Add(rptDS);
                reportViewer1.SetDisplayMode(DisplayMode.PrintLayout);
                reportViewer1.ZoomMode = ZoomMode.Percent;
                reportViewer1.ZoomPercent = 100;
                reportViewer1.RefreshReport();
            }
            catch (Exception ex)
            {
                if (cn.State == ConnectionState.Open) cn.Close();
                MessageBox.Show(ex.Message, "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        // Load full inventory report
        public void LoadReport()
        {
            try
            {
                this.reportViewer1.LocalReport.ReportPath = Application.StartupPath + @"\Bill\Report3.rdlc";
                this.reportViewer1.LocalReport.DataSources.Clear();

                DataSet1 ds = new DataSet1();
                string query = @"SELECT p.pcode, p.barcode, p.pdesc, b.brand, c.category, p.price, p.qty, p.reorder
                                 FROM TblProduct1 AS p
                                 INNER JOIN BrandTbl AS b ON p.bid = b.id
                                 INNER JOIN TblCatecory AS c ON p.cid = c.id";

                using (SQLiteDataAdapter da = new SQLiteDataAdapter(query, cn))
                {
                    cn.Open();
                    da.Fill(ds.Tables["dtInventory"]);
                    cn.Close();
                }

                ReportDataSource rptDS = new ReportDataSource("DataSet1", ds.Tables["dtInventory"]);
                reportViewer1.LocalReport.DataSources.Add(rptDS);
                reportViewer1.SetDisplayMode(DisplayMode.PrintLayout);
                reportViewer1.ZoomMode = ZoomMode.Percent;
                reportViewer1.ZoomPercent = 100;
                reportViewer1.RefreshReport();
            }
            catch (Exception ex)
            {
                if (cn.State == ConnectionState.Open) cn.Close();
                MessageBox.Show(ex.Message, "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        public void LoadStockReport(string psql, string param)
        {
            try
            {
                this.reportViewer1.LocalReport.ReportPath = Application.StartupPath + @"\Bill\ReportStock.rdlc";
                this.reportViewer1.LocalReport.DataSources.Clear();

                DataSet1 ds = new DataSet1();
                using (SQLiteDataAdapter da = new SQLiteDataAdapter(psql, cn))
                {
                    cn.Open();
                    da.Fill(ds.Tables["dtStockin"]);
                    cn.Close();
                }

                ReportParameter pDate = new ReportParameter("pDate", param);
                reportViewer1.LocalReport.SetParameters(pDate);

                ReportDataSource rptDS = new ReportDataSource("DataSet1", ds.Tables["dtStockin"]);
                reportViewer1.LocalReport.DataSources.Add(rptDS);
                reportViewer1.SetDisplayMode(DisplayMode.PrintLayout);
                reportViewer1.ZoomMode = ZoomMode.Percent;
                reportViewer1.ZoomPercent = 100;
                reportViewer1.RefreshReport();
            }
            catch (Exception ex)
            {
                if (cn.State == ConnectionState.Open) cn.Close();
                MessageBox.Show(ex.Message, "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        public void LoadCancelReport(string psql, string param)
        {
            try
            {
                this.reportViewer1.LocalReport.ReportPath = Application.StartupPath + @"\Bill\ReportCancel.rdlc";
                this.reportViewer1.LocalReport.DataSources.Clear();

                DataSet1 ds = new DataSet1();
                using (SQLiteDataAdapter da = new SQLiteDataAdapter(psql, cn))
                {
                    cn.Open();
                    da.Fill(ds.Tables["dtCancel"]);
                    cn.Close();
                }

                ReportParameter pDate = new ReportParameter("pDate", param);
                reportViewer1.LocalReport.SetParameters(pDate);

                ReportDataSource rptDS = new ReportDataSource("DataSet1", ds.Tables["dtCancel"]);
                reportViewer1.LocalReport.DataSources.Add(rptDS);
                reportViewer1.SetDisplayMode(DisplayMode.PrintLayout);
                reportViewer1.ZoomMode = ZoomMode.Percent;
                reportViewer1.ZoomPercent = 100;
                reportViewer1.RefreshReport();
            }
            catch (Exception ex)
            {
                if (cn.State == ConnectionState.Open) cn.Close();
                MessageBox.Show(ex.Message, "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
    }
}