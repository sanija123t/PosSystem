using System;
using System.Data;
using System.Data.SQLite;
using System.Windows.Forms;
using Microsoft.Reporting.WinForms;
using System.IO;

namespace PosSystem
{
    public partial class frmResipt : Form
    {
        SQLiteConnection cn;
        Form1 f1;
        frmPOS fPOS;

        // Updated Constructor to handle generic Form and cast to specific types
        public frmResipt(Form frm)
        {
            InitializeComponent();
            cn = new SQLiteConnection(DBConnection.MyConnection());

            if (frm is Form1)
            {
                f1 = (Form1)frm;
            }
            else if (frm is frmPOS)
            {
                fPOS = (frmPOS)frm;
            }
        }

        private void frmResipt_Load(object sender, EventArgs e)
        {
            this.reportViewer1.RefreshReport();
        }

        public void LoadReport(string pcash, string pchange)
        {
            ReportDataSource rtpDataSource;
            try
            {
                string reportPath = Path.Combine(Application.StartupPath, "Bill", "Report1.rdlc");
                this.reportViewer1.LocalReport.ReportPath = reportPath;
                this.reportViewer1.LocalReport.DataSources.Clear();

                DataSet1 ds = new DataSet1();
                SQLiteDataAdapter da = new SQLiteDataAdapter();

                cn.Open();
                string sql = "SELECT c.id, c.transno, c.pcode, c.price, c.qty, c.disc, (c.price * c.qty) as total, c.sdate, c.status, p.pdesc " +
                             "FROM tblCart1 AS c INNER JOIN TblProduct1 AS p ON p.pcode = c.pcode " +
                             "WHERE transno LIKE @transno";

                da.SelectCommand = new SQLiteCommand(sql, cn);

                // Determine which form's labels to use
                string transno = (fPOS != null) ? fPOS.lblTransno.Text : f1.lblTransno.Text;
                da.SelectCommand.Parameters.AddWithValue("@transno", transno);

                da.Fill(ds.Tables["dtSold"]);
                cn.Close();

                // Parameters mapping from either frmPOS or Form1
                ReportParameter pPhone = new ReportParameter("pPhone", (fPOS != null) ? fPOS.lblPhone.Text : f1.lblPhone.Text);
                ReportParameter pTotal = new ReportParameter("pTotal", (fPOS != null) ? fPOS.lblTotal.Text : f1.lblTotal.Text);
                ReportParameter pCash = new ReportParameter("pCash", pcash);
                ReportParameter pDiscount = new ReportParameter("pDiscount", (fPOS != null) ? fPOS.lblDiscount.Text : f1.lblDiscount.Text);
                ReportParameter pChange = new ReportParameter("pChange", pchange);
                ReportParameter pStore = new ReportParameter("pStore", (fPOS != null) ? fPOS.lblSname.Text : f1.lblSname.Text);
                ReportParameter pAddress = new ReportParameter("pAddress", (fPOS != null) ? fPOS.lblAddress.Text : f1.lblAddress.Text);
                ReportParameter pTransaction = new ReportParameter("pTransaction", "Invoice #: " + transno);
                ReportParameter pCashier = new ReportParameter("pCashier", (fPOS != null) ? fPOS.LblUser.Text : f1.lblUser.Text);

                reportViewer1.LocalReport.SetParameters(new ReportParameter[] {
                    pPhone, pTotal, pCash, pDiscount, pChange, pStore, pAddress, pTransaction, pCashier
                });

                rtpDataSource = new ReportDataSource("DataSet1", ds.Tables["dtSold"]);
                reportViewer1.LocalReport.DataSources.Add(rtpDataSource);

                reportViewer1.SetDisplayMode(DisplayMode.PrintLayout);
                reportViewer1.ZoomMode = ZoomMode.Percent;
                reportViewer1.ZoomPercent = 100;
                reportViewer1.RefreshReport();
            }
            catch (Exception ex)
            {
                if (cn.State == ConnectionState.Open) cn.Close();
                MessageBox.Show(ex.Message, "Receipt Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void reportViewer1_Load(object sender, EventArgs e) { }
        private void reportViewer1_Load_1(object sender, EventArgs e) { }
    }
}