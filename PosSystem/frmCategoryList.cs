using System;
using System.Data;
using System.Data.SQLite;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PosSystem
{
    public partial class frmCategoryList : Form
    {
        public frmCategoryList()
        {
            InitializeComponent();
            KeyPreview = true;
            _ = LoadCategoryAsync(); // Fire and forget on startup
        }

        private void pictureBox1_Click(object sender, EventArgs e)
        {
            Dispose();
        }

        // =========================
        // 🔹 SYNC WRAPPER FOR LEGACY CALLS
        // =========================
        public void LoadCategory()
        {
            LoadCategoryAsync().Wait();
        }

        // =========================
        // 🔹 LOAD CATEGORY ASYNC
        // =========================
        public async Task LoadCategoryAsync()
        {
            try
            {
                dataGridView1.SuspendLayout();
                dataGridView1.Rows.Clear();

                using (var cn = new SQLiteConnection(DBConnection.MyConnection()))
                {
                    await cn.OpenAsync();

                    string query = "SELECT * FROM TblCategory ORDER BY category";
                    using (var cm = new SQLiteCommand(query, cn))
                    using (var dr = await cm.ExecuteReaderAsync())
                    {
                        int i = 0;
                        while (await dr.ReadAsync())
                        {
                            i++;
                            dataGridView1.Rows.Add(i, dr["id"].ToString(), dr["category"].ToString());
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                dataGridView1.ResumeLayout();
            }
        }

        private void pictureBox2_Click(object sender, EventArgs e) // Add Category
        {
            var frm = new frmCategory(this)
            {
                btnSave = { Enabled = true },
                btnUpdate = { Enabled = false }
            };
            frm.ShowDialog();
        }

        // =========================
        // 🔹 EDIT / DELETE CATEGORY
        // =========================
        private async void dataGridView1_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;

            string colName = dataGridView1.Columns[e.ColumnIndex].Name;

            try
            {
                if (colName == "Edit")
                {
                    var frm = new frmCategory(this)
                    {
                        lblId = { Text = dataGridView1.Rows[e.RowIndex].Cells[1].Value.ToString() },
                        txtcategory = { Text = dataGridView1.Rows[e.RowIndex].Cells[2].Value.ToString() }
                    };
                    frm.btnSave.Enabled = false;
                    frm.btnUpdate.Enabled = true;
                    frm.ShowDialog();
                }
                else if (colName == "Delete")
                {
                    if (MessageBox.Show("Are you sure you want to delete this category?", "Delete Category",
                        MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                    {
                        using (var cn = new SQLiteConnection(DBConnection.MyConnection()))
                        {
                            await cn.OpenAsync();
                            using (var cm = new SQLiteCommand("DELETE FROM TblCategory WHERE id = @id", cn))
                            {
                                cm.Parameters.AddWithValue("@id", dataGridView1.Rows[e.RowIndex].Cells[1].Value.ToString());
                                await cm.ExecuteNonQueryAsync();
                            }
                        }

                        MessageBox.Show("Record has been successfully deleted!", "Deleted", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        await LoadCategoryAsync(); // Reload categories
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // Optional: keyboard shortcuts
        private void frmCategoryList_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape)
                Dispose();
        }
    }
}