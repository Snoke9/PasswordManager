using System.Data.SQLite;
using System.Windows;
using PasswordManager.Models;


namespace PasswordManager.Services
{
    public class Database
    {
        private string connectionString = "Data Source=./passwords.db;Version=3;";

        public Database()
        {
            ExecuteNonQuery(@"CREATE TABLE IF NOT EXISTS Accounts (
                                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                                ServiceName TEXT NOT NULL,
                                Username TEXT NOT NULL,
                                EncryptedPassword TEXT NOT NULL,
                                Additional TEXT)");
        }

        private void ExecuteNonQuery(string query, SQLiteParameter[] parameters = null)
        {
            using (var connection = new SQLiteConnection(connectionString))
            {
                connection.Open();
                using (var command = new SQLiteCommand(query, connection))
                {
                    if (parameters != null)
                        command.Parameters.AddRange(parameters);
                    command.ExecuteNonQuery();
                }
            }
        }

        public void InsertAccount(Account account)
        {
            string query = "INSERT INTO Accounts (ServiceName, Username, EncryptedPassword, Additional) VALUES (@ServiceName, @Username, @EncryptedPassword, @Additional)";
            SQLiteParameter[] parameters =
            {
                new SQLiteParameter("@ServiceName", account.ServiceName),
                new SQLiteParameter("@Username", account.Username),
                new SQLiteParameter("@EncryptedPassword", account.Password),
                new SQLiteParameter("@Additional", account.Additional)
            };
            ExecuteNonQuery(query, parameters);
        }

        public List<Account> GetAllAccounts()
        {
            List<Account> accounts = new List<Account>();
            using (var connection = new SQLiteConnection(connectionString))
            {
                connection.Open();
                string selectQuery = "SELECT Id, ServiceName, Username, EncryptedPassword, Additional FROM Accounts";
                using (var command = new SQLiteCommand(selectQuery, connection))
                {
                    using (SQLiteDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            Account account = new Account
                            {
                                Id = Convert.ToInt32(reader["Id"]),
                                ServiceName = reader["ServiceName"].ToString(),
                                Username = reader["Username"].ToString(),
                                Password = Encryption.Decrypt(reader["EncryptedPassword"].ToString()),
                                Additional = reader["Additional"].ToString()
                            };
                            accounts.Add(account);
                        }
                    }
                }
            }
            return accounts;
        }

        public void DeleteAccount(Account account)
        {
            try
            {
                string query = "DELETE FROM Accounts WHERE Id = @Id";
                SQLiteParameter[] parameters =
                {
                new SQLiteParameter("@Id", account.Id)
                };
                ExecuteNonQuery(query, parameters);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}");
            }
        }
    }
}
