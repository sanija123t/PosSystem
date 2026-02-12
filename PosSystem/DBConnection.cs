using System;
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
        private static readonly string con = $"Data Source={dbPath};Version=3;Journal Mode=WAL;Pooling=True;";
        private static bool _initialized = false;
        private static readonly object _lock = new object();

        public static string MyConnection() => con;

        // SHA256 password hash with optional salt
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

        // Database bootstrapper
        public static void InitializeDatabase()
        {
            if (_initialized) return;

            lock (_lock)
            {
                if (_initialized) return;
                _initialized = true;

                try
                {
                    if (!Directory.Exists(dbDirectory))
                        Directory.CreateDirectory(dbDirectory);

                    if (!File.Exists(dbPath))
                        SQLiteConnection.CreateFile(dbPath);

                    using (var cn = new SQLiteConnection(MyConnection()))
                    {
                        cn.Open();

                        using (var pragma = new SQLiteCommand("PRAGMA foreign_keys = ON;", cn))
                            pragma.ExecuteNonQuery();

                        string script = @"
-- Tables
CREATE TABLE IF NOT EXISTS BrandTbl (id INTEGER PRIMARY KEY AUTOINCREMENT, brand TEXT NOT NULL);
CREATE TABLE IF NOT EXISTS TblCategory (id INTEGER PRIMARY KEY AUTOINCREMENT, category TEXT NOT NULL);
CREATE TABLE IF NOT EXISTS tblStore (store TEXT, address TEXT, phone TEXT);
CREATE TABLE IF NOT EXISTS tblVat (id INTEGER PRIMARY KEY AUTOINCREMENT, vat REAL DEFAULT 0);
CREATE TABLE IF NOT EXISTS TblProduct1 (
    pcode TEXT PRIMARY KEY,
    barcode TEXT,
    pdesc TEXT NOT NULL,
    bid INTEGER,
    cid INTEGER,
    price REAL DEFAULT 0,
    qty INTEGER DEFAULT 0,
    reorder INTEGER DEFAULT 0,
    FOREIGN KEY (bid) REFERENCES BrandTbl(id),
    FOREIGN KEY (cid) REFERENCES TblCategory(id)
);
CREATE TABLE IF NOT EXISTS tblUser (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    username TEXT UNIQUE,
    password TEXT,
    salt TEXT DEFAULT '',
    role TEXT,
    name TEXT,
    isactive INTEGER DEFAULT 1,
    isdeleted INTEGER DEFAULT 0
);
CREATE TABLE IF NOT EXISTS tblCart1 (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    transno TEXT,
    pcode TEXT,
    price REAL,
    qty INTEGER,
    sdate TEXT,
    status TEXT DEFAULT 'Pending',
    disc REAL DEFAULT 0,
    total REAL DEFAULT 0,
    user TEXT,
    FOREIGN KEY (pcode) REFERENCES TblProduct1(pcode)
);
CREATE TABLE IF NOT EXISTS tblAdjustment (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    referenceno TEXT,
    pcode TEXT,
    qty INTEGER,
    action TEXT,
    remarks TEXT,
    sdate TEXT,
    [user] TEXT,
    FOREIGN KEY (pcode) REFERENCES TblProduct1(pcode)
);
CREATE TABLE IF NOT EXISTS tblVendor (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    vendor TEXT,
    address TEXT,
    contactperson TEXT,
    telephone TEXT,
    email TEXT,
    fax TEXT
);
CREATE TABLE IF NOT EXISTS tblStockIn (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    refno TEXT,
    pcode TEXT,
    qty INTEGER DEFAULT 0,
    sdate TEXT,
    stockinby TEXT,
    vendorid INTEGER,
    status TEXT DEFAULT 'Pending',
    FOREIGN KEY (pcode) REFERENCES TblProduct1(pcode),
    FOREIGN KEY (vendorid) REFERENCES tblVendor(id)
);
CREATE TABLE IF NOT EXISTS tblCancel (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    transno TEXT,
    pcode TEXT,
    price REAL,
    qty INTEGER,
    total REAL,
    sdate TEXT,
    voidby TEXT,
    cancelledby TEXT,
    reason TEXT,
    action TEXT
);

-- Views
DROP VIEW IF EXISTS vwInventory;
CREATE VIEW vwInventory AS
SELECT p.pcode, p.barcode, p.pdesc, b.brand, c.category, p.price, p.qty, p.reorder
FROM TblProduct1 p
INNER JOIN BrandTbl b ON b.id = p.bid
INNER JOIN TblCategory c ON c.id = p.cid;

DROP VIEW IF EXISTS vwCriticalItems;
CREATE VIEW vwCriticalItems AS
SELECT * FROM vwInventory WHERE qty <= reorder;

DROP VIEW IF EXISTS vwSoldItems;
CREATE VIEW vwSoldItems AS
SELECT c.id, c.transno, c.pcode, p.pdesc, c.price, c.qty, c.disc, c.total, c.sdate, c.status, c.user
FROM tblCart1 c
INNER JOIN TblProduct1 p ON c.pcode = p.pcode;

DROP VIEW IF EXISTS vwCancelledOrder;
CREATE VIEW vwCancelledOrder AS
SELECT c.id, c.transno, c.pcode, p.pdesc, c.price, c.qty, c.total, c.sdate, c.voidby, c.cancelledby, c.reason, c.action
FROM tblCancel c
INNER JOIN TblProduct1 p ON c.pcode = p.pcode;
";

                        using (var cm = new SQLiteCommand(script, cn))
                            cm.ExecuteNonQuery();

                        // SAFETY CHECK: Add isactive column if it doesn't exist (for existing databases)
                        try
                        {
                            using (var checkCol = new SQLiteCommand("PRAGMA table_info(tblUser);", cn))
                            using (var reader = checkCol.ExecuteReader())
                            {
                                bool exists = false;
                                while (reader.Read())
                                {
                                    if (reader["name"].ToString().Equals("isactive", StringComparison.OrdinalIgnoreCase))
                                    {
                                        exists = true;
                                        break;
                                    }
                                }
                                if (!exists)
                                {
                                    using (var alter = new SQLiteCommand("ALTER TABLE tblUser ADD COLUMN isactive INTEGER DEFAULT 1;", cn))
                                        alter.ExecuteNonQuery();
                                }
                            }
                        }
                        catch { /* Column already exists or table busy */ }

                        // VAT row
                        using (var vatCheck = new SQLiteCommand("SELECT COUNT(*) FROM tblVat", cn))
                        {
                            if (Convert.ToInt32(vatCheck.ExecuteScalar()) == 0)
                            {
                                using (var insertVat = new SQLiteCommand("INSERT INTO tblVat (vat) VALUES (0)", cn))
                                    insertVat.ExecuteNonQuery();
                            }
                        }

                        // Admin user with salt
                        using (var userCheck = new SQLiteCommand("SELECT COUNT(*) FROM tblUser", cn))
                        {
                            if (Convert.ToInt32(userCheck.ExecuteScalar()) == 0)
                            {
                                string salt = Guid.NewGuid().ToString("N");
                                using (var insertAdmin = new SQLiteCommand(
                                    "INSERT INTO tblUser (username,password,salt,role,name,isactive) VALUES ('admin',@pass,@salt,'Administrator','System Admin', 1)", cn))
                                {
                                    insertAdmin.Parameters.AddWithValue("@salt", salt);
                                    insertAdmin.Parameters.AddWithValue("@pass", GetHash("admin123", salt));
                                    insertAdmin.ExecuteNonQuery();
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Critical Database Error:\n" + ex.Message,
                        "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        // VAT
        public static double GetVal()
        {
            try
            {
                using var cn = new SQLiteConnection(MyConnection());
                cn.Open();
                using var cm = new SQLiteCommand("SELECT vat FROM tblVat LIMIT 1", cn);
                return Convert.ToDouble(cm.ExecuteScalar() ?? 0);
            }
            catch { return 0; }
        }

        // Today sales (localtime)
        public static double DailySales()
        {
            try
            {
                using var cn = new SQLiteConnection(MyConnection());
                cn.Open();
                using var cm = new SQLiteCommand(
                    "SELECT IFNULL(SUM(total),0) FROM tblCart1 WHERE DATE(sdate)=DATE('now', 'localtime') AND status='Sold'", cn);
                return Convert.ToDouble(cm.ExecuteScalar() ?? 0);
            }
            catch { return 0; }
        }

        public static double ProductLine()
        {
            try
            {
                using var cn = new SQLiteConnection(MyConnection());
                cn.Open();
                using var cm = new SQLiteCommand("SELECT COUNT(*) FROM TblProduct1", cn);
                return Convert.ToDouble(cm.ExecuteScalar() ?? 0);
            }
            catch { return 0; }
        }

        public static double StockOnHand()
        {
            try
            {
                using var cn = new SQLiteConnection(MyConnection());
                cn.Open();
                using var cm = new SQLiteCommand("SELECT IFNULL(SUM(qty),0) FROM TblProduct1", cn);
                return Convert.ToDouble(cm.ExecuteScalar() ?? 0);
            }
            catch { return 0; }
        }

        public static bool HasAnyUser()
        {
            try
            {
                using var cn = new SQLiteConnection(MyConnection());
                cn.Open();
                using var cm = new SQLiteCommand("SELECT COUNT(*) FROM tblUser WHERE isdeleted=0", cn);
                return Convert.ToInt32(cm.ExecuteScalar() ?? 0) > 0;
            }
            catch { return false; }
        }

        public static double CriticalItems()
        {
            try
            {
                using var cn = new SQLiteConnection(MyConnection());
                cn.Open();
                using var cm = new SQLiteCommand("SELECT COUNT(*) FROM vwCriticalItems", cn);
                return Convert.ToDouble(cm.ExecuteScalar() ?? 0);
            }
            catch { return 0; }
        }

        public static double CriticalItemsCount() => CriticalItems();
    }
}