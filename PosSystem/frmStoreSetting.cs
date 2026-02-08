using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Data.SQLite;

namespace PosSystem
{
    public partial class frmStoreSetting : Form
    {
        SQLiteConnection cn;
        SQLiteCommand cm;
        SQLiteDataReader dr;
        string stitle = "PosSystem";

        public frmStoreSetting()
        {
            InitializeComponent();
            cn = new SQLiteConnection(DBConnection.MyConnection());
        }

        private void pictureBox2_Click(object sender, EventArgs e)
        {
            this.Dispose();
        }

        // Run this once or manually in your DB Manager to fix the "no such table" error
        // CREATE TABLE "tblStore" ("store" TEXT, "address" TEXT, "phone" TEXT);

        public void LoadRecord()
        {
            try
            {
                cn.Open();
                cm = new SQLiteCommand("SELECT * FROM tblStore", cn);
                dr = cm.ExecuteReader();
                if (dr.Read())
                {
                    txtStore.Text = dr["store"].ToString();
                    txtAddress.Text = dr["address"].ToString();
                    txtPhone.Text = dr["phone"].ToString();
                }
                else
                {
                    txtStore.Clear();
                    txtAddress.Clear();
                    txtPhone.Clear();
                }
                dr.Close();
                cn.Close();
            }
            catch (Exception ex)
            {
                if (cn.State == ConnectionState.Open) cn.Close();
                MessageBox.Show(ex.Message, stitle, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            try
            {
                if (MessageBox.Show("Save store details?", stitle, MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    cn.Open();
                    cm = new SQLiteCommand("SELECT COUNT(*) FROM tblStore", cn);
                    int count = Convert.ToInt32(cm.ExecuteScalar());
                    cn.Close();

                    cn.Open();
                    if (count > 0)
                    {
                        cm = new SQLiteCommand("UPDATE tblStore SET store=@store, address=@address, phone=@phone", cn);
                    }
                    else
                    {
                        cm = new SQLiteCommand("INSERT INTO tblStore (store, address, phone) VALUES (@store, @address, @phone)", cn);
                    }

                    cm.Parameters.AddWithValue("@store", txtStore.Text);
                    cm.Parameters.AddWithValue("@address", txtAddress.Text);
                    cm.Parameters.AddWithValue("@phone", txtPhone.Text);
                    cm.ExecuteNonQuery();
                    cn.Close();

                    MessageBox.Show("Store details has been successfully saved!", stitle, MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                if (cn.State == ConnectionState.Open) cn.Close();
                MessageBox.Show(ex.Message, stitle, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void frmStoreSetting_Load(object sender, EventArgs e)
        {
            LoadRecord();
        }
    }
}