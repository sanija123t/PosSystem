using System;
using System.Data.SQLite;
using System.Threading.Tasks;

namespace PosSystem
{
    public static class UserRepositoryV2
    {
        /// <summary>
        /// ELITE: Uses Transactions to ensure account creation or reactivation is atomic.
        /// </summary>
        public static async Task<bool> CreateAccountAsync(string username, string password, string role, string name)
        {
            if (string.IsNullOrWhiteSpace(username)) return false;

            try
            {
                using (var cn = new SQLiteConnection(DBConnection.MyConnection()))
                {
                    await cn.OpenAsync();
                    using (var transaction = cn.BeginTransaction())
                    {
                        // 1. Unified Check for existence and status
                        string checkQuery = "SELECT isdeleted FROM tblUser WHERE username = @user LIMIT 1";
                        object result;
                        using (var cmCheck = new SQLiteCommand(checkQuery, cn, transaction))
                        {
                            cmCheck.Parameters.AddWithValue("@user", username.Trim().ToLower());
                            result = await cmCheck.ExecuteScalarAsync();
                        }

                        string newSalt = Guid.NewGuid().ToString("N");
                        string newHash = DBConnection.GetHash(password, newSalt);

                        if (result != null) // User Exists in history
                        {
                            bool isDeleted = Convert.ToInt32(result) == 1;
                            if (!isDeleted) return false; // Account active, cannot recreate

                            // ELITE: Reactivation logic (Overwrites old credentials with new ones)
                            string reactivateQuery = @"UPDATE tblUser 
                                                     SET password=@pass, salt=@salt, role=@role, name=@name, isdeleted=0, isactive=1 
                                                     WHERE username=@user";

                            using (var cmUp = new SQLiteCommand(reactivateQuery, cn, transaction))
                            {
                                cmUp.Parameters.AddWithValue("@user", username.Trim().ToLower());
                                cmUp.Parameters.AddWithValue("@pass", newHash);
                                cmUp.Parameters.AddWithValue("@salt", newSalt);
                                cmUp.Parameters.AddWithValue("@role", role);
                                cmUp.Parameters.AddWithValue("@name", name);
                                await cmUp.ExecuteNonQueryAsync();
                            }
                        }
                        else // Brand New User
                        {
                            string insertQuery = @"INSERT INTO tblUser (username, password, salt, role, name, isactive, isdeleted)
                                                 VALUES (@username, @password, @salt, @role, @name, 1, 0)";

                            using (var cmIns = new SQLiteCommand(insertQuery, cn, transaction))
                            {
                                cmIns.Parameters.AddWithValue("@username", username.Trim().ToLower());
                                cmIns.Parameters.AddWithValue("@password", newHash);
                                cmIns.Parameters.AddWithValue("@salt", newSalt);
                                cmIns.Parameters.AddWithValue("@role", role);
                                cmIns.Parameters.AddWithValue("@name", name);
                                await cmIns.ExecuteNonQueryAsync();
                            }
                        }

                        transaction.Commit();
                        return true;
                    }
                }
            }
            catch (Exception) { throw; }
        }

        /// <summary>
        /// RESTORED: Required by frmUserAccount for the status checkbox logic.
        /// </summary>
        public static async Task<bool> GetUserStatusAsync(string username)
        {
            using (var cn = new SQLiteConnection(DBConnection.MyConnection()))
            {
                await cn.OpenAsync();
                using (var cm = new SQLiteCommand("SELECT isactive FROM tblUser WHERE username=@username AND isdeleted=0", cn))
                {
                    cm.Parameters.AddWithValue("@username", username.Trim().ToLower());
                    var result = await cm.ExecuteScalarAsync();
                    return result != null && Convert.ToInt32(result) == 1;
                }
            }
        }

        /// <summary>
        /// ELITE: Force-generates a NEW salt on password change (Re-salting).
        /// </summary>
        public static async Task<bool> ChangePasswordAsync(string username, string oldPassword, string newPassword)
        {
            try
            {
                using (var cn = new SQLiteConnection(DBConnection.MyConnection()))
                {
                    await cn.OpenAsync();
                    using (var transaction = cn.BeginTransaction())
                    {
                        string currentHash = "";
                        string currentSalt = "";

                        using (var selectCmd = new SQLiteCommand("SELECT password, salt FROM tblUser WHERE username=@u AND isdeleted=0", cn, transaction))
                        {
                            selectCmd.Parameters.AddWithValue("@u", username.ToLower());
                            using (var reader = await selectCmd.ExecuteReaderAsync())
                            {
                                if (!await reader.ReadAsync()) return false;
                                currentHash = reader["password"].ToString();
                                currentSalt = reader["salt"].ToString();
                            }
                        }

                        // Cryptographic Verification
                        if (!currentHash.Equals(DBConnection.GetHash(oldPassword, currentSalt))) return false;

                        // Re-salting
                        string newSalt = Guid.NewGuid().ToString("N");
                        string newHash = DBConnection.GetHash(newPassword, newSalt);

                        using (var updateCmd = new SQLiteCommand("UPDATE tblUser SET password=@pass, salt=@s WHERE username=@u", cn, transaction))
                        {
                            updateCmd.Parameters.AddWithValue("@pass", newHash);
                            updateCmd.Parameters.AddWithValue("@s", newSalt);
                            updateCmd.Parameters.AddWithValue("@u", username.ToLower());
                            await updateCmd.ExecuteNonQueryAsync();
                        }

                        transaction.Commit();
                        return true;
                    }
                }
            }
            catch (Exception) { throw; }
        }

        public static async Task<bool> UpdateStatusAsync(string username, bool isActive)
        {
            using (var cn = new SQLiteConnection(DBConnection.MyConnection()))
            {
                await cn.OpenAsync();
                using (var cm = new SQLiteCommand("UPDATE tblUser SET isactive=@active WHERE username=@u AND isdeleted=0", cn))
                {
                    cm.Parameters.AddWithValue("@active", isActive ? 1 : 0);
                    cm.Parameters.AddWithValue("@u", username.Trim().ToLower());
                    return await cm.ExecuteNonQueryAsync() > 0;
                }
            }
        }

        public static async Task<bool> DeleteUserAsync(string username)
        {
            using (var cn = new SQLiteConnection(DBConnection.MyConnection()))
            {
                await cn.OpenAsync();
                // Soft Delete
                using (var cm = new SQLiteCommand("UPDATE tblUser SET isdeleted=1, isactive=0 WHERE username=@u", cn))
                {
                    cm.Parameters.AddWithValue("@u", username.Trim().ToLower());
                    return await cm.ExecuteNonQueryAsync() > 0;
                }
            }
        }
    }
}