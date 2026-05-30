using Npgsql;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using WpfApp.Database;

namespace WpfApp2
{
    public partial class StatusWindow : Window
    {
        private DatabaseConnection dbconnection = new DatabaseConnection();
        private string currentUserRole;
        private string currentUserLogin;

        public class Status
        {
            public int StatusId { get; set; }
            public string StatusName { get; set; }
        }

        public StatusWindow(string userRole = "Пользователь", string userLogin = "")
        {
            InitializeComponent();
            currentUserRole = userRole;
            currentUserLogin = userLogin;
            LoadStatuses();
        }

        private void LoadStatuses()
        {
            using (var connection = dbconnection.GetConnection())
            {
                try
                {
                    connection.Open();
                    string query = "SELECT statusid, statusname FROM status ORDER BY statusid";
                    using (var cmd = new NpgsqlCommand(query, connection))
                    using (var reader = cmd.ExecuteReader())
                    {
                        List<Status> statuses = new List<Status>();
                        while (reader.Read())
                        {
                            statuses.Add(new Status
                            {
                                StatusId = reader.GetInt32(0),
                                StatusName = reader.GetString(1)
                            });
                        }
                        StatusDataGrid.ItemsSource = statuses;
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка загрузки состояний: {ex.Message}", "Ошибка",
                                   MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            string statusName = StatusNameTextBox.Text.Trim();

            if (string.IsNullOrWhiteSpace(statusName))
            {
                MessageBox.Show("Введите название состояния", "Предупреждение",
                               MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            using (var connection = dbconnection.GetConnection())
            {
                try
                {
                    connection.Open();

                    string checkQuery = "SELECT COUNT(*) FROM status WHERE statusname = @statusname";
                    using (var checkCmd = new NpgsqlCommand(checkQuery, connection))
                    {
                        checkCmd.Parameters.AddWithValue("@statusname", statusName);
                        int count = Convert.ToInt32(checkCmd.ExecuteScalar());
                        if (count > 0)
                        {
                            MessageBox.Show("Состояние с таким названием уже существует", "Предупреждение",
                                           MessageBoxButton.OK, MessageBoxImage.Warning);
                            return;
                        }
                    }

                    string addQuery = "INSERT INTO status (statusname) VALUES (@statusname)";
                    using (var addCmd = new NpgsqlCommand(addQuery, connection))
                    {
                        addCmd.Parameters.AddWithValue("@statusname", statusName);
                        addCmd.ExecuteNonQuery();
                    }

                    MessageBox.Show("Состояние успешно добавлено", "Успех",
                                   MessageBoxButton.OK, MessageBoxImage.Information);
                    ClearFields();
                    LoadStatuses();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка добавления состояния: {ex.Message}", "Ошибка",
                                   MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void UpdateButton_Click(object sender, RoutedEventArgs e)
        {
            if (StatusDataGrid.SelectedItem is Status selectedStatus)
            {
                string statusName = StatusNameTextBox.Text.Trim();

                if (string.IsNullOrWhiteSpace(statusName))
                {
                    MessageBox.Show("Введите название состояния", "Предупреждение",
                                   MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                using (var connection = dbconnection.GetConnection())
                {
                    try
                    {
                        connection.Open();

                        string checkQuery = "SELECT COUNT(*) FROM status WHERE statusname = @statusname AND statusid != @statusid";
                        using (var checkCmd = new NpgsqlCommand(checkQuery, connection))
                        {
                            checkCmd.Parameters.AddWithValue("@statusname", statusName);
                            checkCmd.Parameters.AddWithValue("@statusid", selectedStatus.StatusId);
                            int count = Convert.ToInt32(checkCmd.ExecuteScalar());
                            if (count > 0)
                            {
                                MessageBox.Show("Состояние с таким названием уже существует", "Предупреждение",
                                               MessageBoxButton.OK, MessageBoxImage.Warning);
                                return;
                            }
                        }

                        string updateQuery = "UPDATE status SET statusname = @statusname WHERE statusid = @statusid";
                        using (var updateCmd = new NpgsqlCommand(updateQuery, connection))
                        {
                            updateCmd.Parameters.AddWithValue("@statusname", statusName);
                            updateCmd.Parameters.AddWithValue("@statusid", selectedStatus.StatusId);
                            updateCmd.ExecuteNonQuery();
                        }

                        MessageBox.Show("Состояние успешно обновлено", "Успех",
                                       MessageBoxButton.OK, MessageBoxImage.Information);
                        ClearFields();
                        LoadStatuses();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Ошибка обновления состояния: {ex.Message}", "Ошибка",
                                       MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            else
            {
                MessageBox.Show("Выберите состояние для обновления", "Предупреждение",
                               MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            if (StatusDataGrid.SelectedItem is Status selectedStatus)
            {
                MessageBoxResult result = MessageBox.Show($"Удалить состояние '{selectedStatus.StatusName}'?",
                    "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    using (var connection = dbconnection.GetConnection())
                    {
                        try
                        {
                            connection.Open();

                            // Проверка: используется ли это состояние в других таблицах
                            string checkQuery = "SELECT COUNT(*) FROM equipment WHERE statusid = @statusid";
                            using (var checkCmd = new NpgsqlCommand(checkQuery, connection))
                            {
                                checkCmd.Parameters.AddWithValue("@statusid", selectedStatus.StatusId);
                                int count = Convert.ToInt32(checkCmd.ExecuteScalar());
                                if (count > 0)
                                {
                                    MessageBox.Show("Невозможно удалить состояние, так как оно используется в оборудовании",
                                                   "Предупреждение", MessageBoxButton.OK, MessageBoxImage.Warning);
                                    return;
                                }
                            }

                            string deleteQuery = "DELETE FROM status WHERE statusid = @statusid";
                            using (var deleteCmd = new NpgsqlCommand(deleteQuery, connection))
                            {
                                deleteCmd.Parameters.AddWithValue("@statusid", selectedStatus.StatusId);
                                deleteCmd.ExecuteNonQuery();
                            }

                            MessageBox.Show("Состояние успешно удалено", "Успех",
                                           MessageBoxButton.OK, MessageBoxImage.Information);
                            ClearFields();
                            LoadStatuses();
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show($"Ошибка удаления состояния: {ex.Message}", "Ошибка",
                                           MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                }
            }
            else
            {
                MessageBox.Show("Выберите состояние для удаления", "Предупреждение",
                               MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            LoadStatuses();
            ClearFields();
        }

        private void ExitButton_Click(object sender, RoutedEventArgs e)
        {
            MessageBoxResult result = MessageBox.Show("Вы уверены, что хотите выйти?",
                "Выход", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                if (currentUserRole == "Администратор")
                {
                    AdminWindow adminWindow = new AdminWindow(currentUserLogin);
                    adminWindow.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                    adminWindow.WindowState = WindowState.Maximized;
                    adminWindow.Show();
                }
                else if (currentUserRole == "Технолог")
                {
                    TechnologistWindow technologistWindow = new TechnologistWindow(currentUserLogin);
                    technologistWindow.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                    technologistWindow.WindowState = WindowState.Maximized;
                    technologistWindow.Show();
                }
                else if (currentUserRole == "Мастер смены")
                {
                    MasterWindow masterWindow = new MasterWindow(currentUserLogin);
                    masterWindow.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                    masterWindow.WindowState = WindowState.Maximized;
                    masterWindow.Show();
                }
                else if (currentUserRole == "Оператор")
                {
                    OperatorWindow operatorWindow = new OperatorWindow(currentUserLogin);
                    operatorWindow.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                    operatorWindow.WindowState = WindowState.Maximized;
                    operatorWindow.Show();
                }

                // Закрываем текущее окно
                this.Close();
            }
        }

        private void ClearFields()
        {
            StatusNameTextBox.Text = string.Empty;
        }

        private void StatusDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (StatusDataGrid.SelectedItem is Status selectedStatus)
            {
                StatusNameTextBox.Text = selectedStatus.StatusName;
            }
        }
    }
}