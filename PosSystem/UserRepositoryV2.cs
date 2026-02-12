using System;
using System.Data.SQLite;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace PosSystem
{
    public static class UserRepositoryV2
    {
        /// <summary>
        /// Creates a new user account with a unique salt and hashed password.
        /// </summary>
        public static async Task<bool> CreateAccountAsync(string username, string password, string role, string name)
        {
            try
            {
                string salt = Guid.NewGuid().ToString("N");
                string hashedPassword = DBConnection.GetHash(password, salt);

                using var cn = new SQLiteConnection(DBConnection.MyConnection());
                await cn.OpenAsync();

                string query = @"INSERT INTO tblUser (username, password, salt, role, name, isactive, isdeleted)
                                 VALUES (@username, @password, @salt, @role, @name, 1, 0)";
                using var cm = new SQLiteCommand(query, cn);
                cm.Parameters.AddWithValue("@username", username);
                cm.Parameters.AddWithValue("@password", hashedPassword);
                cm.Parameters.AddWithValue("@salt", salt);
                cm.Parameters.AddWithValue("@role", role);
                cm.Parameters.AddWithValue("@name", name);

                int rows = await cm.ExecuteNonQueryAsync();
                return rows > 0;
            }
            catch (SQLiteException ex) when (ex.ResultCode == SQLiteErrorCode.Constraint)
            {
                // Username already exists
                return false;
            }
            catch (Exception)
            {
                throw; // Let the calling code handle general exceptions
            }
        }

        /// <summary>
        /// Changes the password for an existing user. Returns true if successful.
        /// </summary>
        public static async Task<bool> ChangePasswordAsync(string username, string oldPassword, string newPassword)
        {
            try
            {
                using var cn = new SQLiteConnection(DBConnection.MyConnection());
                await cn.OpenAsync();

                // 1. Get current hash and salt
                string selectQuery = "SELECT password, salt FROM tblUser WHERE username=@username AND isdeleted=0";
                using var selectCmd = new SQLiteCommand(selectQuery, cn);
                selectCmd.Parameters.AddWithValue("@username", username);

                using var reader = await selectCmd.ExecuteReaderAsync();
                if (!reader.Read()) return false;

                string currentHash = reader["password"].ToString();
                string salt = reader["salt"].ToString();
                reader.Close();

                // 2. Verify old password
                string oldHash = DBConnection.GetHash(oldPassword, salt);
                if (!currentHash.Equals(oldHash)) return false;

                // 3. Generate new salt and hash
                string newSalt = Guid.NewGuid().ToString("N");
                string newHash = DBConnection.GetHash(newPassword, newSalt);

                // 4. Update in DB
                string updateQuery = "UPDATE tblUser SET password=@pass, salt=@salt WHERE username=@username AND isdeleted=0";
                using var updateCmd = new SQLiteCommand(updateQuery, cn);
                updateCmd.Parameters.AddWithValue("@pass", newHash);
                updateCmd.Parameters.AddWithValue("@salt", newSalt);
                updateCmd.Parameters.AddWithValue("@username", username);

                int rows = await updateCmd.ExecuteNonQueryAsync();
                return rows > 0;
            }
            catch (Exception)
            {
                throw;
            }
        }


        public static async Task<bool> GetUserStatusAsync(string username)
        {
            using var cn = new SQLiteConnection(DBConnection.MyConnection());
            await cn.OpenAsync();
            using var cm = new SQLiteCommand("SELECT isactive FROM tblUser WHERE username=@username AND isdeleted=0", cn);
            cm.Parameters.AddWithValue("@username", username);
            var result = await cm.ExecuteScalarAsync();
            return result != null && Convert.ToBoolean(result);
        }

        /// <summary>
        /// Activate or deactivate a user account.
        /// </summary>
        public static async Task<bool> UpdateStatusAsync(string username, bool isActive)
        {
            try
            {
                using var cn = new SQLiteConnection(DBConnection.MyConnection());
                await cn.OpenAsync();

                string query = "UPDATE tblUser SET isactive=@active WHERE username=@username AND isdeleted=0";
                using var cm = new SQLiteCommand(query, cn);
                cm.Parameters.AddWithValue("@active", isActive ? 1 : 0);
                cm.Parameters.AddWithValue("@username", username);

                int rows = await cm.ExecuteNonQueryAsync();
                return rows > 0;
            }
            catch (Exception)
            {
                throw;
            }
        }
        public static async Task<bool> DeleteUserAsync(string username)
        {
            try
            {
                using (var cn = new SQLiteConnection(DBConnection.MyConnection()))
                {
                    await cn.OpenAsync();
                    // We use a Soft Delete (isdeleted = 1) to keep data integrity
                    string query = "UPDATE tblUser SET isdeleted = 1, isactive = 0 WHERE username = @user";

                    using (var cm = new SQLiteCommand(query, cn))
                    {
                        cm.Parameters.AddWithValue("@user", username.ToLower());
                        int rowsAffected = await cm.ExecuteNonQueryAsync();

                        // Returns true if a user was actually found and updated
                        return rowsAffected > 0;
                    }
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        /// <summary>
        /// Checks if a username exists (optional helper).
        /// </summary>
        public static async Task<bool> UserExistsAsync(string username)
        {
            try
            {
                using var cn = new SQLiteConnection(DBConnection.MyConnection());
                await cn.OpenAsync();

                string query = "SELECT COUNT(*) FROM tblUser WHERE username=@username AND isdeleted=0";
                using var cm = new SQLiteCommand(query, cn);
                cm.Parameters.AddWithValue("@username", username);

                int count = Convert.ToInt32(await cm.ExecuteScalarAsync());
                return count > 0;
            }
            catch
            {
                return false;
            }
        }
    }
}