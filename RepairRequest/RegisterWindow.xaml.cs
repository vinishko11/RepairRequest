using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text.RegularExpressions;
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
    public partial class RegisterWindow : Window
    {
        public RegisterWindow()
        {
            InitializeComponent();
        }

        string connectionString = "Server=desktop-85kbji0\\sqlexpress;Database=PR;Integrated Security=true";

        private void LoginWindowButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                LoginWindow loginWindow = new LoginWindow();
                loginWindow.Show();

                loginWindow.Left = this.Left;
                loginWindow.Top = this.Top;
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при открытии окна входа: " + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void RegisterWindowButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                RegisterWindow registerWindow = new RegisterWindow();
                registerWindow.Show();

                registerWindow.Left = this.Left;
                registerWindow.Top = this.Top;
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при открытии окна регистрации: " + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void RegistrationButton_Click(object sender, RoutedEventArgs e)
        {
            string fullName = fullNameTextBox.Text.Trim();
            string phoneNumber = phoneNumberTextBox.Text.Trim();
            string email = emailTextBox.Text.Trim();
            string password = passwordBox.Password;
            string confirmPassword = confirmPasswordBox.Password;

            if (!Regex.IsMatch(fullName, @"^[А-Яа-яЁё\s]+$"))
            {
                MessageBox.Show("Поле ФИО может содержать только кириллицу и пробелы.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!Regex.IsMatch(phoneNumber, @"^\d{11}$"))
            {
                MessageBox.Show("Номер телефона должен содержать ровно 11 цифр.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
            {
                MessageBox.Show("Введите корректный адрес электронной почты.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!Regex.IsMatch(password, @"^(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]{8,}$"))
            {
                MessageBox.Show("Пароль должен содержать минимум 8 символов, одну заглавную букву, одну цифру и один специальный символ.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (password != confirmPassword)
            {
                MessageBox.Show("Пароли не совпадают.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                if (string.IsNullOrEmpty(fullName))
                {
                    throw new ArgumentException("Поле ФИО не может быть пустым.");
                }

                if (string.IsNullOrEmpty(phoneNumber))
                {
                    throw new ArgumentException("Номер телефона не может бытоь пустым.");
                }

                if (string.IsNullOrEmpty(email))
                {
                    throw new ArgumentException("Электронная почта не может быть пустой.");
                }

                if (string.IsNullOrEmpty(password))
                {
                    throw new ArgumentException("Пароль не может быть пустым.");
                }

                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    string query = "INSERT INTO [User] (fullName, phoneNumber, Login, Password, Status) VALUES (@FullName, @PhoneNumber, @Login, @Password, 'Заказчик')";

                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@FullName", fullName);
                        command.Parameters.AddWithValue("@PhoneNumber", phoneNumber.StartsWith("7") ? phoneNumber : "7" + phoneNumber.Substring(1));
                        command.Parameters.AddWithValue("@Login", email);
                        command.Parameters.AddWithValue("@Password", password);

                        int result = command.ExecuteNonQuery();

                        if (result > 0)
                        {
                            MessageBox.Show("Регистрация прошла успешно.", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);

                            LoginWindow loginWindow = new LoginWindow();
                            loginWindow.Show();

                            loginWindow.Left = this.Left;
                            loginWindow.Top = this.Top;
                            this.Close();
                        }
                        else
                        {
                            MessageBox.Show("Ошибка при регистрации.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                }
            }
            catch (SqlException sqlEx)
            {
                MessageBox.Show("Ошибка базы данных: " + sqlEx.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Неизвестная ошибка: " + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
