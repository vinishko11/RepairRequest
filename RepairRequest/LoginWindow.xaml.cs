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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace RepairRequest
{
    public partial class LoginWindow : Window
    {
        public int UserID { get; private set; }
        public LoginWindow()
        {
            InitializeComponent();
        }

        string connectionString = "Server=desktop-85kbji0\\sqlexpress;Database=PR;Integrated Security=true";

        private void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            string email = emailTextBox.Text;
            string password = passwordBox.Password;

            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                MessageBox.Show("Все поля должны быть заполнены.");
                return;
            }

            (bool isValid, string userStatus) = ValidateUser(email, password);

            if (isValid)
            {
                this.UserID = GetClientID(email);

                MessageBox.Show("Вход выполнен успешно!");

                if (userStatus == "Оператор")
                {
                    OperatorRequestWindow operatorRequestWindow = new OperatorRequestWindow(UserID, userStatus);
                    operatorRequestWindow.Show();

                    operatorRequestWindow.Left = this.Left;
                    operatorRequestWindow.Top = this.Top;
                    this.Close();
                }
                else if (userStatus == "Заказчик")
                {
                    ClientRequestsWindow clientRequestsWindow = new ClientRequestsWindow(UserID, userStatus);
                    clientRequestsWindow.Show();

                    clientRequestsWindow.Left = this.Left;
                    clientRequestsWindow.Top = this.Top;
                    this.Close();
                }
                else if(userStatus == "Мастер")
                {
                    MasterRequestsWindow masterRequestsWindow = new MasterRequestsWindow(UserID, userStatus);
                    masterRequestsWindow.Show();

                    masterRequestsWindow.Left = this.Left;
                    masterRequestsWindow.Top = this.Top;
                    this.Close();
                }
                else if (userStatus == "Менеджер")
                {
                    ManagerStatisticsWindow managerStatisticsWindow = new ManagerStatisticsWindow(UserID, userStatus);
                    managerStatisticsWindow.Show();

                    managerStatisticsWindow.Left = this.Left;
                    managerStatisticsWindow.Top = this.Top;
                    this.Close();
                }
            }
            else
            {
                MessageBox.Show("Неверный логин или пароль.");
            }
        }

        private (bool isValid, string userStatus) ValidateUser(string email, string password)
        {
            bool isValid = false;
            string status = "";

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                try
                {
                    connection.Open();
                    string query = "SELECT Password, Status FROM [User] WHERE Login = @Login";
                    SqlCommand command = new SqlCommand(query, connection);
                    command.Parameters.AddWithValue("@Login", email);

                    SqlDataReader reader = command.ExecuteReader();

                    if (reader.Read())
                    {
                        string storedPassword = reader["Password"].ToString();
                        status = reader["Status"].ToString();

                        if (password == storedPassword)
                        {
                            isValid = true;
                        }
                    }

                    reader.Close();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Ошибка при подключении к базе данных: " + ex.Message);
                }
            }

            return (isValid, status);
        }

        private int GetClientID(string email)
        {
            int clientID = 0;

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                try
                {
                    connection.Open();
                    string query = "SELECT userID FROM [User] WHERE Login = @Login";
                    SqlCommand command = new SqlCommand(query, connection);
                    command.Parameters.AddWithValue("@Login", email);

                    object result = command.ExecuteScalar();
                    if (result != null)
                    {
                        clientID = Convert.ToInt32(result);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Ошибка при получении ID клиента: " + ex.Message);
                }
            }

            return clientID;
        }

        private void RegisterWindowButton_Click(object sender, RoutedEventArgs e)
        {
            RegisterWindow registerWindow = new RegisterWindow();
            registerWindow.Show();

            registerWindow.Left = this.Left;
            registerWindow.Top = this.Top;
            this.Close();
        }

        private void ForgotPasswordButton_Click(object sender, RoutedEventArgs e)
        {
            ForgotWindow forgotWindow = new ForgotWindow();
            forgotWindow.Show();

            forgotWindow.Left = this.Left;
            forgotWindow.Top = this.Top;
            this.Close();
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
