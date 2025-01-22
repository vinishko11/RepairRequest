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
    public partial class AddDetailsWindow : Window
    {
        private int masterID;
        private int requestID;
        private string userStatus;
        private string connectionString = "Server=desktop-85kbji0\\sqlexpress;Database=PR;Integrated Security=true";

        public AddDetailsWindow(int requestID, int masterID, string userStatus)
        {
            this.masterID = masterID;
            this.requestID = requestID;
            this.userStatus = userStatus;
            InitializeComponent();
            LoadRequestDetails();
        }

        private void LoadRequestDetails()
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                string query = "SELECT * FROM Request WHERE requestID = @RequestID";
                SqlCommand command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@RequestID", requestID);
                SqlDataReader reader = command.ExecuteReader();
                if (reader.Read())
                {
                    RequestIdTextBlock.Text = reader["requestID"].ToString();
                    TechTypeTextBlock.Text = reader["homeTechType"].ToString();
                    DescriptionTextBlock.Text = reader["problemDescryption"].ToString();
                    RequestDateTextBlock.Text = Convert.ToDateTime(reader["startDate"]).ToString("dd.MM.yyyy");
                    StatusTextBlock.Text = reader["requestStatus"].ToString();
                    CompletionDateTextBlock.Text = reader["completionDate"] != DBNull.Value ? Convert.ToDateTime(reader["completionDate"]).ToString("dd.MM.yyyy") : "N/A";

                    string currentStatus = reader["requestStatus"].ToString();
                    foreach (ComboBoxItem item in StatusComboBox.Items)
                    {
                        if (item.Content.ToString() == currentStatus)
                        {
                            item.IsSelected = true;
                            break;
                        }
                    }

                    RepairPartsTextBox.Text = reader["repairParts"].ToString();
                }
                reader.Close();

                if (CommentExists())
                {
                    LoadComment();
                }
            }
        }

        private void LoadComment()
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                string query = "SELECT Message FROM Comment WHERE requestID = @RequestID";
                SqlCommand command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@RequestID", requestID);
                object comment = command.ExecuteScalar();
                if (comment != null)
                {
                    CommentTextBox.Text = comment.ToString();
                }
            }
        }
        private bool CommentExists()
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                string query = "SELECT COUNT(*) FROM Comment WHERE requestID = @RequestID";
                SqlCommand command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@RequestID", requestID);
                int commentCount = (int)command.ExecuteScalar();
                return commentCount > 0;
            }
        }

        private void AddCommentButton_Click(object sender, RoutedEventArgs e)
        {
            string commentText = CommentTextBox.Text;
            if (string.IsNullOrEmpty(commentText))
            {
                MessageBox.Show("Комментарий не может быть пустым.");
                return;
            }

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();

                if (CommentExists())
                {
                    string updateQuery = "UPDATE Comment SET Message = @Message WHERE requestID = @RequestID";
                    SqlCommand updateCommand = new SqlCommand(updateQuery, connection);
                    updateCommand.Parameters.AddWithValue("@Message", commentText);
                    updateCommand.Parameters.AddWithValue("@RequestID", requestID);
                    updateCommand.ExecuteNonQuery();
                    MessageBox.Show("Комментарий успешно обновлен.");
                }
                else
                {
                    string insertQuery = "INSERT INTO Comment (Message, requestID, masterID) VALUES (@Message, @RequestID, @MasterID)";
                    SqlCommand insertCommand = new SqlCommand(insertQuery, connection);
                    insertCommand.Parameters.AddWithValue("@Message", commentText);
                    insertCommand.Parameters.AddWithValue("@RequestID", requestID);
                    insertCommand.Parameters.AddWithValue("@MasterID", masterID);
                    insertCommand.ExecuteNonQuery();
                    MessageBox.Show("Комментарий успешно добавлен.");
                }

                string notificationQuery = "INSERT INTO Notifications (UserID, Message, IsRead, IsReadOperator) VALUES (@UserID, @Message, 0, 0)";
                SqlCommand notificationCommand = new SqlCommand(notificationQuery, connection);
                notificationCommand.Parameters.AddWithValue("@UserID", GetRequestUserID(requestID));
                notificationCommand.Parameters.AddWithValue("@Message", $"Мастер оставил новый комментарий к заявке №{requestID}.");
                notificationCommand.ExecuteNonQuery();
            }
        }

        private int GetRequestUserID(int requestID)
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                string query = "SELECT clientID FROM Request WHERE requestID = @RequestID";
                SqlCommand command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@RequestID", requestID);
                return (int)command.ExecuteScalar();
            }
        }

        private void SaveStatusRepairPartsAndCompletionDate(string status, string repairParts, DateTime? completionDate)
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();

                string updateQuery = "UPDATE Request SET requestStatus = @Status, repairParts = @RepairParts";

                if (completionDate.HasValue)
                {
                    updateQuery += ", completionDate = @CompletionDate";
                }

                updateQuery += " WHERE requestID = @RequestID";

                SqlCommand command = new SqlCommand(updateQuery, connection);
                command.Parameters.AddWithValue("@Status", status);
                command.Parameters.AddWithValue("@RepairParts", repairParts);
                command.Parameters.AddWithValue("@RequestID", requestID);

                if (completionDate.HasValue)
                {
                    command.Parameters.AddWithValue("@CompletionDate", completionDate.Value);
                }

                string notificationQuery = "INSERT INTO Notifications (UserID, Message, IsRead, IsReadOperator) VALUES (@UserID, @Message, 0, 0)";
                SqlCommand notificationCommand = new SqlCommand(notificationQuery, connection);
                notificationCommand.Parameters.AddWithValue("@UserID", GetRequestUserID(requestID));
                notificationCommand.Parameters.AddWithValue("@Message", $"Мастер изменил детали о заявке №{requestID}.");
                notificationCommand.ExecuteNonQuery();

                try
                {
                    int rowsAffected = command.ExecuteNonQuery();
                    if (rowsAffected > 0)
                    {
                        MessageBox.Show("Статус, ремонтные работы и дата завершения успешно сохранены.");
                    }
                    else
                    {
                        MessageBox.Show("Не удалось сохранить статус, ремонтные работы и дату завершения.");
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Ошибка при сохранении данных: " + ex.Message);
                }
            }
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

        private void NotificationButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                NotificationPopup.IsOpen = !NotificationPopup.IsOpen;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при открытии уведомлений: " + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void AddDetailsButton_Click(object sender, RoutedEventArgs e)
        {
            string status = ((ComboBoxItem)StatusComboBox.SelectedItem).Content.ToString();
            string repairParts = RepairPartsTextBox.Text;

            if (string.IsNullOrWhiteSpace(status))
            {
                MessageBox.Show("Пожалуйста, выберите статус заявки.");
                return;
            }

            DateTime? completionDate = null;

            if (status == "В процессе ремонта")
            {
                RepairPartsTextBox.IsEnabled = false;
                RepairPartsTextBox.Text = " ";

                SaveStatusRepairPartsAndCompletionDate(status, repairParts, completionDate);
            }
            else
            {
                RepairPartsTextBox.IsEnabled = true;

                if (string.IsNullOrWhiteSpace(repairParts))
                {
                    MessageBox.Show("Пожалуйста, заполните ремонтные работы.");
                    return;
                }

                if (status == "Готова к выдаче")
                {
                    if (ShowConfirmationDialog())
                    {
                        completionDate = DateTime.Today;
                        SaveStatusRepairPartsAndCompletionDate(status, repairParts, completionDate);
                    }
                }
                else
                {
                    SaveStatusRepairPartsAndCompletionDate(status, repairParts, completionDate);
                }
            }
        }

        private bool ShowConfirmationDialog()
        {
            MessageBoxResult result = MessageBox.Show("После сохранения данных изменить их будет невозможно. Вы уверены, что хотите продолжить?", "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            return result == MessageBoxResult.Yes;
        }
    }
}

