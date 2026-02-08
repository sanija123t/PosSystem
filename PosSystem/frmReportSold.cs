using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Data.SqlClient;
using Microsoft.Reporting.WinForms;
using System.IO;

namespace PosSystem
{
    public partial class frmReportSold : Form
    {
        SqlConnection cn = new SqlConnection();
        SqlCommand cm = new SqlCommand();

        // Error Fixed: Removed "DBConnection dbcon = new DBConnection();"
        frmSoldItems f;

        public frmReportSold(frmSoldItems frm)
        {
            InitializeComponent();
            // Error Fixed: Call the static method directly using the Class Name
            cn = new SqlConnection(DBConnection.MyConnection());
            f = frm;
        }

        private void pictureBox2_Click(object sender, EventArgs e)
        {
            this.Dispose();
        }

        public void LoadReport()
        {
            try
            {
                ReportDataSource rptDS;

                // Robust path handling for Installer
                string reportPath = Path.Combine(Application.StartupPath, "Bill", "Report2.rdlc");
                this.reportViewer1.LocalReport.ReportPath = reportPath;
                this.reportViewer1.LocalReport.DataSources.Clear();

                DataSet1 ds = new DataSet1();
                SqlDataAdapter da = new SqlDataAdapter();

                cn.Open();

                string sql;
                if (f.cbCashier.Text == "All Cashier")
                {
                    sql = "SELECT c.id, c.transno, c.pcode, p.pdesc, c.price, c.qty, c.disc as discount, total FROM tblCart1 as c INNER JOIN TblProduct1 as p ON c.pcode = p.pcode WHERE status LIKE 'sold' AND sdate BETWEEN @dateFrom AND @dateTo";
                    da.SelectCommand = new SqlCommand(sql, cn);
                }
                else
                {
                    sql = "SELECT c.id, c.transno, c.pcode, p.pdesc, c.price, c.qty, c.disc as discount, total FROM tblCart1 as c INNER JOIN TblProduct1 as p ON c.pcode = p.pcode WHERE status LIKE 'sold' AND sdate BETWEEN @dateFrom AND @dateTo AND cashier LIKE @cashier";
                    da.SelectCommand = new SqlCommand(sql, cn);
                    da.SelectCommand.Parameters.AddWithValue("@cashier", f.cbCashier.Text);
                }

                // Use Parameters for dates to avoid regional format errors
                da.SelectCommand.Parameters.AddWithValue("@dateFrom", f.dateTimePicker1.Value);
                da.SelectCommand.Parameters.AddWithValue("@dateTo", f.dateTimePicker2.Value);

                da.Fill(ds.Tables["dtSoldReport"]);
                cn.Close();

                // Parameters for the Report Designer
                ReportParameter pDate = new ReportParameter("pDate", "Date From: " + f.dateTimePicker1.Value.ToShortDateString() + " To " + f.dateTimePicker2.Value.ToShortDateString());
                ReportParameter pCashier = new ReportParameter("pCashier", "Cashier: " + f.cbCashier.Text);
                ReportParameter pHeader = new ReportParameter("pHeader", "SALES REPORT");

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
                if (cn.State == ConnectionState.Open) cn.Close();
                MessageBox.Show(ex.Message, "Report Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void frmReportSold_Load(object sender, EventArgs e)
        {
        }

        private void reportViewer1_Load(object sender, EventArgs e) { }
        private void panel1_Paint(object sender, PaintEventArgs e) { }
        private void reportViewer1_Load_1(object sender, EventArgs e) { }
    }
}