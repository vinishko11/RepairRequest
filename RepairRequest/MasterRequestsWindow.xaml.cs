using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

namespace RepairRequest
{
    public partial class MasterRequestsWindow : Window
    {
        private string connectionString = "Server=desktop-85kbji0\\sqlexpress;Database=PR;Integrated Security=true";
        private int masterID;
        private string userStatus;
        public int RequestID { get; private set; }
        public DataTable RepairRequests { get; set; }
        private int unreadNotificationsCount = 0;

        public MasterRequestsWindow(int masterID, string userStatus)
        {
            this.masterID = masterID;
            this.userStatus = userStatus;
            InitializeComponent();
            DataContext = this;
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
                            requestID AS Id,
                            homeTechType AS TechType,
                            homeTechModel AS TechModel, 
                            startDate AS RequestDate, 
                            requestStatus AS Status, 
                            completionDate AS CompletionDate,
                            problemDescryption AS Description
                        FROM Request 
                        WHERE masterID = @MasterID";
                    SqlCommand command = new SqlCommand(query, connection);
                    command.Parameters.AddWithValue("@MasterID", masterID);

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

        private void SearchRequests(string searchText)
        {
            if (string.IsNullOrWhiteSpace(searchText))
            {
                LoadRequests(); 
                return;
            }

            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    string query = @"
                        SELECT 
                            r.requestID AS Id,
                            r.homeTechType AS TechType,
                            r.homeTechModel AS TechModel, 
                            r.startDate AS RequestDate, 
                            r.requestStatus AS Status, 
                            r.completionDate AS CompletionDate,
                            r.problemDescryption AS Description,
                            r.masterID AS MasterID
                        FROM Request r
                        WHERE r.masterID = @MasterID AND 
                              (r.requestID LIKE @SearchText
                              OR r.homeTechType LIKE @SearchText
                              OR r.requestStatus LIKE @SearchText
                              OR r.problemDescryption LIKE @SearchText)";
                    SqlCommand command = new SqlCommand(query, connection);
                    command.Parameters.AddWithValue("@MasterID", masterID);
                    command.Parameters.AddWithValue("@SearchText", "%" + searchText + "%");

                    SqlDataAdapter adapter = new SqlDataAdapter(command);
                    RepairRequests.Clear();
                    adapter.Fill(RepairRequests);

                    if (RepairRequests.Rows.Count == 0)
                    {
                        NoRequestsTextBlock.Visibility = Visibility.Visible;
                    }
                    else
                    {
                        NoRequestsTextBlock.Visibility = Visibility.Collapsed;
                    }
                }
            }
            catch (SqlException sqlEx)
            {
                MessageBox.Show("Ошибка базы данных при поиске заявок: " + sqlEx.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Неизвестная ошибка при поиске заявок: " + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            SearchRequests(SearchBox.Text);
        }

        private void MasterRequestsWindowButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                MasterRequestsWindow masterRequestWindow = new MasterRequestsWindow(masterID, userStatus);
                masterRequestWindow.Show();

                masterRequestWindow.Left = this.Left;
                masterRequestWindow.Top = this.Top;
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при открытии окна заявок: " + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CommentButton_Click(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            DataRowView row = button.DataContext as DataRowView;
            if (row != null)
            {
                string status = row["Status"].ToString();
                if (status == "Готова к выдаче")
                {
                    MessageBox.Show("Изменить данные нельзя", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
                else
                {
                    int requestID = (int)row["Id"];
                    this.RequestID = requestID;

                    AddDetailsWindow addDetailsWindow = new AddDetailsWindow(RequestID, masterID, userStatus);
                    addDetailsWindow.Show();

                    addDetailsWindow.Left = this.Left;
                    addDetailsWindow.Top = this.Top;
                    this.Close();
                }
            }
        }

        private void ProfileButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ProfileWindow profileWindow = new ProfileWindow(masterID, userStatus);
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
            List<string> notifications = GetUserNotifications(masterID);
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
                    countCommand.Parameters.AddWithValue("@UserID", masterID);
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
            string pattern = @"заявки №(\d+)";
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

        private List<string> GetUserNotifications(int masterID)
        {
            List<string> notifications = new List<string>();
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                string query = "SELECT NotificationID, Message FROM Notifications WHERE UserID = @UserID";
                SqlCommand command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@UserID", masterID);
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
                    command.Parameters.AddWithValue("@UserID", masterID);

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