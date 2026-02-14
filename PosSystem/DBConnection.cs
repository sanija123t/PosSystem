using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Forms;

namespace PosSystem
{
    public static class DBConnection
    {
        private static readonly string dbDirectory = Path.Combine(Application.StartupPath, "DATABASE");
        private static readonly string dbPath = Path.Combine(dbDirectory, "PosDB.db");

        // ULTRA PERFORMANCE CONNECTION STRING
        private static readonly string con = $"Data Source={dbPath};Version=3;Journal Mode=WAL;Pooling=True;Cache=Shared;Busy Timeout=5000;Page Size=32768;Synchronous=Normal;";

        private static bool _initialized = false;
        private static readonly object _lock = new object();

        public static string MyConnection() => con;

        // SECURE HASH
        public static string GetHash(string password, string salt = "")
        {
            using (SHA256 sha = SHA256.Create())
            {
                byte[] bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(password + salt));
                StringBuilder sb = new StringBuilder();
                foreach (byte b in bytes)
                    sb.Append(b.ToString("x2"));
                return sb.ToString();
            }
        }

        public static void InitializeDatabase()
        {
            if (_initialized) return;

            lock (_lock)
            {
                if (_initialized) return;

                try
                {
                    if (!Directory.Exists(dbDirectory))
                        Directory.CreateDirectory(dbDirectory);

                    if (!File.Exists(dbPath))
                        SQLiteConnection.CreateFile(dbPath);

                    using (var cn = new SQLiteConnection(MyConnection()))
                    {
                        cn.Open();

                        // PRAGMA settings
                        new SQLiteCommand("PRAGMA foreign_keys = ON;", cn).ExecuteNonQuery();
                        new SQLiteCommand("PRAGMA synchronous = NORMAL;", cn).ExecuteNonQuery();
                        new SQLiteCommand("PRAGMA journal_mode = WAL;", cn).ExecuteNonQuery();
                        new SQLiteCommand("PRAGMA temp_store = MEMORY;", cn).ExecuteNonQuery();
                        new SQLiteCommand("PRAGMA cache_size = -20000;", cn).ExecuteNonQuery(); // 20MB cache

                        // CREATE CORE TABLES, TRIGGERS, INDEXES, VIEWS
                        string script = @"
CREATE TABLE IF NOT EXISTS BrandTbl (id INTEGER PRIMARY KEY AUTOINCREMENT, brand TEXT NOT NULL);
CREATE TABLE IF NOT EXISTS TblCategory (id INTEGER PRIMARY KEY AUTOINCREMENT, category TEXT NOT NULL);
CREATE TABLE IF NOT EXISTS tblStore (store TEXT, address TEXT, phone TEXT);
CREATE TABLE IF NOT EXISTS tblVat (id INTEGER PRIMARY KEY AUTOINCREMENT, vat REAL DEFAULT 0);

CREATE TABLE IF NOT EXISTS TblProduct1 (
    pcode TEXT PRIMARY KEY, barcode TEXT, pdesc TEXT NOT NULL, bid INTEGER, cid INTEGER, 
    price REAL DEFAULT 0, cost_price REAL DEFAULT 0, tax_rate REAL DEFAULT 0, 
    sid INTEGER DEFAULT 0, qty INTEGER DEFAULT 0, reorder INTEGER DEFAULT 0, isactive INTEGER DEFAULT 1,
    FOREIGN KEY (bid) REFERENCES BrandTbl(id), FOREIGN KEY (cid) REFERENCES TblCategory(id)
);

CREATE TABLE IF NOT EXISTS tblTransaction (
    transno TEXT PRIMARY KEY, sdate TEXT, subtotal REAL DEFAULT 0, discount REAL DEFAULT 0, 
    vat REAL DEFAULT 0, total REAL DEFAULT 0, payment_type TEXT, cash_tendered REAL DEFAULT 0, 
    cash_change REAL DEFAULT 0, user_id TEXT, status TEXT DEFAULT 'Sold'
);

CREATE TABLE IF NOT EXISTS tblCart1 (
    id INTEGER PRIMARY KEY AUTOINCREMENT, transno TEXT, pcode TEXT, price REAL, qty INTEGER, 
    sdate TEXT, status TEXT DEFAULT 'Pending', disc REAL DEFAULT 0, total REAL DEFAULT 0, [user] TEXT,
    FOREIGN KEY (pcode) REFERENCES TblProduct1(pcode),
    FOREIGN KEY (transno) REFERENCES tblTransaction(transno) ON DELETE CASCADE
);

CREATE TABLE IF NOT EXISTS tblCancel (
    id INTEGER PRIMARY KEY AUTOINCREMENT, transno TEXT, pcode TEXT, price REAL, qty INTEGER, total REAL,
    sdate TEXT, voidby TEXT, cancelledby TEXT, reason TEXT, action TEXT
);

-- Essential Indexes
CREATE INDEX IF NOT EXISTS idx_product_barcode ON TblProduct1(barcode);
CREATE INDEX IF NOT EXISTS idx_cart_transno ON tblCart1(transno);
";

                        using (var cm = new SQLiteCommand(script, cn))
                            cm.ExecuteNonQuery();

                        // ===============================
                        // VERSIONING AND MIGRATION SYSTEM
                        // ===============================
                        EnsureVersioning(cn);   // Make sure tblDBVersion exists
                        MigrateDatabase(cn);    // Run migrations if needed

                        // FIX SCHEMA & SEED DATA
                        FixSchema(cn);
                        SeedData(cn);
                    }

                    _initialized = true;
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Critical Database Error:\n" + ex.Message, "System Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        // ===============================
        // VERSIONING METHODS
        // ===============================
        private static void EnsureVersioning(SQLiteConnection cn)
        {
            new SQLiteCommand("CREATE TABLE IF NOT EXISTS tblDBVersion (version INTEGER);", cn).ExecuteNonQuery();

            using var cmd = new SQLiteCommand("SELECT COUNT(*) FROM tblDBVersion;", cn);
            if (Convert.ToInt32(cmd.ExecuteScalar()) == 0)
                new SQLiteCommand("INSERT INTO tblDBVersion (version) VALUES (1);", cn).ExecuteNonQuery(); // initial version
        }

        private static int GetDBVersion(SQLiteConnection cn)
        {
            using var cmd = new SQLiteCommand("SELECT version FROM tblDBVersion LIMIT 1;", cn);
            var result = cmd.ExecuteScalar();
            if (result == null || result == DBNull.Value)
                return 1;
            return Convert.ToInt32(result);
        }

        private static void SetDBVersion(SQLiteConnection cn, int version)
        {
            new SQLiteCommand($"UPDATE tblDBVersion SET version={version};", cn).ExecuteNonQuery();
        }

        private static void MigrateDatabase(SQLiteConnection cn)
        {
            int dbVersion = GetDBVersion(cn);

            // ================================
            // Version 2: Add tblUser.role column if missing
            // ================================
            if (dbVersion < 2)
            {
                if (!ColumnExists(cn, "tblUser", "role"))
                    new SQLiteCommand("ALTER TABLE tblUser ADD COLUMN role TEXT;", cn).ExecuteNonQuery();

                SetDBVersion(cn, 2);
            }

            // ================================
            // Version 3: Add tblCancel.total column if missing
            // ================================
            if (dbVersion < 3)
            {
                if (!ColumnExists(cn, "tblCancel", "total"))
                    new SQLiteCommand("ALTER TABLE tblCancel ADD COLUMN total REAL DEFAULT 0;", cn).ExecuteNonQuery();

                SetDBVersion(cn, 3);
            }

            // ================================
            // Version 4: Optional: standardize vwSoldItems (user vs cashier) safely
            // ================================
            if (dbVersion < 4)
            {
                using (var tr = cn.BeginTransaction())
                {
                    new SQLiteCommand("DROP VIEW IF EXISTS vwSoldItems;", cn, tr).ExecuteNonQuery();
                    new SQLiteCommand(@"
                        CREATE VIEW IF NOT EXISTS vwSoldItems AS
                        SELECT c.id, c.transno, c.pcode, p.pdesc, c.price, c.qty, c.disc, c.total, c.sdate, c.status, c.user
                        FROM tblCart1 c
                        INNER JOIN TblProduct1 p ON c.pcode = p.pcode;
                    ", cn, tr).ExecuteNonQuery();
                    tr.Commit();
                }

                SetDBVersion(cn, 4);
            }

            // ================================
            // Version 5: Example future migration
            // ================================
            if (dbVersion < 5)
            {
                // Future migrations go here
                // SetDBVersion(cn, 5);
            }
        }

        // ===============================
        // EXISTING FIX SCHEMA & HELPERS
        // ===============================
        private static void FixSchema(SQLiteConnection cn)
        {
            string[,] updates = {
                { "TblProduct1", "cost_price", "REAL DEFAULT 0" },
                { "TblProduct1", "tax_rate", "REAL DEFAULT 0" },
                { "TblProduct1", "sid", "INTEGER DEFAULT 0" },
                { "TblProduct1", "isactive", "INTEGER DEFAULT 1" },
                { "tblUser", "isactive", "INTEGER DEFAULT 1" }
            };

            for (int i = 0; i < updates.GetLength(0); i++)
            {
                if (!ColumnExists(cn, updates[i, 0], updates[i, 1]))
                {
                    using (var cmd = new SQLiteCommand($"ALTER TABLE {updates[i, 0]} ADD COLUMN {updates[i, 1]} {updates[i, 2]};", cn))
                        cmd.ExecuteNonQuery();
                }
            }
        }

        private static bool ColumnExists(SQLiteConnection cn, string tableName, string columnName)
        {
            using (var cmd = new SQLiteCommand($"PRAGMA table_info({tableName});", cn))
            using (var reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                    if (reader["name"].ToString().Equals(columnName, StringComparison.OrdinalIgnoreCase))
                        return true;
            }
            return false;
        }

        private static void SeedData(SQLiteConnection cn)
        {
            using (var cmd = new SQLiteCommand("SELECT COUNT(*) FROM tblVat", cn))
                if (Convert.ToInt32(cmd.ExecuteScalar()) == 0)
                    new SQLiteCommand("INSERT INTO tblVat (vat) VALUES (0)", cn).ExecuteNonQuery();

            using (var cmd = new SQLiteCommand("SELECT COUNT(*) FROM tblUser", cn))
            {
                if (Convert.ToInt32(cmd.ExecuteScalar()) == 0)
                {
                    string salt = Guid.NewGuid().ToString("N");
                    var ins = new SQLiteCommand("INSERT INTO tblUser (username,password,salt,role,name,isactive) VALUES ('admin',@p,@s,'Administrator','System Admin',1)", cn);
                    ins.Parameters.AddWithValue("@s", salt);
                    ins.Parameters.AddWithValue("@p", GetHash("admin123", salt));
                    ins.ExecuteNonQuery();
                }
            }

            // Ensure tblStore has at least one row
            using (var cmd = new SQLiteCommand("SELECT COUNT(*) FROM tblStore;", cn))
                if (Convert.ToInt32(cmd.ExecuteScalar()) == 0)
                    new SQLiteCommand("INSERT INTO tblStore (store,address,phone) VALUES ('Default Store','Default Address','0000000000');", cn).ExecuteNonQuery();
        }

        // DASHBOARD
        public static double DailySales()
        {
            try
            {
                using var cn = new SQLiteConnection(con); cn.Open();
                using var cm = new SQLiteCommand("SELECT IFNULL(SUM(total),0) FROM tblTransaction WHERE DATE(sdate)=DATE('now','localtime') AND status='Sold'", cn);
                return Convert.ToDouble(cm.ExecuteScalar() ?? 0);
            }
            catch { return 0; }
        }

        public static double ProductLine() { try { using var cn = new SQLiteConnection(con); cn.Open(); using var cm = new SQLiteCommand("SELECT COUNT(*) FROM TblProduct1 WHERE isactive=1", cn); return Convert.ToDouble(cm.ExecuteScalar() ?? 0); } catch { return 0; } }
        public static double StockOnHand() { try { using var cn = new SQLiteConnection(con); cn.Open(); using var cm = new SQLiteCommand("SELECT IFNULL(SUM(qty),0) FROM TblProduct1 WHERE isactive=1", cn); return Convert.ToDouble(cm.ExecuteScalar() ?? 0); } catch { return 0; } }
        public static double CriticalItems() { try { using var cn = new SQLiteConnection(con); cn.Open(); using var cm = new SQLiteCommand("SELECT COUNT(*) FROM vwCriticalItems", cn); return Convert.ToDouble(cm.ExecuteScalar() ?? 0); } catch { return 0; } }
    }
}