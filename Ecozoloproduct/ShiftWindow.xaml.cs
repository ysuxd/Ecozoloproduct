using Npgsql;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using WpfApp.Database;

namespace WpfApp2
{
    public partial class ShiftWindow : Window
    {
        private DatabaseConnection dbconnection = new DatabaseConnection();
        private string currentUserRole;
        private string currentUserLogin;

        public class Shift
        {
            public int ShiftId { get; set; }
            public DateTime ShiftDate { get; set; }
            public TimeSpan ShiftStartTime { get; set; }
            public TimeSpan ShiftEndTime { get; set; }
            public double Duration { get; set; }
            public string ShiftStartTimeDisplay
            {
                get { return ShiftStartTime.ToString(@"hh\:mm"); }
            }
            public string ShiftEndTimeDisplay
            {
                get { return ShiftEndTime.ToString(@"hh\:mm"); }
            }
        }

        public ShiftWindow(string userRole = "Пользователь", string userLogin = "")
        {
            InitializeComponent();
            currentUserRole = userRole;
            currentUserLogin = userLogin;

            SetDefaultShiftTimes();
            LoadShifts();
        }

        private void SetDefaultShiftTimes()
        {
            DateTime now = DateTime.Now;
            TimeSpan currentTime = now.TimeOfDay;

            TimeSpan firstShiftStart = new TimeSpan(8, 0, 0);
            TimeSpan firstShiftEnd = new TimeSpan(20, 0, 0);

            ShiftDatePicker.SelectedDate = DateTime.Today;

            if (currentTime >= firstShiftStart && currentTime < firstShiftEnd)
            {
                StartTimeTextBox.Text = "08:00";
                EndTimeTextBox.Text = "20:00";
            }
            else
            {
                StartTimeTextBox.Text = "20:00";
                EndTimeTextBox.Text = "08:00";

                if (currentTime < firstShiftStart)
                {
                    ShiftDatePicker.SelectedDate = DateTime.Today.AddDays(-1);
                }
            }
        }

        private void LoadShifts()
        {
            using (var connection = dbconnection.GetConnection())
            {
                try
                {
                    connection.Open();
                    string query = "SELECT shiftid, shiftdate, shiftstarttime, shiftendtime FROM shift ORDER BY shiftdate DESC, shiftstarttime";

                    using (var cmd = new NpgsqlCommand(query, connection))
                    using (var reader = cmd.ExecuteReader())
                    {
                        List<Shift> shifts = new List<Shift>();
                        while (reader.Read())
                        {
                            TimeSpan startTime = reader.GetTimeSpan(2);
                            TimeSpan endTime = reader.GetTimeSpan(3);
                            double duration = (endTime - startTime).TotalHours;
                            if (duration < 0)
                            {
                                duration += 24;
                            }

                            var shift = new Shift
                            {
                                ShiftId = reader.GetInt32(0),
                                ShiftDate = reader.GetDateTime(1),
                                ShiftStartTime = startTime,
                                ShiftEndTime = endTime,
                                Duration = Math.Round(duration, 1)
                            };
                            shifts.Add(shift);
                        }
                        ShiftDataGrid.ItemsSource = shifts;
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка загрузки смен: {ex.Message}", "Ошибка",
                                   MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void ViewDetailsButton_Click(object sender, RoutedEventArgs e)
        {
            if (ShiftDataGrid.SelectedItem is Shift selectedShift)
            {
                ShiftDetailsWindow detailsWindow = new ShiftDetailsWindow(currentUserRole, currentUserLogin, selectedShift.ShiftId);
                detailsWindow.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                detailsWindow.WindowState = WindowState.Maximized;
                detailsWindow.Show();
            }
            else
            {
                MessageBox.Show("Выберите смену для просмотра деталей", "Предупреждение",
                               MessageBoxButton.OK, MessageBoxImage.Warning);
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
            DateTime? selectedDate = ShiftDatePicker.SelectedDate;

            if (selectedDate == null)
            {
                MessageBox.Show("Выберите дату смены", "Предупреждение",
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

            bool isNightShift = startTime > endTime;

            if (!isNightShift && startTime >= endTime)
            {
                MessageBox.Show("Время начала должно быть меньше времени окончания", "Предупреждение",
                               MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            using (var connection = dbconnection.GetConnection())
            {
                try
                {
                    connection.Open();

                    string checkQuery = @"SELECT COUNT(*) FROM shift 
                                        WHERE shiftdate = @shiftdate 
                                        AND shiftstarttime = @shiftstarttime";
                    using (var checkCmd = new NpgsqlCommand(checkQuery, connection))
                    {
                        checkCmd.Parameters.AddWithValue("@shiftdate", selectedDate.Value);
                        checkCmd.Parameters.AddWithValue("@shiftstarttime", startTime);
                        int count = Convert.ToInt32(checkCmd.ExecuteScalar());
                        if (count > 0)
                        {
                            MessageBox.Show("Смена с такой датой и временем начала уже существует", "Предупреждение",
                                           MessageBoxButton.OK, MessageBoxImage.Warning);
                            return;
                        }
                    }

                    string addQuery = "INSERT INTO shift (shiftdate, shiftstarttime, shiftendtime) VALUES (@shiftdate, @shiftstarttime, @shiftendtime)";
                    using (var addCmd = new NpgsqlCommand(addQuery, connection))
                    {
                        addCmd.Parameters.AddWithValue("@shiftdate", selectedDate.Value);
                        addCmd.Parameters.AddWithValue("@shiftstarttime", startTime);
                        addCmd.Parameters.AddWithValue("@shiftendtime", endTime);
                        addCmd.ExecuteNonQuery();
                    }

                    MessageBox.Show("Смена успешно добавлена", "Успех",
                                   MessageBoxButton.OK, MessageBoxImage.Information);
                    ClearFields();
                    LoadShifts();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка добавления смены: {ex.Message}", "Ошибка",
                                   MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void UpdateButton_Click(object sender, RoutedEventArgs e)
        {
            if (ShiftDataGrid.SelectedItem is Shift selectedShift)
            {
                DateTime? selectedDate = ShiftDatePicker.SelectedDate;

                if (selectedDate == null)
                {
                    MessageBox.Show("Выберите дату смены", "Предупреждение",
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

                bool isNightShift = startTime > endTime;

                if (!isNightShift && startTime >= endTime)
                {
                    MessageBox.Show("Время начала должно быть меньше времени окончания", "Предупреждение",
                                   MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                using (var connection = dbconnection.GetConnection())
                {
                    try
                    {
                        connection.Open();

                        string checkQuery = @"SELECT COUNT(*) FROM shift 
                                            WHERE shiftdate = @shiftdate 
                                            AND shiftstarttime = @shiftstarttime 
                                            AND shiftid != @shiftid";
                        using (var checkCmd = new NpgsqlCommand(checkQuery, connection))
                        {
                            checkCmd.Parameters.AddWithValue("@shiftdate", selectedDate.Value);
                            checkCmd.Parameters.AddWithValue("@shiftstarttime", startTime);
                            checkCmd.Parameters.AddWithValue("@shiftid", selectedShift.ShiftId);
                            int count = Convert.ToInt32(checkCmd.ExecuteScalar());
                            if (count > 0)
                            {
                                MessageBox.Show("Смена с такой датой и временем начала уже существует", "Предупреждение",
                                               MessageBoxButton.OK, MessageBoxImage.Warning);
                                return;
                            }
                        }

                        string updateQuery = "UPDATE shift SET shiftdate = @shiftdate, shiftstarttime = @shiftstarttime, shiftendtime = @shiftendtime WHERE shiftid = @shiftid";
                        using (var updateCmd = new NpgsqlCommand(updateQuery, connection))
                        {
                            updateCmd.Parameters.AddWithValue("@shiftdate", selectedDate.Value);
                            updateCmd.Parameters.AddWithValue("@shiftstarttime", startTime);
                            updateCmd.Parameters.AddWithValue("@shiftendtime", endTime);
                            updateCmd.Parameters.AddWithValue("@shiftid", selectedShift.ShiftId);
                            updateCmd.ExecuteNonQuery();
                        }

                        MessageBox.Show("Смена успешно обновлена", "Успех",
                                       MessageBoxButton.OK, MessageBoxImage.Information);
                        ClearFields();
                        LoadShifts();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Ошибка обновления смены: {ex.Message}", "Ошибка",
                                       MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            else
            {
                MessageBox.Show("Выберите смену для обновления", "Предупреждение",
                               MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            if (ShiftDataGrid.SelectedItem is Shift selectedShift)
            {
                MessageBoxResult result = MessageBox.Show($"Удалить смену за {selectedShift.ShiftDate.ToString("dd.MM.yyyy")}?",
                    "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    using (var connection = dbconnection.GetConnection())
                    {
                        try
                        {
                            connection.Open();

                            string checkQuery = "SELECT COUNT(*) FROM shiftdetails WHERE shiftid = @shiftid";
                            using (var checkCmd = new NpgsqlCommand(checkQuery, connection))
                            {
                                checkCmd.Parameters.AddWithValue("@shiftid", selectedShift.ShiftId);
                                int count = Convert.ToInt32(checkCmd.ExecuteScalar());
                                if (count > 0)
                                {
                                    MessageBox.Show("Невозможно удалить смену, так как она используется в деталях смен",
                                                   "Предупреждение", MessageBoxButton.OK, MessageBoxImage.Warning);
                                    return;
                                }
                            }

                            string deleteQuery = "DELETE FROM shift WHERE shiftid = @shiftid";
                            using (var deleteCmd = new NpgsqlCommand(deleteQuery, connection))
                            {
                                deleteCmd.Parameters.AddWithValue("@shiftid", selectedShift.ShiftId);
                                deleteCmd.ExecuteNonQuery();
                            }

                            MessageBox.Show("Смена успешно удалена", "Успех",
                                           MessageBoxButton.OK, MessageBoxImage.Information);
                            ClearFields();
                            LoadShifts();
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show($"Ошибка удаления смены: {ex.Message}", "Ошибка",
                                           MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                }
            }
            else
            {
                MessageBox.Show("Выберите смену для удаления", "Предупреждение",
                               MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            LoadShifts();
            SetDefaultShiftTimes();
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
            SetDefaultShiftTimes();
        }

        private void ShiftDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ShiftDataGrid.SelectedItem is Shift selectedShift)
            {
                ShiftDatePicker.SelectedDate = selectedShift.ShiftDate;
                StartTimeTextBox.Text = selectedShift.ShiftStartTime.ToString(@"hh\:mm");
                EndTimeTextBox.Text = selectedShift.ShiftEndTime.ToString(@"hh\:mm");
            }
        }
    }
}