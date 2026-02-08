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
        private static readonly string dbPath =
            Path.Combine(Application.StartupPath, "DATABASE", "PosDB.db");

        // Fixed: Removed 'Version=3;' which caused the 'Keyword not supported' error
        private static readonly string con =
            $"Data Source={dbPath};Journal Mode=WAL;";

        private static bool _initialized = false;

        public static string MyConnection()
        {
            return con;
        }

        public static string GetHash(string password)
        {
            using (SHA256 sha256Hash = SHA256.Create())
            {
                byte[] bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(password));
                StringBuilder builder = new StringBuilder();
                foreach (byte b in bytes)
                    builder.Append(b.ToString("x2"));
                return builder.ToString();
            }
        }

        public static void InitializeDatabase()
        {
            if (_initialized) return;
            _initialized = true;

            try
            {
                string directory = Path.GetDirectoryName(dbPath);
                if (!Directory.Exists(directory))
                    Directory.CreateDirectory(directory);

                if (!File.Exists(dbPath))
                    SQLiteConnection.CreateFile(dbPath);

                using (SQLiteConnection cn = new SQLiteConnection(MyConnection()))
                {
                    cn.Open();

                    string script = @"
                    CREATE TABLE IF NOT EXISTS BrandTbl (
                        id INTEGER PRIMARY KEY AUTOINCREMENT,
                        brand TEXT NOT NULL
                    );

                    CREATE TABLE IF NOT EXISTS TblCategory (
                        id INTEGER PRIMARY KEY AUTOINCREMENT,
                        category TEXT NOT NULL
                    );

                    CREATE TABLE IF NOT EXISTS tblStore (
                        store TEXT,
                        address TEXT,
                        phone TEXT
                    );

                    CREATE TABLE IF NOT EXISTS tblVat (
                        id INTEGER PRIMARY KEY AUTOINCREMENT,
                        vat REAL DEFAULT 0
                    );

                    CREATE TABLE IF NOT EXISTS TblProduct1 (
                        pcode TEXT PRIMARY KEY,
                        barcode TEXT,
                        pdesc TEXT NOT NULL,
                        bid INTEGER,
                        cid INTEGER,
                        price REAL DEFAULT 0,
                        qty INTEGER DEFAULT 0,
                        reorder INTEGER DEFAULT 0
                    );

                    CREATE TABLE IF NOT EXISTS tblUser (
                        id INTEGER PRIMARY KEY AUTOINCREMENT,
                        username TEXT UNIQUE,
                        password TEXT,
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
                        total REAL DEFAULT 0
                    );

                    CREATE TABLE IF NOT EXISTS tblAdjustment (
                        id INTEGER PRIMARY KEY AUTOINCREMENT,
                        referenceno TEXT,
                        pcode TEXT,
                        qty INTEGER,
                        action TEXT,
                        remarks TEXT,
                        sdate TEXT,
                        [user] TEXT
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
                        status TEXT DEFAULT 'Pending'
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

                    DROP VIEW IF EXISTS vwCriticalItems;
                    CREATE VIEW vwCriticalItems AS 
                    SELECT * FROM TblProduct1 WHERE qty <= reorder;

                    DROP VIEW IF EXISTS vwInventory;
                    CREATE VIEW vwInventory AS
                    SELECT p.pcode, p.barcode, p.pdesc, b.brand, c.category, p.price, p.qty, p.reorder
                    FROM TblProduct1 as p
                    INNER JOIN BrandTbl as b ON b.id = p.bid
                    INNER JOIN TblCategory as c ON c.id = p.cid;

                    DROP VIEW IF EXISTS vwSoldItems;
                    CREATE VIEW vwSoldItems AS
                    SELECT c.pcode, p.pdesc, c.qty, c.total, c.sdate, c.status, c.disc, c.price
                    FROM tblCart1 AS c
                    INNER JOIN TblProduct1 AS p ON c.pcode = p.pcode;

                    DROP VIEW IF EXISTS vwStockin;
                    CREATE VIEW vwStockin AS
                    SELECT s.id, s.refno, s.pcode, p.pdesc, s.qty, s.sdate, s.stockinby, v.vendor, s.status
                    FROM tblStockIn AS s
                    INNER JOIN TblProduct1 AS p ON s.pcode = p.pcode
                    INNER JOIN tblVendor AS v ON s.vendorid = v.id;

                    DROP VIEW IF EXISTS vwCancelledOrder;
                    CREATE VIEW vwCancelledOrder AS
                    SELECT c.id, c.transno, c.pcode, p.pdesc, c.price, c.qty, c.total, c.sdate, c.voidby, c.cancelledby, c.reason, c.action
                    FROM tblCancel AS c
                    INNER JOIN TblProduct1 AS p ON c.pcode = p.pcode;";

                    using (SQLiteCommand cm = new SQLiteCommand(script, cn))
                    {
                        cm.ExecuteNonQuery();
                    }

                    using (SQLiteCommand cmCheck = new SQLiteCommand("SELECT COUNT(*) FROM tblUser", cn))
                    {
                        if (Convert.ToInt32(cmCheck.ExecuteScalar()) == 0)
                        {
                            string adminPass = GetHash("admin123");
                            using (SQLiteCommand cmInsert = new SQLiteCommand(
                                "INSERT INTO tblUser (username, password, role, name) VALUES ('admin', @pass, 'Administrator', 'System Admin')", cn))
                            {
                                cmInsert.Parameters.AddWithValue("@pass", adminPass);
                                cmInsert.ExecuteNonQuery();
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("DB Init Error: " + ex.Message);
            }
        }

        public static double GetVal()
        {
            try
            {
                using (SQLiteConnection cn = new SQLiteConnection(MyConnection()))
                {
                    cn.Open();
                    using (SQLiteCommand cm = new SQLiteCommand("SELECT vat FROM tblVat LIMIT 1", cn))
                    {
                        object result = cm.ExecuteScalar();
                        return result != null && result != DBNull.Value ? Convert.ToDouble(result) : 0;
                    }
                }
            }
            catch { return 0; }
        }

        public static double DailySales()
        {
            try
            {
                using (SQLiteConnection cn = new SQLiteConnection(MyConnection()))
                {
                    cn.Open();
                    using (SQLiteCommand cm = new SQLiteCommand(
                        "SELECT TOTAL(qty * price) FROM tblCart1 WHERE sdate LIKE @d AND status='Sold'", cn))
                    {
                        cm.Parameters.AddWithValue("@d", DateTime.Now.ToString("yyyy-MM-dd") + "%");
                        return Convert.ToDouble(cm.ExecuteScalar());
                    }
                }
            }
            catch { return 0; }
        }

        public static double ProductLine()
        {
            try
            {
                using (SQLiteConnection cn = new SQLiteConnection(MyConnection()))
                {
                    cn.Open();
                    using (SQLiteCommand cm = new SQLiteCommand("SELECT COUNT(*) FROM TblProduct1", cn))
                    {
                        return Convert.ToDouble(cm.ExecuteScalar());
                    }
                }
            }
            catch { return 0; }
        }

        public static double StockOnHand()
        {
            try
            {
                using (SQLiteConnection cn = new SQLiteConnection(MyConnection()))
                {
                    cn.Open();
                    using (SQLiteCommand cm = new SQLiteCommand("SELECT TOTAL(qty) FROM TblProduct1", cn))
                    {
                        return Convert.ToDouble(cm.ExecuteScalar());
                    }
                }
            }
            catch { return 0; }
        }

        public static bool HasAnyUser()
        {
            try
            {
                using (SQLiteConnection cn = new SQLiteConnection(MyConnection()))
                {
                    cn.Open();
                    using (SQLiteCommand cm = new SQLiteCommand("SELECT COUNT(*) FROM tblUser", cn))
                    {
                        return Convert.ToInt32(cm.ExecuteScalar()) > 0;
                    }
                }
            }
            catch { return false; }
        }

        public static double CraticalItems()
        {
            try
            {
                using (SQLiteConnection cn = new SQLiteConnection(MyConnection()))
                {
                    cn.Open();
                    using (SQLiteCommand cm = new SQLiteCommand(
                        "SELECT COUNT(*) FROM TblProduct1 WHERE qty <= reorder", cn))
                    {
                        return Convert.ToDouble(cm.ExecuteScalar());
                    }
                }
            }
            catch { return 0; }
        }
    }
}