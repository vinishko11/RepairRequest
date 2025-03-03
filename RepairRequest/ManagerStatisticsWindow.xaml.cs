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
using Xceed.Words.NET;
using System.IO;
using Xceed.Document.NET;

namespace RepairRequest
{
    public partial class ManagerStatisticsWindow : Window
    {
        private int userID;
        private string userStatus;
        private string connectionString = "Server=desktop-85kbji0\\sqlexpress;Database=PR;Integrated Security=true";

        public ManagerStatisticsWindow(int userID, string userStatus)
        {
            InitializeComponent();
            this.userID = userID;
            this.userStatus = userStatus;
            LoadStatistics();
        }

        private void LoadStatistics()
        {
            LoadRequestsByStatus();
            LoadCompletedRequestsCount();
            LoadAverageCompletionTime();
            LoadRequestsByMaster();
            LoadReadNotificationsCount();
            LoadNotificationsByTime();
        }

        private void LoadRequestsByStatus()
        {
            var data = new Dictionary<string, int>();

            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();
                var command = new SqlCommand("SELECT requestStatus, COUNT(*) AS Count FROM Request GROUP BY requestStatus", connection);
                var reader = command.ExecuteReader();
                while (reader.Read())
                {
                    data.Add(reader["requestStatus"].ToString(), (int)reader["Count"]);
                }
            }ya zaebalas'



            RequestsByStatusListView.ItemsSource = data;
        }

        private void LoadCompletedRequestsCount()
        {
            int completedRequestsCount;

            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();
                var command = new SqlCommand("SELECT COUNT(*) FROM Request WHERE requestStatus = 'Готова к выдаче'", connection);
                completedRequestsCount = (int)command.ExecuteScalar();
            }

            CompletedRequestsCountTextBlock.Text = completedRequestsCount.ToString();
        }
// ну и хуйня
        private void LoadAverageCompletionTime()
        {
            double averageCompletionTime;

            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();
                var command = new SqlCommand("SELECT AVG(DATEDIFF(day, startDate, completionDate)) FROM Request WHERE completionDate IS NOT NULL", connection);
                var result = command.ExecuteScalar();
                if (result != DBNull.Value)
                {
                    averageCompletionTime = Convert.ToDouble(result);
                    AverageCompletionTimeTextBlock.Text = averageCompletionTime.ToString("F2");
                }
                else
                {
                    AverageCompletionTimeTextBlock.Text = "N/A";
                }
            }
        }

        private void LoadRequestsByMaster()
        {
            var data = new Dictionary<int, int>();

            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();
                var command = new SqlCommand("SELECT masterID, COUNT(*) AS Count FROM Request GROUP BY masterID", connection);
                var reader = command.ExecuteReader();
                while (reader.Read())
                {
                    var masterID = reader["masterID"] != DBNull.Value ? (int)reader["masterID"] : 0;
                    var count = (int)reader["Count"];
                    data.Add(masterID, count);
                }
            }

            RequestsByMasterListView.ItemsSource = data.Select(kvp => new { MasterID = kvp.Key, Count = kvp.Value }).ToList();
        }

        private void LoadReadNotificationsCount()
        {
            int readNotificationsCount;

            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();
                var command = new SqlCommand("SELECT COUNT(*) FROM Notifications WHERE isReadOperator = 1", connection);
                readNotificationsCount = (int)command.ExecuteScalar();
            }

            ReadNotificationsCountTextBlock.Text = readNotificationsCount.ToString();
        }

        private void LoadNotificationsByTime()
        {
            var data = new Dictionary<DateTime, int>();

            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();
                var command = new SqlCommand("SELECT CAST(createdAt AS DATE) AS Date, COUNT(*) AS Count FROM Notifications GROUP BY CAST(createdAt AS DATE)", connection);
                var reader = command.ExecuteReader();
                while (reader.Read())
                {
                    var date = reader["Date"] != DBNull.Value ? (DateTime)reader["Date"] : DateTime.MinValue;
                    var count = (int)reader["Count"];
                    data.Add(date, count);
                }
            }

            NotificationsByTimeListView.ItemsSource = data.Select(kvp => new { Date = kvp.Key, Count = kvp.Value }).ToList();
        }

        private void GenerateReportButton_Click(object sender, RoutedEventArgs e)
        {
            string reportPath = @"D:\RepairRequest\Reports\ManagerReport_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".docx";
            using (var document = DocX.Create(reportPath))
            {
                document.InsertParagraph("Отчет по статистике менеджера").FontSize(20).Bold().Alignment = Alignment.center;
                document.InsertParagraph($"Дата: {DateTime.Now.ToString("dd.MM.yyyy HH:mm")}\n\n");

                document.InsertParagraph("Количество запросов по статусу:").Bold();
                foreach (var item in (Dictionary<string, int>)RequestsByStatusListView.ItemsSource)
                {
                    document.InsertParagraph($"Статус: {item.Key}, Количество: {item.Value}");
                }
                document.InsertParagraph("\n");

                document.InsertParagraph($"Количество выполненных заявок: {CompletedRequestsCountTextBlock.Text}").Bold();
                document.InsertParagraph($"Среднее время выполнения запросов (дни): {AverageCompletionTimeTextBlock.Text}\n").Bold();

                document.InsertParagraph("Распределение запросов по мастерам:").Bold();
                foreach (var item in RequestsByMasterListView.ItemsSource)
                {
                    var masterItem = item.GetType().GetProperty("MasterID").GetValue(item);
                    var countItem = item.GetType().GetProperty("Count").GetValue(item);
                    document.InsertParagraph($"Мастер ID: {masterItem}, Количество: {countItem}");
                }
                document.InsertParagraph("\n");

                document.InsertParagraph($"Количество уведомлений, прочитанных оператором: {ReadNotificationsCountTextBlock.Text}").Bold();
                document.InsertParagraph("Распределение уведомлений по времени:").Bold();
                foreach (var item in NotificationsByTimeListView.ItemsSource)
                {
                    var dateItem = item.GetType().GetProperty("Date").GetValue(item);
                    var countItem = item.GetType().GetProperty("Count").GetValue(item);
                    document.InsertParagraph($"Дата: {((DateTime)dateItem).ToString("dd.MM.yyyy")}, Количество: {countItem}");
                }
                document.Save();
            }

            MessageBox.Show($"Отчет сохранен в файл: {reportPath}", "Отчет сформирован", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void ProfileButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ProfileWindow profileWindow = new ProfileWindow(userID, userStatus);
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
    }
}