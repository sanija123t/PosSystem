using System;
using System.Data;
using System.Data.SQLite;
using System.Windows.Forms;

namespace PosSystem
{
    public partial class frmProduct : Form
    {
        frmProduct_List flist;

        public frmProduct(frmProduct_List frm)
        {
            InitializeComponent();
            flist = frm;
        }

        public void LocalCategory()
        {
            comboBox2.Items.Clear();
            using (SQLiteConnection con = new SQLiteConnection(DBConnection.MyConnection()))
            {
                con.Open();
                using (SQLiteCommand cmd = new SQLiteCommand("SELECT category FROM TblCategory", con))
                using (SQLiteDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        comboBox2.Items.Add(reader[0].ToString());
                    }
                }
            }
        }

        public void LocalBrand()
        {
            comboBox1.Items.Clear();
            using (SQLiteConnection con = new SQLiteConnection(DBConnection.MyConnection()))
            {
                con.Open();
                using (SQLiteCommand cmd = new SQLiteCommand("SELECT brand FROM BrandTbl", con))
                using (SQLiteDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        comboBox1.Items.Add(reader[0].ToString());
                    }
                }
            }
        }

        private void pictureBox2_Click(object sender, EventArgs e)
        {
            this.Dispose();
        }

        private void frmProduct_Load(object sender, EventArgs e)
        {
            LocalBrand();
            LocalCategory();
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            try
            {
                if (string.IsNullOrEmpty(TxtPcode.Text) || string.IsNullOrEmpty(txtBarcode.Text))
                {
                    MessageBox.Show("Please fill in required fields.", "Input Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                if (comboBox1.SelectedIndex < 0 || comboBox2.SelectedIndex < 0)
                {
                    MessageBox.Show("Please select a Brand and Category.", "Selection Required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                if (!double.TryParse(txtPrice.Text, out double price))
                {
                    MessageBox.Show("Invalid Price value.", "Input Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    txtPrice.Focus();
                    return;
                }

                if (!int.TryParse(txtReOrder.Text, out int reorder))
                {
                    MessageBox.Show("Invalid Reorder value.", "Input Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    txtReOrder.Focus();
                    return;
                }

                if (MessageBox.Show("Save this product?", "Confirm", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    using (SQLiteConnection connection = new SQLiteConnection(DBConnection.MyConnection()))
                    {
                        connection.Open();

                        // Check for duplicate Barcode
                        using (SQLiteCommand cmdCheck = new SQLiteCommand(
                            "SELECT COUNT(*) FROM TblProduct1 WHERE barcode = @barcode", connection))
                        {
                            cmdCheck.Parameters.AddWithValue("@barcode", txtBarcode.Text);
                            int count = Convert.ToInt32(cmdCheck.ExecuteScalar());
                            if (count > 0)
                            {
                                MessageBox.Show("Barcode already exists!", "Duplicate", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                                return;
                            }
                        }

                        // Get BrandID and CategoryID
                        string brandID = null, categoryID = null;

                        using (SQLiteCommand cmd = new SQLiteCommand("SELECT id FROM BrandTbl WHERE brand = @brand", connection))
                        {
                            cmd.Parameters.AddWithValue("@brand", comboBox1.Text);
                            using (SQLiteDataReader dr = cmd.ExecuteReader())
                            {
                                if (dr.Read()) brandID = dr[0].ToString();
                            }
                        }

                        using (SQLiteCommand cmd = new SQLiteCommand("SELECT id FROM TblCategory WHERE category = @category", connection))
                        {
                            cmd.Parameters.AddWithValue("@category", comboBox2.Text);
                            using (SQLiteDataReader dr = cmd.ExecuteReader())
                            {
                                if (dr.Read()) categoryID = dr[0].ToString();
                            }
                        }

                        if (string.IsNullOrEmpty(brandID) || string.IsNullOrEmpty(categoryID))
                        {
                            MessageBox.Show("Invalid Brand or Category selection.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            return;
                        }

                        // Insert product
                        using (SQLiteCommand cmd = new SQLiteCommand(
                            @"INSERT INTO TblProduct1 (pcode, barcode, pdesc, bid, cid, price, reorder) 
                              VALUES (@pcode, @barcode, @pdesc, @bid, @cid, @price, @reorder)", connection))
                        {
                            cmd.Parameters.AddWithValue("@pcode", TxtPcode.Text);
                            cmd.Parameters.AddWithValue("@barcode", txtBarcode.Text);
                            cmd.Parameters.AddWithValue("@pdesc", txtPdesc.Text);
                            cmd.Parameters.AddWithValue("@bid", brandID);
                            cmd.Parameters.AddWithValue("@cid", categoryID);
                            cmd.Parameters.AddWithValue("@price", price);
                            cmd.Parameters.AddWithValue("@reorder", reorder);
                            cmd.ExecuteNonQuery();
                        }
                    }

                    MessageBox.Show("Product saved successfully!");
                    Clear();
                    flist.LoadRecords();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message);
            }
        }

        public void Clear()
        {
            txtPrice.Clear();
            txtBarcode.Clear();
            txtPdesc.Clear();
            TxtPcode.Clear();
            txtReOrder.Clear();
            comboBox1.Text = "";
            comboBox2.Text = "";
            TxtPcode.Focus();
            btnSave.Enabled = true;
            btnUpdate.Enabled = false;
        }

        public void Clear1()
        {
            txtPrice.Clear();
            txtBarcode.Clear();
            txtPdesc.Clear();
            TxtPcode.Clear();
            txtReOrder.Clear();
            comboBox1.Text = "";
            comboBox2.Text = "";
            TxtPcode.Focus();
            btnSave.Enabled = false;
            btnUpdate.Enabled = true;
        }

        private void btnUpdate_Click(object sender, EventArgs e)
        {
            try
            {
                if (MessageBox.Show("Are you sure you want to update this product?", "Update product", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    if (!double.TryParse(txtPrice.Text, out double price))
                    {
                        MessageBox.Show("Invalid Price value.", "Input Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        txtPrice.Focus();
                        return;
                    }

                    if (!int.TryParse(txtReOrder.Text, out int reorder))
                    {
                        MessageBox.Show("Invalid Reorder value.", "Input Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        txtReOrder.Focus();
                        return;
                    }

                    using (SQLiteConnection connection = new SQLiteConnection(DBConnection.MyConnection()))
                    {
                        connection.Open();

                        // Check for duplicate Barcode (excluding current product)
                        using (SQLiteCommand cmdCheck = new SQLiteCommand(
                            "SELECT COUNT(*) FROM TblProduct1 WHERE barcode = @barcode AND pcode != @pcode", connection))
                        {
                            cmdCheck.Parameters.AddWithValue("@barcode", txtBarcode.Text);
                            cmdCheck.Parameters.AddWithValue("@pcode", TxtPcode.Text);
                            int count = Convert.ToInt32(cmdCheck.ExecuteScalar());
                            if (count > 0)
                            {
                                MessageBox.Show("Barcode already assigned to another product!", "Duplicate Barcode", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                                return;
                            }
                        }

                        // Get BrandID and CategoryID
                        string brandID = null, categoryID = null;

                        using (SQLiteCommand cmd = new SQLiteCommand("SELECT id FROM BrandTbl WHERE brand = @brand", connection))
                        {
                            cmd.Parameters.AddWithValue("@brand", comboBox1.Text);
                            using (SQLiteDataReader dr = cmd.ExecuteReader())
                            {
                                if (dr.Read()) brandID = dr[0].ToString();
                            }
                        }

                        using (SQLiteCommand cmd = new SQLiteCommand("SELECT id FROM TblCategory WHERE category = @category", connection))
                        {
                            cmd.Parameters.AddWithValue("@category", comboBox2.Text);
                            using (SQLiteDataReader dr = cmd.ExecuteReader())
                            {
                                if (dr.Read()) categoryID = dr[0].ToString();
                            }
                        }

                        if (string.IsNullOrEmpty(brandID) || string.IsNullOrEmpty(categoryID))
                        {
                            MessageBox.Show("Invalid Brand or Category selection.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            return;
                        }

                        // Update product
                        using (SQLiteCommand cmd = new SQLiteCommand(
                            @"UPDATE TblProduct1 
                              SET barcode=@barcode, pdesc=@pdesc, bid=@bid, cid=@cid, price=@price, reorder=@reorder 
                              WHERE pcode=@pcode", connection))
                        {
                            cmd.Parameters.AddWithValue("@pcode", TxtPcode.Text);
                            cmd.Parameters.AddWithValue("@barcode", txtBarcode.Text);
                            cmd.Parameters.AddWithValue("@pdesc", txtPdesc.Text);
                            cmd.Parameters.AddWithValue("@bid", brandID);
                            cmd.Parameters.AddWithValue("@cid", categoryID);
                            cmd.Parameters.AddWithValue("@price", price);
                            cmd.Parameters.AddWithValue("@reorder", reorder);
                            cmd.ExecuteNonQuery();
                        }
                    }

                    MessageBox.Show("Product has been successfully updated.");
                    Clear1();
                    flist.LoadRecords();
                    this.Dispose();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message);
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            Clear();
        }

        private void txtPrice_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar) && (e.KeyChar != '.'))
                e.Handled = true;

            if ((e.KeyChar == '.') && ((sender as TextBox).Text.IndexOf('.') > -1))
                e.Handled = true;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            frmscanBarcode frm = new frmscanBarcode(this);
            frm.Show();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            frmCreatebarcode FRM = new frmCreatebarcode();
            FRM.ShowDialog();
        }

        private void textBox1_TextChanged(object sender, EventArgs e) { }
    }
}