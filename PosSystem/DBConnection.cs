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

                        new SQLiteCommand("PRAGMA foreign_keys = ON;", cn).ExecuteNonQuery();
                        new SQLiteCommand("PRAGMA synchronous = NORMAL;", cn).ExecuteNonQuery();
                        new SQLiteCommand("PRAGMA journal_mode = WAL;", cn).ExecuteNonQuery();
                        new SQLiteCommand("PRAGMA temp_store = MEMORY;", cn).ExecuteNonQuery();
                        new SQLiteCommand("PRAGMA cache_size = -20000;", cn).ExecuteNonQuery(); // 20MB cache

                        string script = @"

-- CORE TABLES
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
    cost_price REAL DEFAULT 0,
    tax_rate REAL DEFAULT 0,
    sid INTEGER DEFAULT 0,
    qty INTEGER DEFAULT 0,
    reorder INTEGER DEFAULT 0,
    isactive INTEGER DEFAULT 1,
    FOREIGN KEY (bid) REFERENCES BrandTbl(id),
    FOREIGN KEY (cid) REFERENCES TblCategory(id)
);

CREATE TABLE IF NOT EXISTS tblTransaction (
    transno TEXT PRIMARY KEY,
    sdate TEXT,
    subtotal REAL DEFAULT 0,
    discount REAL DEFAULT 0,
    vat REAL DEFAULT 0,
    total REAL DEFAULT 0,
    payment_type TEXT,
    cash_tendered REAL DEFAULT 0,
    cash_change REAL DEFAULT 0,
    user_id TEXT,
    status TEXT DEFAULT 'Sold'
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
    FOREIGN KEY (pcode) REFERENCES TblProduct1(pcode),
    FOREIGN KEY (transno) REFERENCES tblTransaction(transno) ON DELETE CASCADE
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
    transno TEXT, pcode TEXT, price REAL, qty INTEGER, total REAL,
    sdate TEXT, voidby TEXT, cancelledby TEXT, reason TEXT, action TEXT
);

-- ========================================
-- ENTERPRISE TABLES (NEW)
-- ========================================

CREATE TABLE IF NOT EXISTS tblAudit(
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    user TEXT,
    action TEXT,
    details TEXT,
    sdate TEXT
);

CREATE TABLE IF NOT EXISTS tblProfitLog(
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    transno TEXT,
    pcode TEXT,
    cost REAL,
    sell REAL,
    qty INTEGER,
    profit REAL,
    sdate TEXT
);

-- ========================================
-- INDEXES FOR 100K+ PRODUCTS PERFORMANCE
-- ========================================

CREATE INDEX IF NOT EXISTS idx_product_barcode ON TblProduct1(barcode);
CREATE INDEX IF NOT EXISTS idx_product_active ON TblProduct1(isactive);
CREATE INDEX IF NOT EXISTS idx_cart_transno ON tblCart1(transno);
CREATE INDEX IF NOT EXISTS idx_cart_pcode ON tblCart1(pcode);
CREATE INDEX IF NOT EXISTS idx_cart_status ON tblCart1(status);
CREATE INDEX IF NOT EXISTS idx_trans_date ON tblTransaction(sdate);
CREATE INDEX IF NOT EXISTS idx_stockin_pcode ON tblStockIn(pcode);

-- ========================================
-- TRIGGERS (AUTO PROFIT + AUDIT LOGGING)
-- ========================================

CREATE TRIGGER IF NOT EXISTS trg_after_sale
AFTER INSERT ON tblCart1
WHEN NEW.status='Sold'
BEGIN
    INSERT INTO tblProfitLog(transno,pcode,cost,sell,qty,profit,sdate)
    SELECT
        NEW.transno,
        NEW.pcode,
        p.cost_price,
        NEW.price,
        NEW.qty,
        (NEW.price - p.cost_price)*NEW.qty,
        NEW.sdate
    FROM TblProduct1 p WHERE p.pcode = NEW.pcode;
END;

CREATE TRIGGER IF NOT EXISTS trg_user_update
AFTER UPDATE ON tblUser
BEGIN
    INSERT INTO tblAudit(user,action,details,sdate)
    VALUES(NEW.username,'USER UPDATED','User record modified',datetime('now','localtime'));
END;

CREATE TRIGGER IF NOT EXISTS trg_product_update
AFTER UPDATE ON TblProduct1
BEGIN
    INSERT INTO tblAudit(user,action,details,sdate)
    VALUES('SYSTEM','PRODUCT UPDATED','Product '||NEW.pcode||' modified',datetime('now','localtime'));
END;

-- ========================================
-- VIEWS
-- ========================================

DROP VIEW IF EXISTS vwInventory;
CREATE VIEW vwInventory AS
SELECT p.pcode, p.barcode, p.pdesc, b.brand, c.category, p.price, p.cost_price, p.qty, p.reorder, p.isactive
FROM TblProduct1 p
LEFT JOIN BrandTbl b ON b.id = p.bid
LEFT JOIN TblCategory c ON c.id = p.cid;

DROP VIEW IF EXISTS vwCriticalItems;
CREATE VIEW vwCriticalItems AS
SELECT * FROM vwInventory WHERE qty <= reorder AND isactive = 1;

DROP VIEW IF EXISTS vwSoldItems;
CREATE VIEW vwSoldItems AS
SELECT c.id, c.transno, c.pcode, p.pdesc, c.price, c.qty, c.disc, c.total, c.sdate, c.status, c.user
FROM tblCart1 c
INNER JOIN TblProduct1 p ON c.pcode = p.pcode;

DROP VIEW IF EXISTS vwProfitDaily;
CREATE VIEW vwProfitDaily AS
SELECT DATE(sdate) AS day, SUM(profit) AS total_profit
FROM tblProfitLog
GROUP BY DATE(sdate);

DROP VIEW IF EXISTS vwProfitProducts;
CREATE VIEW vwProfitProducts AS
SELECT pcode, SUM(qty) sold_qty, SUM(profit) profit
FROM tblProfitLog
GROUP BY pcode;

";

                        using (var cm = new SQLiteCommand(script, cn))
                            cm.ExecuteNonQuery();

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