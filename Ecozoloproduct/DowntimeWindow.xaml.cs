using Npgsql;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using WpfApp.Database;

namespace WpfApp2
{
    public partial class DowntimeRecordWindow : Window
    {
        private DatabaseConnection dbconnection = new DatabaseConnection();
        private string currentUserRole;
        private string currentUserLogin;

        public class DowntimeRecord
        {
            public int DowntimeId { get; set; }
            public int EquipmentId { get; set; }
            public string EquipmentName { get; set; }
            public DateTime DowntimeDate { get; set; }
            public TimeSpan DowntimeStartTime { get; set; }
            public TimeSpan DowntimeEndTime { get; set; }
            public TimeSpan DowntimeInterval { get; set; }
            public string DowntimeIntervalDisplay { get; set; }
            public string DowntimeReason { get; set; }
            public string DowntimeStartTimeDisplay => DowntimeStartTime.ToString(@"hh\:mm");
            public string DowntimeEndTimeDisplay => DowntimeEndTime.ToString(@"hh\:mm");
        }

        public class SimpleItem
        {
            public int Id { get; set; }
            public string Name { get; set; }
        }

        public DowntimeRecordWindow(string userRole = "Пользователь", string userLogin = "")
        {
            InitializeComponent();
            currentUserRole = userRole;
            currentUserLogin = userLogin;

            DowntimeDatePicker.SelectedDate = DateTime.Today;
            StartTimeTextBox.Text = "08:00";
            EndTimeTextBox.Text = "09:00";

            LoadEquipment();
            LoadDowntimeRecords();
        }

        private void LoadEquipment()
        {
            using (var connection = dbconnection.GetConnection())
            {
                try
                {
                    connection.Open();
                    string query = "SELECT equipmentid, equipmentname FROM equipment ORDER BY equipmentname";
                    using (var cmd = new NpgsqlCommand(query, connection))
                    using (var reader = cmd.ExecuteReader())
                    {
                        var items = new List<SimpleItem>();
                        while (reader.Read())
                        {
                            items.Add(new SimpleItem
                            {
                                Id = reader.GetInt32(0),
                                Name = reader.GetString(1)
                            });
                        }
                        EquipmentComboBox.ItemsSource = items;
                        if (items.Count > 0)
                            EquipmentComboBox.SelectedIndex = 0;
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка загрузки оборудования: {ex.Message}", "Ошибка",
                                   MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void LoadDowntimeRecords()
        {
            using (var connection = dbconnection.GetConnection())
            {
                try
                {
                    connection.Open();
                    string query = @"
                        SELECT 
                            d.downtimeid,
                            d.equipmentid,
                            e.equipmentname,
                            d.downtimedate,
                            d.downtimestarttime,
                            d.downtimeendtime,
                            d.downtimereason
                        FROM downtime d
                        LEFT JOIN equipment e ON d.equipmentid = e.equipmentid
                        ORDER BY d.downtimedate DESC, d.downtimestarttime DESC";

                    using (var cmd = new NpgsqlCommand(query, connection))
                    using (var reader = cmd.ExecuteReader())
                    {
                        List<DowntimeRecord> records = new List<DowntimeRecord>();
                        while (reader.Read())
                        {
                            TimeSpan startTime = reader.GetTimeSpan(4);
                            TimeSpan endTime = reader.GetTimeSpan(5);
                            TimeSpan interval = endTime - startTime;
                            if (interval.TotalHours < 0)
                                interval = interval.Add(TimeSpan.FromHours(24));

                            records.Add(new DowntimeRecord
                            {
                                DowntimeId = reader.GetInt32(0),
                                EquipmentId = reader.GetInt32(1),
                                EquipmentName = reader.GetString(2),
                                DowntimeDate = reader.GetDateTime(3),
                                DowntimeStartTime = startTime,
                                DowntimeEndTime = endTime,
                                DowntimeInterval = interval,
                                DowntimeIntervalDisplay = $"{interval.Hours:00}:{interval.Minutes:00}:{interval.Seconds:00}",
                                DowntimeReason = reader.IsDBNull(6) ? "" : reader.GetString(6)
                            });
                        }
                        DowntimeDataGrid.ItemsSource = records;
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка загрузки записей простоя: {ex.Message}", "Ошибка",
                                   MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private bool ValidateTime(string time, out TimeSpan result)
        {
            result = TimeSpan.Zero;
            if (string.IsNullOrWhiteSpace(time))
                return false;

            if (TimeSpan.TryParse(time, out result))
            {
                return true;
            }
            return false;
        }

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            if (EquipmentComboBox.SelectedItem == null)
            {
                MessageBox.Show("Выберите оборудование", "Предупреждение",
                               MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            DateTime? selectedDate = DowntimeDatePicker.SelectedDate;
            if (selectedDate == null)
            {
                MessageBox.Show("Выберите дату простоя", "Предупреждение",
                               MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            TimeSpan startTime, endTime;
            if (!ValidateTime(StartTimeTextBox.Text, out startTime))
            {
                MessageBox.Show("Введите корректное время начала (формат: HH:MM)", "Предупреждение",
                               MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!ValidateTime(EndTimeTextBox.Text, out endTime))
            {
                MessageBox.Show("Введите корректное время окончания (формат: HH:MM)", "Предупреждение",
                               MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (startTime >= endTime)
            {
                MessageBox.Show("Время начала должно быть меньше времени окончания", "Предупреждение",
                               MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            int equipmentId = ((SimpleItem)EquipmentComboBox.SelectedItem).Id;
            string reason = DowntimeReasonTextBox.Text.Trim();

            using (var connection = dbconnection.GetConnection())
            {
                try
                {
                    connection.Open();

                    string query = @"INSERT INTO downtime 
                                   (equipmentid, downtimedate, downtimestarttime, downtimeendtime, downtimereason) 
                                   VALUES (@equipmentid, @downtimedate, @downtimestarttime, @downtimeendtime, @downtimereason)";

                    using (var cmd = new NpgsqlCommand(query, connection))
                    {
                        cmd.Parameters.AddWithValue("@equipmentid", equipmentId);
                        cmd.Parameters.AddWithValue("@downtimedate", selectedDate.Value);
                        cmd.Parameters.AddWithValue("@downtimestarttime", startTime);
                        cmd.Parameters.AddWithValue("@downtimeendtime", endTime);
                        cmd.Parameters.AddWithValue("@downtimereason", string.IsNullOrWhiteSpace(reason) ? DBNull.Value : (object)reason);
                        cmd.ExecuteNonQuery();
                    }

                    MessageBox.Show("Запись о простое успешно добавлена", "Успех",
                                   MessageBoxButton.OK, MessageBoxImage.Information);
                    ClearFields();
                    LoadDowntimeRecords();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка добавления: {ex.Message}", "Ошибка",
                                   MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void UpdateButton_Click(object sender, RoutedEventArgs e)
        {
            if (DowntimeDataGrid.SelectedItem is DowntimeRecord selected)
            {
                if (EquipmentComboBox.SelectedItem == null)
                {
                    MessageBox.Show("Выберите оборудование", "Предупреждение",
                                   MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                DateTime? selectedDate = DowntimeDatePicker.SelectedDate;
                if (selectedDate == null)
                {
                    MessageBox.Show("Выберите дату простоя", "Предупреждение",
                                   MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                TimeSpan startTime, endTime;
                if (!ValidateTime(StartTimeTextBox.Text, out startTime))
                {
                    MessageBox.Show("Введите корректное время начала", "Предупреждение",
                                   MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (!ValidateTime(EndTimeTextBox.Text, out endTime))
                {
                    MessageBox.Show("Введите корректное время окончания", "Предупреждение",
                                   MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (startTime >= endTime)
                {
                    MessageBox.Show("Время начала должно быть меньше времени окончания", "Предупреждение",
                                   MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                int equipmentId = ((SimpleItem)EquipmentComboBox.SelectedItem).Id;
                string reason = DowntimeReasonTextBox.Text.Trim();

                using (var connection = dbconnection.GetConnection())
                {
                    try
                    {
                        connection.Open();

                        string query = @"UPDATE downtime 
                                       SET equipmentid = @equipmentid,
                                           downtimedate = @downtimedate,
                                           downtimestarttime = @downtimestarttime,
                                           downtimeendtime = @downtimeendtime,
                                           downtimereason = @downtimereason
                                       WHERE downtimeid = @downtimeid";

                        using (var cmd = new NpgsqlCommand(query, connection))
                        {
                            cmd.Parameters.AddWithValue("@equipmentid", equipmentId);
                            cmd.Parameters.AddWithValue("@downtimedate", selectedDate.Value);
                            cmd.Parameters.AddWithValue("@downtimestarttime", startTime);
                            cmd.Parameters.AddWithValue("@downtimeendtime", endTime);
                            cmd.Parameters.AddWithValue("@downtimereason", string.IsNullOrWhiteSpace(reason) ? DBNull.Value : (object)reason);
                            cmd.Parameters.AddWithValue("@downtimeid", selected.DowntimeId);
                            cmd.ExecuteNonQuery();
                        }

                        MessageBox.Show("Запись о простое успешно обновлена", "Успех",
                                       MessageBoxButton.OK, MessageBoxImage.Information);
                        ClearFields();
                        LoadDowntimeRecords();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Ошибка обновления: {ex.Message}", "Ошибка",
                                       MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            else
            {
                MessageBox.Show("Выберите запись для обновления", "Предупреждение",
                               MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            if (DowntimeDataGrid.SelectedItem is DowntimeRecord selected)
            {
                MessageBoxResult result = MessageBox.Show($"Удалить запись о простое от {selected.DowntimeDate:dd.MM.yyyy}?",
                    "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    using (var connection = dbconnection.GetConnection())
                    {
                        try
                        {
                            connection.Open();
                            string query = "DELETE FROM downtime WHERE downtimeid = @downtimeid";
                            using (var cmd = new NpgsqlCommand(query, connection))
                            {
                                cmd.Parameters.AddWithValue("@downtimeid", selected.DowntimeId);
                                cmd.ExecuteNonQuery();
                            }
                            MessageBox.Show("Запись удалена", "Успех",
                                           MessageBoxButton.OK, MessageBoxImage.Information);
                            ClearFields();
                            LoadDowntimeRecords();
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show($"Ошибка удаления: {ex.Message}", "Ошибка",
                                           MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                }
            }
            else
            {
                MessageBox.Show("Выберите запись для удаления", "Предупреждение",
                               MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            LoadEquipment();
            LoadDowntimeRecords();
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
                this.Close();
            }
        }

        private void ClearFields()
        {
            DowntimeDatePicker.SelectedDate = DateTime.Today;
            StartTimeTextBox.Text = "08:00";
            EndTimeTextBox.Text = "09:00";
            DowntimeReasonTextBox.Text = string.Empty;
            if (EquipmentComboBox.Items.Count > 0) EquipmentComboBox.SelectedIndex = 0;
        }

        private void DowntimeDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DowntimeDataGrid.SelectedItem is DowntimeRecord selected)
            {
                DowntimeDatePicker.SelectedDate = selected.DowntimeDate;
                StartTimeTextBox.Text = selected.DowntimeStartTime.ToString(@"hh\:mm");
                EndTimeTextBox.Text = selected.DowntimeEndTime.ToString(@"hh\:mm");
                DowntimeReasonTextBox.Text = selected.DowntimeReason;

                for (int i = 0; i < EquipmentComboBox.Items.Count; i++)
                {
                    var item = (SimpleItem)EquipmentComboBox.Items[i];
                    if (item.Id == selected.EquipmentId)
                    {
                        EquipmentComboBox.SelectedIndex = i;
                        break;
                    }
                }
            }
        }
    }
}