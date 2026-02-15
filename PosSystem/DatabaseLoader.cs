using System.Data;
using System.Data.SQLite;

namespace PosSystem
{
    public static class DatabaseLoader
    {
        public static DataTable Load(string table)
        {
            using (SQLiteConnection cn = new SQLiteConnection(DBConnection.MyConnection()))
            {
                cn.Open();

                using (SQLiteDataAdapter da = new SQLiteDataAdapter($"SELECT * FROM {table}", cn))
                {
                    DataTable dt = new DataTable();
                    da.Fill(dt);
                    return dt;
                }
            }
        }

        public static DataTable LoadSchema(string table)
        {
            using (SQLiteConnection cn = new SQLiteConnection(DBConnection.MyConnection()))
            {
                cn.Open();

                using (SQLiteDataAdapter da = new SQLiteDataAdapter($"SELECT * FROM {table} LIMIT 0", cn))
                {
                    DataTable dt = new DataTable();
                    da.FillSchema(dt, SchemaType.Source);
                    return dt;
                }
            }
        }
    }
}