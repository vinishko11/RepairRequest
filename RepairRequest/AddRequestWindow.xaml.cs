using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
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
using System.Windows.Threading;

namespace RepairRequest
{
    public partial class AddRequestWindow : Window
    {
        private int clientID;
        private string userStatus;
        private string connectionString = "Server=desktop-85kbji0\\sqlexpress;Database=PR;Integrated Security=true";
        private int unreadNotificationsCount = 0;

        public AddRequestWindow(int clientID, string userStatus)
        {
            InitializeComponent();
            this.clientID = clientID;
            this.userStatus = userStatus;
            UpdateNotificationBadge();
        }

        private void createRequestButton_Click(object sender, RoutedEventArgs e)
        {
            string techType = techTypeTextBox.Text;
            string techModel = techModelTextBox.Text;
            string problemDescryption = problemDescriptionTextBox.Text;

            string insertQuery = @"
                INSERT INTO Request (startDate, homeTechType, homeTechModel, problemDescryption, requestStatus, clientID)
                VALUES (@StartDate, @HomeTechType, @HomeTechModel, @ProblemDescryption, @RequestStatus, @ClientID);
                SELECT SCOPE_IDENTITY();";

            try
            {
                if (string.IsNullOrEmpty(techType))
                {
                    throw new ArgumentException("Тип техники не может быть пустым.");
                }

                if (string.IsNullOrEmpty(techModel))
                {
                    throw new ArgumentException("Модель техники не может быть пустой.");
                }

                if (string.IsNullOrEmpty(problemDescryption))
                {
                    throw new ArgumentException("Описание проблемы не может быть пустым.");
                }

                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();

                    SqlCommand command = new SqlCommand(insertQuery, connection);
                    command.Parameters.AddWithValue("@StartDate", DateTime.Now);
                    command.Parameters.AddWithValue("@HomeTechType", techType);
                    command.Parameters.AddWithValue("@HomeTechModel", techModel);
                    command.Parameters.AddWithValue("@ProblemDescryption", problemDescryption);
                    command.Parameters.AddWithValue("@RequestStatus", "Новая заявка");
                    command.Parameters.AddWithValue("@ClientID", clientID);

                    int newRequestID = Convert.ToInt32(command.ExecuteScalar()); 

                    string notificationQuery = @"
                        INSERT INTO Notifications (UserID, Message, isRead, isReadOperator)
                        VALUES (@UserID, @Message, 0, 0)";
                    SqlCommand notificationCommand = new SqlCommand(notificationQuery, connection);
                    notificationCommand.Parameters.AddWithValue("@UserID", GetManagerUserID());
                    notificationCommand.Parameters.AddWithValue("@Message", $"Пользователь добавил новую заявку №{newRequestID}.");
                    notificationCommand.ExecuteNonQuery();

                    MessageBox.Show("Заявка успешно добавлена", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (SqlException sqlEx)
            {
                MessageBox.Show("Ошибка базы данных при добавлении заявки: " + sqlEx.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (InvalidOperationException invalidOpEx)
            {
                MessageBox.Show("Ошибка при работе с базой данных: " + invalidOpEx.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Неизвестная ошибка при добавлении заявки: " + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private int GetManagerUserID()
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                string query = "SELECT userID FROM [User] WHERE status = 'Менеджер'";
                SqlCommand command = new SqlCommand(query, connection);
                return (int)command.ExecuteScalar();
            }
        }

        private void ClientRequestsWindowButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ClientRequestsWindow clientRequestsWindow = new ClientRequestsWindow(clientID, userStatus);
                clientRequestsWindow.Show();

                clientRequestsWindow.Left = this.Left;
                clientRequestsWindow.Top = this.Top;
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при открытии окна заявок: " + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void AddRequestWindowButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                AddRequestWindow addRequestWindow = new AddRequestWindow(clientID, userStatus);
                addRequestWindow.Show();

                addRequestWindow.Left = this.Left;
                addRequestWindow.Top = this.Top;
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при открытии окна добавления заявки: " + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ProfileButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ProfileWindow profileWindow = new ProfileWindow(clientID, userStatus);
                profileWindow.Show();

                profileWindow.Left = this.Left;
                profileWindow.Top = this.Top;
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при открытии окна профиля: " + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadUserNotifications()
        {
            List<string> notifications = GetUserNotifications(clientID);
            NotificationPopupStackPanel.Children.Clear();
            foreach (var notification in notifications)
            {
                Border border = new Border
                {
                    Background = new SolidColorBrush(Color.FromRgb(247, 243, 250)),
                    Padding = new Thickness(10),
                    CornerRadius = new CornerRadius(5),
                    Margin = new Thickness(10),
                    Tag = notification
                };

                TextBlock textBlock = new TextBlock
                {
                    Text = notification,
                    Foreground = new SolidColorBrush(Color.FromRgb(101, 97, 118))
                };

                border.Child = textBlock;
                border.MouseLeftButtonUp += NotificationBorder_MouseLeftButtonUp;
                NotificationPopupStackPanel.Children.Add(border);
            }
        }

        private void MarkNotificationsAsRead(int notificationID)
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    string updateQuery = "UPDATE Notifications SET IsRead = 1 WHERE NotificationID = @NotificationID";
                    SqlCommand command = new SqlCommand(updateQuery, connection);
                    command.Parameters.AddWithValue("@NotificationID", notificationID);
                    command.ExecuteNonQuery();

                    string countQuery = "SELECT COUNT(*) FROM Notifications WHERE userID = @UserID AND IsRead = 0";
                    SqlCommand countCommand = new SqlCommand(countQuery, connection);
                    countCommand.Parameters.AddWithValue("@UserID", clientID);
                    unreadNotificationsCount = (int)countCommand.ExecuteScalar();
                }

                if (unreadNotificationsCount <= 0)
                {
                    NotificationBadge.Visibility = Visibility.Collapsed;
                    NotificationCount.Visibility = Visibility.Collapsed;
                }
                else
                {
                    NotificationCount.Text = unreadNotificationsCount.ToString();
                }
            }
            catch (SqlException sqlEx)
            {
                MessageBox.Show("Ошибка базы данных при обновлении статуса уведомлений: " + sqlEx.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Неизвестная ошибка при обновлении статуса уведомлений: " + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void NotificationBorder_MouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            Border border = sender as Border;
            if (border != null && border.Tag != null)
            {
                string notification = border.Tag.ToString();

                int notificationID = ExtractNotificationID(notification);

                MarkNotificationsAsRead(notificationID);
            }
        }

        private int ExtractNotificationID(string notification)
        {
            string pattern = @"ID (\d+)";
            var match = Regex.Match(notification, pattern);
            if (match.Success)
            {
                return int.Parse(match.Groups[1].Value);
            }
            return -1;
        }

        private List<string> GetUserNotifications(int clientID)
        {
            List<string> notifications = new List<string>();
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                string query = "SELECT NotificationID, Message FROM Notifications WHERE UserID = @UserID";
                SqlCommand command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@UserID", clientID);
                SqlDataReader reader = command.ExecuteReader();
                while (reader.Read())
                {
                    string message = reader["Message"].ToString();
                    int notificationID = (int)reader["NotificationID"];
                    notifications.Add($"ID {notificationID}: {message}");
                }
            }
            return notifications;
        }

        private void NotificationButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                LoadUserNotifications();
                NotificationPopup.IsOpen = !NotificationPopup.IsOpen;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при открытии уведомлений: " + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UpdateNotificationBadge()
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    string query = "SELECT COUNT(*) FROM Notifications WHERE userID = @UserID AND IsRead = 0";
                    SqlCommand command = new SqlCommand(query, connection);
                    command.Parameters.AddWithValue("@UserID", clientID);

                    unreadNotificationsCount = (int)command.ExecuteScalar();
                }

                if (unreadNotificationsCount > 0)
                {
                    NotificationBadge.Visibility = Visibility.Visible;
                    NotificationCount.Visibility = Visibility.Visible;
                    NotificationCount.Text = unreadNotificationsCount.ToString();
                }
                else
                {
                    NotificationBadge.Visibility = Visibility.Collapsed;
                    NotificationCount.Visibility = Visibility.Collapsed;
                }
            }
            catch (SqlException sqlEx)
            {
                MessageBox.Show("Ошибка базы данных при обновлении уведомлений: " + sqlEx.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Неизвестная ошибка при обновлении уведомлений: " + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
