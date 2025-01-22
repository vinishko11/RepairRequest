using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace RepairRequest
{
    public partial class ForgotWindow : Window
    {
        public ForgotWindow()
        {
            InitializeComponent();
        }

        private void EmailTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                string email = emailTextBox.Text;

                if (ValidateEmail(email))
                {
                    emailBorder.BorderBrush = new SolidColorBrush(Colors.Green);
                    passwordBox.IsEnabled = true;
                    confirmPasswordBox.IsEnabled = true;
                }
                else
                {
                    emailBorder.BorderBrush = new SolidColorBrush(Colors.Red);
                    passwordBox.IsEnabled = false;
                    confirmPasswordBox.IsEnabled = false;
                }
            }
        }

        private bool ValidateEmail(string email)
        {
            bool isValid = false;
            string connectionString = "Server=desktop-85kbji0\\sqlexpress;Database=PR;Integrated Security=true";

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                string query = "SELECT COUNT(1) FROM [User] WHERE Login=@Email";
                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Email", email);
                    isValid = (int)command.ExecuteScalar() == 1;
                }
            }

            return isValid;
        }

        private void ResetPasswordButton_Click(object sender, RoutedEventArgs e)
        {
            string email = emailTextBox.Text;
            string password = passwordBox.Password;
            string confirmPassword = confirmPasswordBox.Password;

            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(confirmPassword))
            {
                MessageBox.Show("Все поля должны быть заполнены.");
                return;
            }

            if (password != confirmPassword)
            {
                MessageBox.Show("Пароли не совпадают.");
                return;
            }

            if (ValidateEmail(email))
            {
                UpdatePassword(email, password);
                MessageBox.Show("Пароль восстановлен успешно!");

                LoginWindow loginWindow = new LoginWindow();
                loginWindow.Show();

                loginWindow.Left = this.Left;
                loginWindow.Top = this.Top;
                this.Close();
            }
            else
            {
                MessageBox.Show("Неверный логин.");
            }
        }

        private void UpdatePassword(string email, string newPassword)
        {
            string connectionString = "Server=desktop-85kbji0\\sqlexpress;Database=PR;Integrated Security=true";

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                string query = "UPDATE [User] SET Password=@Password WHERE Login=@Email";
                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Email", email);
                    command.Parameters.AddWithValue("@Password", newPassword);
                    command.ExecuteNonQuery();
                }
            }
        }

        private void LoginWindowButton_Click(object sender, RoutedEventArgs e)
        {
            LoginWindow loginWindow = new LoginWindow();
            loginWindow.Show();

            loginWindow.Left = this.Left;
            loginWindow.Top = this.Top;
            this.Close();
        }
    }
}