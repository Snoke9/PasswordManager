using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using PasswordManager.Services;
using PasswordManager.Models;


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
            additionalTextBox.KeyDown += textBox_KeyDown;
        }

        private void saveButton_Click(object sender, RoutedEventArgs e)
        {
            string serviceName = serviceNameTextBox.Text;
            string username = usernameTextBox.Text;
            string password = passwordTextBox.Text;
            string additional = additionalTextBox.Text;

            if (string.IsNullOrWhiteSpace(serviceName) || string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                MessageBox.Show(Application.Current.MainWindow,"Заполните все обязательные поля", "Внимание", MessageBoxButton.OK, MessageBoxImage.Information);
                serviceNameTextBox.Focus();
                return;
            }

            string encryptedPassword = Encryption.Encrypt(password);

            Account newAccount = new Account
            {
                ServiceName = serviceName,
                Username = username,
                Password = encryptedPassword,
                Additional = additional
            };

            db.InsertAccount(newAccount);

            serviceNameTextBox.Clear();
            usernameTextBox.Clear();
            passwordTextBox.Clear();
            additionalTextBox.Clear();
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
                MessageBox.Show("Выберите запись для удаления", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
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
                        if (dataGridCell.Column.Header.ToString() == "Логин")
                            Clipboard.SetText(account.Username);
                        else if (dataGridCell.Column.Header.ToString() == "Пароль")
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
                    additionalTextBox.Focus();
                else if (sender == additionalTextBox)
                    saveButton_Click(null!, null!);
            }
        }
    }
}
