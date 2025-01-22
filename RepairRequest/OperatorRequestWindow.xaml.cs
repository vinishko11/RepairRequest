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
using System.Windows.Threading;

namespace RepairRequest
{
    public partial class OperatorRequestWindow : Window
    {
        private string connectionString = "Server=desktop-85kbji0\\sqlexpress;Database=PR;Integrated Security=true";
        private int operatorID;
        private string userStatus;
        public DataTable RepairRequests { get; set; }
        public DataTable MastersTable { get; set; }
        private int unreadNotificationsCount = 0;
        private DataRowView selectedMaster;
        private DataRowView selectedRequest;

        public OperatorRequestWindow(int operatorID, string userStatus)
        {
            InitializeComponent();
            this.operatorID = operatorID;
            this.userStatus = userStatus;
            LoadMasters();
            LoadRequests();
            DataContext = this;
            UpdateNotificationBadge();
        }

        private void LoadMasters()
        {
            MastersTable = new DataTable();
            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    string query = "SELECT userID, fullName FROM [User] WHERE Status = 'Мастер'";
                    SqlCommand command = new SqlCommand(query, connection);

                    SqlDataAdapter adapter = new SqlDataAdapter(command);
                    adapter.Fill(MastersTable);
                }
            }
            catch (SqlException sqlEx)
            {
                MessageBox.Show("Ошибка базы данных при загрузке мастеров: " + sqlEx.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Неизвестная ошибка при загрузке мастеров: " + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
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
                            r.requestID AS Id,
                            r.homeTechType AS TechType,
                            r.homeTechModel AS TechModel, 
                            r.startDate AS RequestDate, 
                            r.requestStatus AS Status, 
                            r.completionDate AS CompletionDate,
                            r.problemDescryption AS Description,
                            r.masterID AS MasterID,
                            u.fullName AS MasterFullName
                        FROM Request r
                        LEFT JOIN [User] u ON r.masterID = u.userID";
                    SqlCommand command = new SqlCommand(query, connection);

                    SqlDataAdapter adapter = new SqlDataAdapter(command);
                    adapter.Fill(RepairRequests);
                }

                NoRequestsTextBlock.Visibility = RepairRequests.Rows.Count == 0 ? Visibility.Visible : Visibility.Collapsed;

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
                            r.masterID AS MasterID,
                            u.fullName AS MasterFullName
                        FROM Request r
                        LEFT JOIN [User] u ON r.masterID = u.userID
                        WHERE r.requestID LIKE @searchText
                        OR r.homeTechType LIKE @searchText
                        OR r.requestStatus LIKE @searchText
                        OR r.problemDescryption LIKE @searchText";

                    SqlDataAdapter adapter = new SqlDataAdapter(query, connection);
                    adapter.SelectCommand.Parameters.AddWithValue("@searchText", "%" + searchText + "%");
                    RepairRequests.Clear();
                    adapter.Fill(RepairRequests);

                    NoRequestsTextBlock.Visibility = RepairRequests.Rows.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
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

        private void OperatorRequestsWindowButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                OperatorRequestWindow operatorRequestWindow = new OperatorRequestWindow(operatorID, userStatus);
                operatorRequestWindow.Show();

                operatorRequestWindow.Left = this.Left;
                operatorRequestWindow.Top = this.Top;
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при открытии окна заявок: " + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ProfileButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ProfileWindow profileWindow = new ProfileWindow(operatorID, userStatus);
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

        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count > 0)
            {
                selectedMaster = e.AddedItems[0] as DataRowView;
                selectedRequest = ((FrameworkElement)sender).DataContext as DataRowView;
            }
        }

        private void EditButton_Click(object sender, RoutedEventArgs e)
        {
            if (selectedMaster != null && selectedRequest != null)
            {
                MessageBoxResult result = MessageBox.Show("Вы уверены, что хотите назначить выбранного мастера для этой заявки?", "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (result == MessageBoxResult.Yes)
                {
                    int requestID = (int)selectedRequest["Id"];
                    int masterID = (int)selectedMaster["userID"];

                    try
                    {
                        using (SqlConnection connection = new SqlConnection(connectionString))
                        {
                            connection.Open();
                            string query = "UPDATE Request SET masterID = @masterID WHERE requestID = @requestID";
                            SqlCommand command = new SqlCommand(query, connection);
                            command.Parameters.AddWithValue("@masterID", masterID);
                            command.Parameters.AddWithValue("@requestID", requestID);

                            command.ExecuteNonQuery();

                            string notificationQuery = "INSERT INTO Notifications (UserID, Message, IsRead, IsReadOperator) VALUES (@UserID, @Message, 0, 0)";
                            SqlCommand notificationCommand = new SqlCommand(notificationQuery, connection);
                            notificationCommand.Parameters.AddWithValue("@UserID", masterID);
                            notificationCommand.Parameters.AddWithValue("@Message", $"Мастер {selectedMaster["fullName"]} назначен исполнителем заявки №{requestID}");

                            notificationCommand.ExecuteNonQuery();

                            selectedRequest["MasterID"] = masterID;
                            selectedRequest["MasterFullName"] = selectedMaster["fullName"];
                        }
                    }
                    catch (SqlException sqlEx)
                    {
                        MessageBox.Show("Ошибка базы данных при обновлении мастера: " + sqlEx.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Неизвестная ошибка при обновлении мастера: " + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            else
            {
                MessageBox.Show("Пожалуйста, выберите мастера и заявку, прежде чем нажать на кнопку карандаша.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private int GetRequestUserID(int requestID)
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                string query = "SELECT userID FROM [User] WHERE userID = (SELECT masterID FROM Request WHERE requestID = @RequestID)";
                SqlCommand command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@RequestID", requestID);
                var result = command.ExecuteScalar();
                if (result != null && int.TryParse(result.ToString(), out int userID))
                {
                    return userID;
                }
                else
                {
                    throw new Exception("Ошибка получения userID для заявки.");
                }
            }
        }

        private void LoadUserNotifications()
        {
            List<string> notifications = GetAllNotifications();
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
                    string updateQuery = "UPDATE Notifications SET IsReadOperator = 1 WHERE NotificationID = @NotificationID";
                    SqlCommand command = new SqlCommand(updateQuery, connection);
                    command.Parameters.AddWithValue("@NotificationID", notificationID);
                    command.ExecuteNonQuery();

                    string countQuery = "SELECT COUNT(*) FROM Notifications WHERE isReadOperator = 0";
                    SqlCommand countCommand = new SqlCommand(countQuery, connection);

                    unreadNotificationsCount = (int)countCommand.ExecuteScalar();

                    UpdateNotificationBadge();
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

        private void UpdateNotificationBadge()
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    string query = "SELECT COUNT(*) FROM Notifications WHERE isReadOperator = 0";
                    SqlCommand command = new SqlCommand(query, connection);

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

        private int ExtractRequestID(string notification)
        {
            string pattern = @"№(\d+)";
            var match = Regex.Match(notification, pattern);
            if (match.Success)
            {
                return int.Parse(match.Groups[1].Value);
            }
            return -1;
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

        private List<string> GetAllNotifications()
        {
            List<string> notifications = new List<string>();
            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    string query = "SELECT NotificationID, Message FROM Notifications";
                    SqlCommand command = new SqlCommand(query, connection);
                    SqlDataReader reader = command.ExecuteReader();
                    while (reader.Read())
                    {
                        string message = reader["Message"].ToString();
                        int notificationID = (int)reader["NotificationID"];
                        notifications.Add($"ID {notificationID}: {message}");
                    }
                }
            }
            catch (SqlException sqlEx)
            {
                MessageBox.Show("Ошибка базы данных при получении уведомлений: " + sqlEx.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Неизвестная ошибка при получении уведомлений: " + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            return notifications;
        }
    }
}