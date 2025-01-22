using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Data;
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
using System.Text.RegularExpressions;
using System.Xml.Linq;
using System.Windows.Threading;

namespace RepairRequest
{
    public partial class ClientRequestsWindow : Window
    {
        private int clientID;
        private string userStatus;
        private string connectionString = "Server=desktop-85kbji0\\sqlexpress;Database=PR;Integrated Security=true";
        public int RequestID { get; private set; }
        public DataTable RepairRequests { get; set; }
        private int unreadNotificationsCount = 0;

        public ClientRequestsWindow(int clientID, string userStatus)
        {
            InitializeComponent();
            this.clientID = clientID;
            this.userStatus = userStatus;
            LoadRequests();
            UpdateNotificationBadge();
        }

        private void LoadRequests()
        {
            RepairRequests = new DataTable();

            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    string query = @"
                SELECT 
                    R.requestID AS Id,
                    R.homeTechType AS TechType,
                    R.homeTechModel AS TechModel, 
                    R.startDate AS RequestDate, 
                    R.requestStatus AS Status, 
                    R.completionDate AS CompletionDate,
                    R.problemDescryption AS Description,
                    C.Message AS Message
                    FROM Request R
                    LEFT JOIN Comment C ON R.requestID = C.requestID
                    WHERE R.clientID = @ClientID";
                    SqlCommand command = new SqlCommand(query, connection);
                    command.Parameters.AddWithValue("@ClientID", clientID);

                    SqlDataAdapter adapter = new SqlDataAdapter(command);
                    adapter.Fill(RepairRequests);
                }

                if (RepairRequests.Rows.Count == 0)
                {
                    NoRequestsTextBlock.Visibility = Visibility.Visible;
                }
                else
                {
                    NoRequestsTextBlock.Visibility = Visibility.Collapsed;
                }

                this.DataContext = new { RepairRequests = RepairRequests.DefaultView };
            }
            catch (SqlException sqlEx)
            {
                MessageBox.Show("Ошибка базы данных при загрузке заявок: " + sqlEx.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Неизвестная ошибка при загрузке заявок: " + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
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
                int requestID = ExtractRequestID(notification);

                MarkNotificationsAsRead(notificationID);

                HighlightRequest(requestID);
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

        private int ExtractRequestID(string notification)
        {
            string pattern = @"заявке №(\d+)";
            var match = Regex.Match(notification, pattern);
            if (match.Success)
            {
                return int.Parse(match.Groups[1].Value);
            }
            return -1;
        }

        private void HighlightRequest(int requestID)
        {
            foreach (var item in RepairRequestsGrid.Items)
            {
                DataRowView rowView = item as DataRowView;
                if (rowView != null && Convert.ToInt32(rowView["Id"]) == requestID)
                {
                    var container = RepairRequestsGrid.ItemContainerGenerator.ContainerFromItem(item) as FrameworkElement;
                    if (container != null)
                    {
                        Border border = FindVisualChild<Border>(container);
                        if (border != null)
                        {
                            border.BorderBrush = Brushes.Gray;
                            border.BorderThickness = new Thickness(2);

                            DispatcherTimer timer = new DispatcherTimer();
                            timer.Interval = TimeSpan.FromSeconds(2);
                            timer.Tick += (s, e) =>
                            {
                                border.BorderBrush = Brushes.Transparent;
                                border.BorderThickness = new Thickness(0);
                                timer.Stop();
                            };
                            timer.Start();
                        }
                    }
                    break;
                }
            }
        }

        private T FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child != null && child is T)
                {
                    return (T)child;
                }
                else
                {
                    var childOfChild = FindVisualChild<T>(child);
                    if (childOfChild != null)
                    {
                        return childOfChild;
                    }
                }
            }
            return null;
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

        private void OptionsButton_Click(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            if (button != null && button.ContextMenu != null)
            {
                button.ContextMenu.PlacementTarget = button;
                button.ContextMenu.IsOpen = true;
            }
        }

        private void EditRequestMenuItem_Click(object sender, RoutedEventArgs e)
        {
            MenuItem menuItem = sender as MenuItem;
            if (menuItem != null && menuItem.Tag != null)
            {
                int requestID = (int)menuItem.Tag;
                this.RequestID = requestID;

                try
                {
                    using (SqlConnection connection = new SqlConnection(connectionString))
                    {
                        connection.Open();
                        string query = "SELECT requestStatus FROM Request WHERE requestID = @RequestID";
                        SqlCommand command = new SqlCommand(query, connection);
                        command.Parameters.AddWithValue("@RequestID", requestID);

                        string status = command.ExecuteScalar() as string;
                        if (status == "Новая заявка")
                        {
                            EditRequestWindow editRequestWindow = new EditRequestWindow(requestID, clientID, userStatus);
                            editRequestWindow.Show();

                            editRequestWindow.Left = this.Left;
                            editRequestWindow.Top = this.Top;
                            this.Close();
                        }
                        else
                        {
                            MessageBox.Show("Можно редактировать только заявки со статусом 'Новая заявка'.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                }
                catch (SqlException sqlEx)
                {
                    MessageBox.Show("Ошибка базы данных при редактировании заявки: " + sqlEx.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Неизвестная ошибка при редактировании заявки: " + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void DeleteRequestMenuItem_Click(object sender, RoutedEventArgs e)
        {
            MenuItem menuItem = sender as MenuItem;
            if (menuItem != null && menuItem.Tag != null)
            {
                int requestID = (int)menuItem.Tag;

                MessageBoxResult result = MessageBox.Show("Вы уверены, что хотите удалить эту заявку? Данное действие нельзя отмеенить в дальнейшем.", "Подтверждение удаления", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        using (SqlConnection connection = new SqlConnection(connectionString))
                        {
                            connection.Open();

                            string deleteCommentsQuery = "DELETE FROM Comment WHERE requestID = @RequestID";
                            SqlCommand deleteCommentsCommand = new SqlCommand(deleteCommentsQuery, connection);
                            deleteCommentsCommand.Parameters.AddWithValue("@RequestID", requestID);
                            deleteCommentsCommand.ExecuteNonQuery();

                            string deleteNotificationQuery = @"DELETE FROM Notifications WHERE requestID = requestID";
                            SqlCommand deleteNotificationCommand = new SqlCommand(deleteNotificationQuery, connection);
                            deleteNotificationCommand.Parameters.AddWithValue("@RequestID", requestID);
                            deleteNotificationCommand.ExecuteNonQuery();

                            string notificationQuery = @"
                            INSERT INTO Notifications (UserID, Message, isRead, isReadOperator)
                            VALUES (@UserID, @Message, 0, 0)";
                            SqlCommand notificationCommand = new SqlCommand(notificationQuery, connection);
                            notificationCommand.Parameters.AddWithValue("@UserID", GetRequestUserID(requestID));
                            notificationCommand.Parameters.AddWithValue("@Message", $"Пользователь удалил заявку №{requestID}.");
                            notificationCommand.ExecuteNonQuery();

                            string deleteRequestQuery = "DELETE FROM Request WHERE requestID = @RequestID";
                            SqlCommand deleteRequestCommand = new SqlCommand(deleteRequestQuery, connection);
                            deleteRequestCommand.Parameters.AddWithValue("@RequestID", requestID);
                            deleteRequestCommand.ExecuteNonQuery();

                            LoadUserNotifications();
                            LoadRequests();
                        }
                    }
                    catch (SqlException sqlEx)
                    {
                        MessageBox.Show("Ошибка базы данных при удалении заявки: " + sqlEx.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Неизвестная ошибка при удалении заявки: " + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
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
    }
}