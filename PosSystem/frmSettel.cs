using System;
using System.Data;
using System.Data.SQLite;
using System.Windows.Forms;

namespace PosSystem
{
    public partial class frmSettel : Form
    {
        private string connectionString = DBConnection.MyConnection();
        private Form1 f;
        private frmPOS fPOS;

        private decimal tempValue = 0;           // For arithmetic operations
        private string currentOperator = "";     // +, -, *, /
        private bool operatorClicked = false;    // Track if operator was clicked

        public frmSettel(Form1 fp)
        {
            InitializeComponent();
            f = fp;
            KeyPreview = true;
        }

        public frmSettel(frmPOS fp)
        {
            InitializeComponent();
            fPOS = fp;
            KeyPreview = true;
        }

        private void frmSettel_Load(object sender, EventArgs e)
        {
            txtCash.Focus();
        }

        private void pictureBox2_Click(object sender, EventArgs e) => this.Dispose();

        #region Numeric & Math Button Handlers

        private void btnNumber_Click(object sender, EventArgs e)
        {
            Button btn = sender as Button;
            if (operatorClicked)
            {
                txtCash.Clear();
                operatorClicked = false;
            }
            txtCash.Text += btn.Text;
        }

        private void btnC_Click(object sender, EventArgs e)
        {
            txtCash.Clear();
            txtCash.Focus();
            tempValue = 0;
            currentOperator = "";
            operatorClicked = false;
        }

        private void btnDecimal_Click(object sender, EventArgs e)
        {
            if (!txtCash.Text.Contains("."))
                txtCash.Text += ".";
        }

        private void btnOperator_Click(object sender, EventArgs e)
        {
            Button btn = sender as Button;
            if (decimal.TryParse(txtCash.Text, out decimal val))
            {
                if (!string.IsNullOrEmpty(currentOperator) && !operatorClicked)
                {
                    val = Calculate(tempValue, val, currentOperator);
                    txtCash.Text = val.ToString("0.00");
                }
                tempValue = val;
                currentOperator = btn.Text;
                operatorClicked = true;
            }
            else
            {
                currentOperator = btn.Text;
                operatorClicked = true;
            }
        }

        private void btnEquals_Click(object sender, EventArgs e)
        {
            if (!decimal.TryParse(txtCash.Text, out decimal secondValue))
                secondValue = tempValue;

            decimal result = Calculate(tempValue, secondValue, currentOperator);
            txtCash.Text = result.ToString("0.00");
            tempValue = 0;
            currentOperator = "";
            operatorClicked = false;
        }

        private decimal Calculate(decimal first, decimal second, string op)
        {
            return op switch
            {
                "+" => first + second,
                "-" => first - second,
                "*" => first * second,
                "/" => second != 0 ? first / second : 0,
                _ => second
            };
        }

        #endregion

        private void txtCash_TextChanged(object sender, EventArgs e)
        {
            try
            {
                decimal sale = decimal.TryParse(txtSale.Text, out decimal s) ? s : 0;
                decimal cash = decimal.TryParse(txtCash.Text, out decimal c) ? c : 0;
                decimal change = cash - sale;
                txtChange.Text = (change < 0) ? "0.00" : change.ToString("0.00");
            }
            catch
            {
                txtChange.Text = "0.00";
            }
        }

        private void btnEnter_Click(object sender, EventArgs e)
        {
            string currentTransNo = fPOS != null ? fPOS.lblTransno.Text : f.lblTransno.Text;
            Form activeForm = fPOS != null ? (Form)fPOS : (Form)f;

            DataGridView dgv = fPOS != null ? fPOS.dataGridView1 : f.dataGridView1;
            if (dgv.Rows.Count == 0) return;

            decimal saleAmount = decimal.TryParse(txtSale.Text, out decimal s) ? s : 0;
            decimal cashAmount = decimal.TryParse(txtCash.Text, out decimal c) ? c : 0;

            if (cashAmount < saleAmount)
            {
                MessageBox.Show("Insufficient amount. Please enter the correct amount!", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                using (SQLiteConnection cn = new SQLiteConnection(connectionString))
                {
                    cn.Open();
                    using (var transaction = cn.BeginTransaction())
                    {
                        foreach (DataGridViewRow row in dgv.Rows)
                        {
                            if (row.IsNewRow) continue;

                            string pcode = row.Cells[2].Value?.ToString();
                            int soldQty = int.TryParse(row.Cells[5].Value?.ToString(), out int q) ? q : 0;
                            int cartId = int.TryParse(row.Cells[1].Value?.ToString(), out int id) ? id : 0;

                            using (var cmdCheck = new SQLiteCommand("SELECT qty FROM TblProduct1 WHERE pcode=@pcode", cn))
                            {
                                cmdCheck.Parameters.AddWithValue("@pcode", pcode);
                                int stockQty = Convert.ToInt32(cmdCheck.ExecuteScalar() ?? 0);
                                if (soldQty > stockQty)
                                {
                                    MessageBox.Show($"Not enough stock for product {pcode}. Remaining: {stockQty}", "Stock Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                                    transaction.Rollback();
                                    return;
                                }
                            }

                            using (var cmdUpdateStock = new SQLiteCommand("UPDATE TblProduct1 SET qty = qty - @qty WHERE pcode=@pcode", cn))
                            {
                                cmdUpdateStock.Parameters.AddWithValue("@qty", soldQty);
                                cmdUpdateStock.Parameters.AddWithValue("@pcode", pcode);
                                cmdUpdateStock.ExecuteNonQuery();
                            }

                            using (var cmdUpdateCart = new SQLiteCommand("UPDATE tblCart1 SET status='Sold' WHERE id=@id", cn))
                            {
                                cmdUpdateCart.Parameters.AddWithValue("@id", cartId);
                                cmdUpdateCart.ExecuteNonQuery();
                            }
                        }
                        transaction.Commit();
                    }
                }

                // Show receipt
                frmResipt frm = new frmResipt(activeForm);
                frm.LoadReport(txtCash.Text, txtChange.Text);
                frm.ShowDialog();

                MessageBox.Show("Payment successfully saved!", "Payment", MessageBoxButtons.OK, MessageBoxIcon.Information);

                // Refresh Parent
                if (fPOS != null) { fPOS.GetTransNo(); fPOS.LoadCart(); }
                else { f.GetTransNo(); f.LoadCart(); }

                Dispose();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        #region Keyboard Support
        private void frmSettel_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape) this.Dispose();
            else if (e.KeyCode == Keys.Enter) { e.SuppressKeyPress = true; btnEnter_Click(sender, e); }
            else if ((e.KeyCode >= Keys.D0 && e.KeyCode <= Keys.D9) || (e.KeyCode >= Keys.NumPad0 && e.KeyCode <= Keys.NumPad9))
            {
                if (operatorClicked) { txtCash.Clear(); operatorClicked = false; }
                string number = e.KeyCode.ToString().Replace("D", "").Replace("NumPad", "");
                txtCash.Text += number;
            }
            else if (e.KeyCode == Keys.Back)
            {
                if (txtCash.Text.Length > 0) txtCash.Text = txtCash.Text.Substring(0, txtCash.Text.Length - 1);
            }
            else if (e.KeyCode == Keys.Decimal || e.KeyCode == Keys.OemPeriod)
            {
                if (!txtCash.Text.Contains(".")) txtCash.Text += ".";
            }
            else if (e.KeyCode == Keys.Add) btnOperator_Click(btnAdd, EventArgs.Empty);
            else if (e.KeyCode == Keys.Subtract) btnOperator_Click(btnSubtract, EventArgs.Empty);
            else if (e.KeyCode == Keys.Multiply) btnOperator_Click(btnMultiply, EventArgs.Empty);
            else if (e.KeyCode == Keys.Divide) btnOperator_Click(btnDivide, EventArgs.Empty);
        }

        private void txtSale_TextChanged(object sender, EventArgs e)
        {
            txtCash_TextChanged(sender, e);
        }

        #endregion

        private void btn1_Click(object sender, EventArgs e) => btnNumber_Click(sender, e);
        private void btn2_Click(object sender, EventArgs e) => btnNumber_Click(sender, e);
        private void btn3_Click(object sender, EventArgs e) => btnNumber_Click(sender, e);
        private void btn4_Click(object sender, EventArgs e) => btnNumber_Click(sender, e);
        private void btn5_Click(object sender, EventArgs e) => btnNumber_Click(sender, e);
        private void btn6_Click(object sender, EventArgs e) => btnNumber_Click(sender, e);
        private void btn7_Click(object sender, EventArgs e) => btnNumber_Click(sender, e);
        private void btn8_Click(object sender, EventArgs e) => btnNumber_Click(sender, e);
        private void btn9_Click(object sender, EventArgs e) => btnNumber_Click(sender, e);
        private void btn0_Click(object sender, EventArgs e) => btnNumber_Click(sender, e);
        private void btn00_Click(object sender, EventArgs e) => btnNumber_Click(sender, e);
    }
}