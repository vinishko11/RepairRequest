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
    public partial class ProfileWindow : Window
    {
        private int clientID;
        private string userStatus;
        private string connectionString = "Server=desktop-85kbji0\\sqlexpress;Database=PR;Integrated Security=true";

        public ProfileWindow(int clientID, string userStatus)
        {
            InitializeComponent();
            this.clientID = clientID;
            this.userStatus = userStatus;

            DisplayUserData();
        }

        private void DisplayUserData()
        {
            string query = "SELECT fullName, phoneNumber, Login FROM [User] WHERE userID = @UserID";

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@UserID", clientID);

                    try
                    {
                        connection.Open();
                        SqlDataReader reader = command.ExecuteReader();
                        if (reader.Read())
                        {
                            fullNameTextBox.Text = reader["fullName"].ToString();
                            phoneNumberTextBox.Text = reader["phoneNumber"].ToString();
                            emailTextBox.Text = reader["Login"].ToString();
                        }
                        reader.Close();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Ошибка при получении данных пользователя: " + ex.Message);
                    }
                }
            }
        }

        private void SaveDataButton_Click(object sender, RoutedEventArgs e)
        {
            string fullName = fullNameTextBox.Text;
            string phoneNumber = phoneNumberTextBox.Text;
            string email = emailTextBox.Text;
            string password = passwordBox.Password;
            string confirmPassword = confirmPasswordBox.Password;

            if (string.IsNullOrWhiteSpace(fullName) || string.IsNullOrWhiteSpace(phoneNumber) ||
                string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(confirmPassword))
            {
                MessageBox.Show("Все поля должны быть заполнены.");
                return;
            }

            if (password != confirmPassword)
            {
                MessageBox.Show("Пароли не совпадают.");
                return;
            }

            string updateQuery = "UPDATE [User] SET fullName = @FullName, phoneNumber = @PhoneNumber, Login = @Email, Password = @Password WHERE userID = @UserID";

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                using (SqlCommand command = new SqlCommand(updateQuery, connection))
                {
                    command.Parameters.AddWithValue("@FullName", fullName);
                    command.Parameters.AddWithValue("@PhoneNumber", phoneNumber);
                    command.Parameters.AddWithValue("@Email", email);
                    command.Parameters.AddWithValue("@Password", password);
                    command.Parameters.AddWithValue("@UserID", clientID);

                    try
                    {
                        connection.Open();
                        int rowsAffected = command.ExecuteNonQuery();
                        if (rowsAffected > 0)
                        {
                            MessageBox.Show("Данные успешно сохранены.");

                            OpenRequestWindow();
                        }
                        else
                        {
                            MessageBox.Show("Не удалось сохранить данные.");
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Ошибка при сохранении данных: " + ex.Message);
                    }
                }
            }
        }

        private void OpenRequestWindow()
        {
            if (userStatus == "Оператор")
            {
                OperatorRequestWindow operatorRequestWindow = new OperatorRequestWindow(clientID, userStatus);
                operatorRequestWindow.Show();

                operatorRequestWindow.Left = this.Left;
                operatorRequestWindow.Top = this.Top;
                this.Close();
            }
            else if (userStatus == "Заказчик")
            {
                ClientRequestsWindow clientRequestsWindow = new ClientRequestsWindow(clientID, userStatus);
                clientRequestsWindow.Show();

                clientRequestsWindow.Left = this.Left;
                clientRequestsWindow.Top = this.Top;
                this.Close();
            }
            else if (userStatus == "Мастер")
            {
                MasterRequestsWindow masterRequestsWindow = new MasterRequestsWindow(clientID, userStatus);
                masterRequestsWindow.Show();

                masterRequestsWindow.Left = this.Left;
                masterRequestsWindow.Top = this.Top;
                this.Close();
            }
            else if (userStatus == "Менеджер")
            {
                ManagerStatisticsWindow managerStatisticsWindow = new ManagerStatisticsWindow(clientID, userStatus);
                managerStatisticsWindow.Show();

                managerStatisticsWindow.Left = this.Left;
                managerStatisticsWindow.Top = this.Top;
                this.Close();
            }
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            OpenRequestWindow();
        }

        private void ExitButton_Click(object sender, RoutedEventArgs e)
        {
            LoginWindow loginWindow = new LoginWindow();
            loginWindow.Show();

            loginWindow.Left = this.Left;
            loginWindow.Top = this.Top;
            this.Close();
        }
    }
}
