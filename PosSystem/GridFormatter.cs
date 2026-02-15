using System.Windows.Forms;

namespace PosSystem
{
    public static class GridFormatter
    {
        public static void Format(DataGridView grid)
        {
            if (grid.Columns.Contains("id"))
                grid.Columns["id"].Visible = false;

            if (grid.Columns.Contains("vendor"))
                grid.Columns["vendor"].HeaderText = "Vendor Name";

            grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            grid.MultiSelect = false;
        }
    }
}