using System;
using System.Collections.Generic;
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

namespace RepairRequest
{
    public partial class EditRequestWindow : Window
    {
        private int clientID;
        private string userStatus;
        private int requestID;
        private string connectionString = "Server=desktop-85kbji0\\sqlexpress;Database=PR;Integrated Security=true";
        private int unreadNotificationsCount = 0;


        public EditRequestWindow(int requestID, int clientID, string userStatus)
        {
            this.clientID = clientID;
            this.requestID = requestID;
            this.userStatus = userStatus;
            InitializeComponent();
            UpdateNotificationBadge();
        }

        private void EditRequestButton_Click(object sender, RoutedEventArgs e)
        {
            string techType = techTypeTextBox.Text;
            string techModel = techModelTextBox.Text;
            string problemDescryption = problemDescriptionTextBox.Text;

            try
            {
                if (string.IsNullOrEmpty(techType) || string.IsNullOrEmpty(techModel) || string.IsNullOrEmpty(problemDescryption))
                {
                    MessageBox.Show("Заполните все поля", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                string updateQuery = @"UPDATE Request SET homeTechType = @HomeTechType, homeTechModel = @HomeTechModel, problemDescryption = @ProblemDescryption WHERE requestID = @RequestID";

                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();

                    using (SqlCommand command = new SqlCommand(updateQuery, connection))
                    {
                        command.Parameters.AddWithValue("@HomeTechType", techType);
                        command.Parameters.AddWithValue("HomeTechModel", techModel);
                        command.Parameters.AddWithValue("@ProblemDescryption", problemDescryption);
                        command.Parameters.AddWithValue("@RequestID", requestID);

                        int rowsAffected = command.ExecuteNonQuery();

                        if (rowsAffected > 0)
                        {
                            string notificationQuery = @"
                            INSERT INTO Notifications (UserID, Message, isRead, isReadOperator)
                            VALUES (@UserID, @Message, 0, 0)";

                            using (SqlCommand notificationCommand = new SqlCommand(notificationQuery, connection))
                            {
                                notificationCommand.Parameters.AddWithValue("@UserID", GetRequestUserID(requestID));
                                notificationCommand.Parameters.AddWithValue("@Message", $"Пользователь обновил заявку №{requestID}.");

                                int notificationRowsAffected = notificationCommand.ExecuteNonQuery();
                                if (notificationRowsAffected > 0)
                                {
                                    MessageBox.Show("Данные успешно сохранены.");

                                    ClientRequestsWindow clientRequestsWindow = new ClientRequestsWindow(clientID, userStatus);
                                    clientRequestsWindow.Show();

                                    clientRequestsWindow.Left = this.Left;
                                    clientRequestsWindow.Top = this.Top;
                                    this.Close();
                                }
                                else
                                {
                                    MessageBox.Show("Не удалось сохранить данные.");
                                }
                            }
                        }
                        else
                        {
                            MessageBox.Show("Не удалось сохранить данные.");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Неизвестная ошибка при обновлении заявки: " + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private int GetRequestUserID(int requestID)
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                string query = "SELECT masterID FROM Request WHERE requestID = @RequestID";
                SqlCommand command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@RequestID", requestID);
                var result = command.ExecuteScalar();

                if (result != null && int.TryParse(result.ToString(), out int masterID))
                {
                    return masterID;
                }
                else
                {
                    string clientIDQuery = "SELECT clientID FROM Request WHERE requestID = @RequestID";
                    SqlCommand clientIDCommand = new SqlCommand(clientIDQuery, connection);
                    clientIDCommand.Parameters.AddWithValue("@RequestID", requestID);
                    var clientIDResult = clientIDCommand.ExecuteScalar();
                    if (clientIDResult != null && int.TryParse(clientIDResult.ToString(), out int clientID))
                    {
                        return clientID;
                    }
                    else
                    {
                        throw new Exception("Ошибка получения userID для заявки.");
                    }
                }
            }
        }

        private void ClientRequestsWindowButton_Click(object sender, RoutedEventArgs e)
        {
            ClientRequestsWindow clientRequestsWindow = new ClientRequestsWindow(clientID, userStatus);
            clientRequestsWindow.Show();

            clientRequestsWindow.Left = this.Left;
            clientRequestsWindow.Top = this.Top;
            this.Close();
        }

        private void AddRequestWindowButton_Click(object sender, RoutedEventArgs e)
        {
            AddRequestWindow addRequestWindow = new AddRequestWindow(clientID, userStatus);
            addRequestWindow.Show();

            addRequestWindow.Left = this.Left;
            addRequestWindow.Top = this.Top;
            this.Close();
        }

        private void ProfileButton_Click(object sender, RoutedEventArgs e)
        {
            ProfileWindow profileWindow = new ProfileWindow(clientID, userStatus);
            profileWindow.Show();

            profileWindow.Left = this.Left;
            profileWindow.Top = this.Top;
            this.Close();
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
