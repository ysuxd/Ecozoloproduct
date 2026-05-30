using Npgsql;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using WpfApp.Database;

namespace WpfApp2
{
    public partial class EquipmentWindow : Window
    {
        private DatabaseConnection dbconnection = new DatabaseConnection();
        private string currentUserRole;
        private string currentUserLogin;

        public class Equipment
        {
            public int EquipmentId { get; set; }
            public string EquipmentName { get; set; }
            public int StatusId { get; set; }
            public string StatusName { get; set; }
            public double Norm { get; set; }
        }

        public class Status
        {
            public int StatusId { get; set; }
            public string StatusName { get; set; }
        }

        public EquipmentWindow(string userRole = "Пользователь", string userLogin = "")
        {
            InitializeComponent();
            currentUserRole = userRole;
            currentUserLogin = userLogin;
            LoadStatuses();
            LoadEquipment();
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
                        StatusComboBox.ItemsSource = statuses;
                        StatusComboBox.DisplayMemberPath = "StatusName";
                        StatusComboBox.SelectedValuePath = "StatusId";

                        if (statuses.Count > 0)
                            StatusComboBox.SelectedIndex = 0;
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка загрузки состояний: {ex.Message}", "Ошибка",
                                   MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void LoadEquipment()
        {
            using (var connection = dbconnection.GetConnection())
            {
                try
                {
                    connection.Open();
                    string query = @"
                        SELECT e.equipmentid, e.equipmentname, e.statusid, s.statusname, e.norm 
                        FROM equipment e
                        LEFT JOIN status s ON e.statusid = s.statusid
                        ORDER BY e.equipmentid";

                    using (var cmd = new NpgsqlCommand(query, connection))
                    using (var reader = cmd.ExecuteReader())
                    {
                        List<Equipment> equipmentList = new List<Equipment>();
                        while (reader.Read())
                        {
                            equipmentList.Add(new Equipment
                            {
                                EquipmentId = reader.GetInt32(0),
                                EquipmentName = reader.GetString(1),
                                StatusId = reader.GetInt32(2),
                                StatusName = reader.IsDBNull(3) ? "Не указано" : reader.GetString(3),
                                Norm = reader.IsDBNull(4) ? 0 : reader.GetDouble(4)
                            });
                        }
                        EquipmentDataGrid.ItemsSource = equipmentList;
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка загрузки оборудования: {ex.Message}", "Ошибка",
                                   MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            string equipmentName = EquipmentNameTextBox.Text.Trim();
            string normText = NormTextBox.Text.Trim();

            if (string.IsNullOrWhiteSpace(equipmentName))
            {
                MessageBox.Show("Введите название оборудования", "Предупреждение",
                               MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (StatusComboBox.SelectedItem == null)
            {
                MessageBox.Show("Выберите состояние оборудования", "Предупреждение",
                               MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            double norm = 0;
            if (!string.IsNullOrWhiteSpace(normText))
            {
                if (!double.TryParse(normText, out norm))
                {
                    MessageBox.Show("Норма времени должна быть числом", "Предупреждение",
                                   MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
            }

            Status selectedStatus = (Status)StatusComboBox.SelectedItem;
            int statusId = selectedStatus.StatusId;

            using (var connection = dbconnection.GetConnection())
            {
                try
                {
                    connection.Open();

                    string checkQuery = "SELECT COUNT(*) FROM equipment WHERE equipmentname = @equipmentname";
                    using (var checkCmd = new NpgsqlCommand(checkQuery, connection))
                    {
                        checkCmd.Parameters.AddWithValue("@equipmentname", equipmentName);
                        int count = Convert.ToInt32(checkCmd.ExecuteScalar());
                        if (count > 0)
                        {
                            MessageBox.Show("Оборудование с таким названием уже существует", "Предупреждение",
                                           MessageBoxButton.OK, MessageBoxImage.Warning);
                            return;
                        }
                    }

                    string addQuery = "INSERT INTO equipment (equipmentname, statusid, norm) VALUES (@equipmentname, @statusid, @norm)";
                    using (var addCmd = new NpgsqlCommand(addQuery, connection))
                    {
                        addCmd.Parameters.AddWithValue("@equipmentname", equipmentName);
                        addCmd.Parameters.AddWithValue("@statusid", statusId);
                        addCmd.Parameters.AddWithValue("@norm", norm);
                        addCmd.ExecuteNonQuery();
                    }

                    MessageBox.Show("Оборудование успешно добавлено", "Успех",
                                   MessageBoxButton.OK, MessageBoxImage.Information);
                    ClearFields();
                    LoadEquipment();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка добавления оборудования: {ex.Message}", "Ошибка",
                                   MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void UpdateButton_Click(object sender, RoutedEventArgs e)
        {
            if (EquipmentDataGrid.SelectedItem is Equipment selectedEquipment)
            {
                string equipmentName = EquipmentNameTextBox.Text.Trim();
                string normText = NormTextBox.Text.Trim();

                if (string.IsNullOrWhiteSpace(equipmentName))
                {
                    MessageBox.Show("Введите название оборудования", "Предупреждение",
                                   MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (StatusComboBox.SelectedItem == null)
                {
                    MessageBox.Show("Выберите состояние оборудования", "Предупреждение",
                                   MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                double norm = 0;
                if (!string.IsNullOrWhiteSpace(normText))
                {
                    if (!double.TryParse(normText, out norm))
                    {
                        MessageBox.Show("Норма времени должна быть числом", "Предупреждение",
                                       MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }
                }

                Status selectedStatus = (Status)StatusComboBox.SelectedItem;
                int statusId = selectedStatus.StatusId;

                using (var connection = dbconnection.GetConnection())
                {
                    try
                    {
                        connection.Open();

                        string checkQuery = "SELECT COUNT(*) FROM equipment WHERE equipmentname = @equipmentname AND equipmentid != @equipmentid";
                        using (var checkCmd = new NpgsqlCommand(checkQuery, connection))
                        {
                            checkCmd.Parameters.AddWithValue("@equipmentname", equipmentName);
                            checkCmd.Parameters.AddWithValue("@equipmentid", selectedEquipment.EquipmentId);
                            int count = Convert.ToInt32(checkCmd.ExecuteScalar());
                            if (count > 0)
                            {
                                MessageBox.Show("Оборудование с таким названием уже существует", "Предупреждение",
                                               MessageBoxButton.OK, MessageBoxImage.Warning);
                                return;
                            }
                        }

                        string updateQuery = "UPDATE equipment SET equipmentname = @equipmentname, statusid = @statusid, norm = @norm WHERE equipmentid = @equipmentid";
                        using (var updateCmd = new NpgsqlCommand(updateQuery, connection))
                        {
                            updateCmd.Parameters.AddWithValue("@equipmentname", equipmentName);
                            updateCmd.Parameters.AddWithValue("@statusid", statusId);
                            updateCmd.Parameters.AddWithValue("@norm", norm);
                            updateCmd.Parameters.AddWithValue("@equipmentid", selectedEquipment.EquipmentId);
                            updateCmd.ExecuteNonQuery();
                        }

                        MessageBox.Show("Оборудование успешно обновлено", "Успех",
                                       MessageBoxButton.OK, MessageBoxImage.Information);
                        ClearFields();
                        LoadEquipment();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Ошибка обновления оборудования: {ex.Message}", "Ошибка",
                                       MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            else
            {
                MessageBox.Show("Выберите оборудование для обновления", "Предупреждение",
                               MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            if (EquipmentDataGrid.SelectedItem is Equipment selectedEquipment)
            {
                MessageBoxResult result = MessageBox.Show($"Удалить оборудование '{selectedEquipment.EquipmentName}'?",
                    "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    using (var connection = dbconnection.GetConnection())
                    {
                        try
                        {
                            connection.Open();

                            string checkQuery = "SELECT COUNT(*) FROM shiftdetails WHERE equipmentid = @equipmentid";
                            using (var checkCmd = new NpgsqlCommand(checkQuery, connection))
                            {
                                checkCmd.Parameters.AddWithValue("@equipmentid", selectedEquipment.EquipmentId);
                                int count = Convert.ToInt32(checkCmd.ExecuteScalar());
                                if (count > 0)
                                {
                                    MessageBox.Show("Невозможно удалить оборудование, так как оно используется в деталях смен",
                                                   "Предупреждение", MessageBoxButton.OK, MessageBoxImage.Warning);
                                    return;
                                }
                            }

                            string deleteQuery = "DELETE FROM equipment WHERE equipmentid = @equipmentid";
                            using (var deleteCmd = new NpgsqlCommand(deleteQuery, connection))
                            {
                                deleteCmd.Parameters.AddWithValue("@equipmentid", selectedEquipment.EquipmentId);
                                deleteCmd.ExecuteNonQuery();
                            }

                            MessageBox.Show("Оборудование успешно удалено", "Успех",
                                           MessageBoxButton.OK, MessageBoxImage.Information);
                            ClearFields();
                            LoadEquipment();
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show($"Ошибка удаления оборудования: {ex.Message}", "Ошибка",
                                           MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                }
            }
            else
            {
                MessageBox.Show("Выберите оборудование для удаления", "Предупреждение",
                               MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            LoadStatuses();
            LoadEquipment();
            ClearFields();
        }

        private void ExitButton_Click(object sender, RoutedEventArgs e)
        {
            MessageBoxResult result = MessageBox.Show("Вы уверены, что хотите выйти?",
                "Выход", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                // Открываем окно администратора или пользователя
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
            EquipmentNameTextBox.Text = string.Empty;
            NormTextBox.Text = string.Empty;
            if (StatusComboBox.Items.Count > 0)
                StatusComboBox.SelectedIndex = 0;
        }

        private void EquipmentDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (EquipmentDataGrid.SelectedItem is Equipment selectedEquipment)
            {
                EquipmentNameTextBox.Text = selectedEquipment.EquipmentName;
                NormTextBox.Text = selectedEquipment.Norm.ToString();

                for (int i = 0; i < StatusComboBox.Items.Count; i++)
                {
                    var status = (Status)StatusComboBox.Items[i];
                    if (status.StatusId == selectedEquipment.StatusId)
                    {
                        StatusComboBox.SelectedIndex = i;
                        break;
                    }
                }
            }
        }
    }
}