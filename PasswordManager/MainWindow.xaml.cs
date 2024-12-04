using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Data.SQLite;
using System.Security.Cryptography;
using System.Windows.Input;
using System.Windows.Media;
using System.Configuration;


namespace PasswordManager
{
    public partial class MainWindow : Window
    {
        private Database db = new Database();

        public MainWindow()
        {
            InitializeComponent();

            serviceNameTextBox.KeyDown += textBox_KeyDown;
            usernameTextBox.KeyDown += textBox_KeyDown;
            passwordTextBox.KeyDown += textBox_KeyDown;

        }

        private void saveButton_Click(object sender, RoutedEventArgs e)
        {

            string serviceName = serviceNameTextBox.Text;
            string username = usernameTextBox.Text;
            string password = passwordTextBox.Text;

            if (string.IsNullOrWhiteSpace(serviceName) || string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                MessageBox.Show("Заполните все поля.", "Внимание", MessageBoxButton.OK, MessageBoxImage.Information);
                serviceNameTextBox.Focus();
                return;
            }

            string encryptedPassword = Encryption.Encrypt(password);

            Account newAccount = new Account
            {
                ServiceName = serviceName,
                Username = username,
                Password = encryptedPassword
            };

            db.InsertAccount(newAccount);

            serviceNameTextBox.Clear();
            usernameTextBox.Clear();
            passwordTextBox.Clear();
            serviceNameTextBox.Focus();

            MessageBox.Show("Пароль сохранен", "Уведомление", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void showPasswordsButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                List<Account> accounts = db.GetAllAccounts();
                dataGridAccounts.ItemsSource = accounts;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Произошла ошибка: {ex.Message}");
            }
        }

        private void searchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            string searchText = searchTextBox.Text.ToLower();
            List<Account> allAccounts = db.GetAllAccounts();
            var filteredAccounts = allAccounts.Where(a => a.ServiceName.ToLower().Contains(searchText)).ToList();

            dataGridAccounts.ItemsSource = filteredAccounts;

        }

        private void deleteButton_Click(object sender, RoutedEventArgs e)
        {
            if (dataGridAccounts.SelectedItem is Account selectedAccount)
            {
                db.DeleteAccount(selectedAccount);

                List<Account> accounts = db.GetAllAccounts();
                dataGridAccounts.ItemsSource = accounts;

                MessageBox.Show("Запись удалена", "Уведомление", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                MessageBox.Show("Выберите запись для удаления", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void dataGridAccounts_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var cell = e.OriginalSource as FrameworkElement;
            if (cell != null)
            {
                var dataGridCell = FindParent<DataGridCell>(cell);
                if (dataGridCell != null)
                {
                    var account = dataGridCell.DataContext as Account;

                    if (account != null)
                    {
                        if (dataGridCell.Column.Header.ToString() == "Username")
                            Clipboard.SetText(account.Username);
                        else if (dataGridCell.Column.Header.ToString() == "Password")
                            Clipboard.SetText(account.Password);
                    }
                }
            }
        }

        private T FindParent<T>(DependencyObject child) where T : DependencyObject
        {
            DependencyObject parentObject = VisualTreeHelper.GetParent(child);

            if (parentObject == null) 
                return null;
            T parent = parentObject as T;
            return parent ?? FindParent<T>(parentObject);
        }

        private void serviceNameTextBox_KeyDown(object sender, KeyEventArgs e)
        {

        }

        private void textBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                e.Handled = true;
                if (sender == serviceNameTextBox)
                    usernameTextBox.Focus();
                else if (sender == usernameTextBox)
                    passwordTextBox.Focus();
                else if (sender == passwordTextBox)
                    saveButton_Click(null!, null!);
            }
        }
    }

    public class Account
    {
        public int Id { get; set; }
        public string ServiceName { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
    }

    public class Database
    {
        private string connectionString = "Data Source=./passwords.db;Version=3;";

        public Database()
        {
            ExecuteNonQuery(@"CREATE TABLE IF NOT EXISTS Accounts (
                                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                                ServiceName TEXT NOT NULL,
                                Username TEXT NOT NULL,
                                EncryptedPassword TEXT NOT NULL)");
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
            string query = "INSERT INTO Accounts (ServiceName, Username, EncryptedPassword) VALUES (@ServiceName, @Username, @EncryptedPassword)";
            SQLiteParameter[] parameters =
            {
                new SQLiteParameter("@ServiceName", account.ServiceName),
                new SQLiteParameter("@Username", account.Username),
                new SQLiteParameter("@EncryptedPassword", account.Password)
            };
            ExecuteNonQuery(query, parameters);
        }

        public List<Account> GetAllAccounts()
        {
            List<Account> accounts = new List<Account>();
            using (var connection = new SQLiteConnection(connectionString))
            {
                connection.Open();
                string selectQuery = "SELECT Id, ServiceName, Username, EncryptedPassword FROM Accounts";
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
                                Password = Encryption.Decrypt(reader["EncryptedPassword"].ToString())
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


    public static class Encryption
    {
        private static readonly string key = ConfigurationManager.AppSettings["KEY"]!.PadRight(32);

        public static string Encrypt(string plainText)
        {
            byte[] plainBytes = Encoding.UTF8.GetBytes(plainText);
            using (Aes aes = Aes.Create())
            {
                aes.Key = Encoding.UTF8.GetBytes(key);
                aes.GenerateIV(); 

                using (ICryptoTransform encryptor = aes.CreateEncryptor(aes.Key, aes.IV))
                {
                    byte[] encrypted = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);

                    byte[] combinedData = new byte[aes.IV.Length + encrypted.Length];
                    Array.Copy(aes.IV, 0, combinedData, 0, aes.IV.Length);
                    Array.Copy(encrypted, 0, combinedData, aes.IV.Length, encrypted.Length);

                    return Convert.ToBase64String(combinedData);
                }
            }
        }

        public static string Decrypt(string encryptedText)
        {
            byte[] combinedData = Convert.FromBase64String(encryptedText);
            using (Aes aes = Aes.Create())
            {
                aes.Key = Encoding.UTF8.GetBytes(key);

                byte[] iv = new byte[16];
                byte[] cipherText = new byte[combinedData.Length - iv.Length];

                Array.Copy(combinedData, 0, iv, 0, iv.Length);
                Array.Copy(combinedData, iv.Length, cipherText, 0, cipherText.Length);

                aes.IV = iv;

                using (ICryptoTransform decryptor = aes.CreateDecryptor(aes.Key, aes.IV))
                {
                    try
                    {
                        byte[] decrypted = decryptor.TransformFinalBlock(cipherText, 0, cipherText.Length);
                        return Encoding.UTF8.GetString(decrypted);
                    }
                    catch (CryptographicException ex)
                    {
                        MessageBox.Show($"Произошла ошибка дешифровки: {ex.Message}");
                        return null;
                    }
                }
            }
        }
    }
}
