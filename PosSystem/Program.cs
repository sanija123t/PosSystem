using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Windows.Forms;

namespace PosSystem
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            try
            {
                // 1. Initialize DB and Create basic tables if they don't exist
                DBConnection.InitializeDatabase();

                // 2. RUN DATABASE PATCH (Adds new columns like cost_price, tax_rate, etc.)
                UpdateDatabaseSchema();

                // 3. Start directly with the main form, bypassing login
                Application.Run(new Form1()); // <-- Login bypassed here
            }
            catch (Exception ex)
            {
                MessageBox.Show("Startup Error: " + ex.Message, "System Failure", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Automatically checks for missing columns and adds them to TblProduct1
        /// This ensures your DB is always up-to-date with our latest changes.
        /// </summary>
        private static void UpdateDatabaseSchema()
        {
            using (SQLiteConnection con = new SQLiteConnection(DBConnection.MyConnection()))
            {
                con.Open();

                // Dictionary: Key = Column Name, Value = SQL Command to add it
                var columnsToAdd = new Dictionary<string, string>
                {
                    { "cost_price", "ALTER TABLE TblProduct1 ADD COLUMN cost_price DECIMAL(18,2) DEFAULT 0" },
                    { "sid", "ALTER TABLE TblProduct1 ADD COLUMN sid INTEGER DEFAULT 0" },
                    { "isactive", "ALTER TABLE TblProduct1 ADD COLUMN isactive INTEGER DEFAULT 1" },
                    { "tax_rate", "ALTER TABLE TblProduct1 ADD COLUMN tax_rate DECIMAL(18,2) DEFAULT 0" }
                };

                foreach (var col in columnsToAdd)
                {
                    if (!ColumnExists(con, "TblProduct1", col.Key))
                    {
                        using (SQLiteCommand cmd = new SQLiteCommand(col.Value, con))
                        {
                            cmd.ExecuteNonQuery();
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Helper to check if a specific column exists in a table
        /// </summary>
        private static bool ColumnExists(SQLiteConnection con, string tableName, string columnName)
        {
            using (SQLiteCommand cmd = new SQLiteCommand($"PRAGMA table_info({tableName})", con))
            using (SQLiteDataReader reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    // Index 1 of PRAGMA table_info is the column name
                    if (reader[1].ToString().Equals(columnName, StringComparison.OrdinalIgnoreCase))
                        return true;
                }
            }
            return false;
        }
    }
}