using Npgsql;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using WpfApp.Database;

namespace WpfApp2
{
    public partial class ShiftDetailsWindow : Window
    {
        private DatabaseConnection dbconnection = new DatabaseConnection();
        private string currentUserRole;
        private string currentUserLogin;
        private int? currentShiftId = null;
        private string currentShiftInfo = "";
        private TimeSpan shiftDuration = TimeSpan.FromHours(12);
        private DateTime? currentShiftDate = null; // Добавлено для хранения даты текущей смены

        public class ShiftDetails
        {
            public int ShiftDetailsId { get; set; }
            public int EquipmentId { get; set; }
            public string EquipmentName { get; set; }
            public int ShiftId { get; set; }
            public TimeSpan EquipmentWorkTime { get; set; }
            public string EquipmentWorkTimeDisplay { get; set; }
            public double ProductionQuantity { get; set; }
            public double QualityProductionQuantity { get; set; }
        }

        public class SimpleItem
        {
            public int Id { get; set; }
            public string Name { get; set; }
        }

        public ShiftDetailsWindow(string userRole = "Пользователь", string userLogin = "")
        {
            InitializeComponent();
            currentUserRole = userRole;
            currentUserLogin = userLogin;
            currentShiftId = null;

            LoadEquipment();
            LoadShiftDetails();

            ShiftInfoTextBlock.Text = "Все смены";
            TitleTextBlock.Text = "⚙️ ДЕТАЛИ РАБОТЫ ОБОРУДОВАНИЯ ⚙️";
        }

        public ShiftDetailsWindow(string userRole = "Пользователь", string userLogin = "", int shiftId = 0)
        {
            InitializeComponent();
            currentUserRole = userRole;
            currentUserLogin = userLogin;
            currentShiftId = shiftId;

            LoadEquipment();
            LoadShiftInfo();
            LoadShiftDetails();
        }

        private void LoadShiftInfo()
        {
            using (var connection = dbconnection.GetConnection())
            {
                try
                {
                    connection.Open();
                    string query = "SELECT shiftid, shiftdate, shiftstarttime, shiftendtime FROM shift WHERE shiftid = @shiftid";
                    using (var cmd = new NpgsqlCommand(query, connection))
                    {
                        cmd.Parameters.AddWithValue("@shiftid", currentShiftId);
                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                int id = reader.GetInt32(0);
                                DateTime date = reader.GetDateTime(1);
                                currentShiftDate = date; // Сохраняем дату смены
                                TimeSpan startTime = reader.GetTimeSpan(2);
                                TimeSpan endTime = reader.GetTimeSpan(3);

                                shiftDuration = endTime - startTime;
                                if (shiftDuration.TotalHours < 0)
                                    shiftDuration = shiftDuration.Add(TimeSpan.FromHours(24));

                                currentShiftInfo = $"{date:dd.MM.yyyy} {startTime:hh\\:mm} - {endTime:hh\\:mm} (Длительность: {shiftDuration.TotalHours} ч)";
                                ShiftInfoTextBlock.Text = currentShiftInfo;
                                TitleTextBlock.Text = $"⚙️ ДЕТАЛИ СМЕНЫ №{id} от {date:dd.MM.yyyy} ⚙️";
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка загрузки информации о смене: {ex.Message}", "Ошибка",
                                   MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        // Метод для открытия окна эффективности
        private void EfficiencyButton_Click(object sender, RoutedEventArgs e)
        {
            if (currentShiftId.HasValue && currentShiftId.Value > 0 && currentShiftDate.HasValue)
            {
                // Открываем окно эффективности с фильтром по дате текущей смены
                EfficiencyWindow efficiencyWindow = new EfficiencyWindow(currentUserRole, currentUserLogin);

                // Устанавливаем даты в окне эффективности (только одна дата - день смены)
                // Для этого нужно добавить метод в EfficiencyWindow для установки дат
                efficiencyWindow.SetDateRange(currentShiftDate.Value, currentShiftDate.Value);
                efficiencyWindow.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                efficiencyWindow.WindowState = WindowState.Maximized;
                efficiencyWindow.Show();
                this.Close();
            }
            else
            {
                // Если не в режиме просмотра конкретной смены, открываем окно эффективности без фильтра
                EfficiencyWindow efficiencyWindow = new EfficiencyWindow(currentUserRole, currentUserLogin);
                efficiencyWindow.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                efficiencyWindow.WindowState = WindowState.Maximized;
                efficiencyWindow.Show();
                this.Close();
            }
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

        private void LoadShiftDetails()
        {
            using (var connection = dbconnection.GetConnection())
            {
                try
                {
                    connection.Open();

                    string query;
                    if (currentShiftId.HasValue && currentShiftId.Value > 0)
                    {
                        query = @"
                            SELECT 
                                sd.shiftdetailsid, 
                                sd.equipmentid, 
                                e.equipmentname,
                                sd.shiftid,
                                sd.equipmentworktime,
                                sd.productionquantity,
                                sd.qualityproductionquantity
                            FROM shiftdetails sd
                            LEFT JOIN equipment e ON sd.equipmentid = e.equipmentid
                            WHERE sd.shiftid = @shiftid
                            ORDER BY sd.shiftdetailsid DESC";
                    }
                    else
                    {
                        query = @"
                            SELECT 
                                sd.shiftdetailsid, 
                                sd.equipmentid, 
                                e.equipmentname,
                                sd.shiftid,
                                sd.equipmentworktime,
                                sd.productionquantity,
                                sd.qualityproductionquantity
                            FROM shiftdetails sd
                            LEFT JOIN equipment e ON sd.equipmentid = e.equipmentid
                            ORDER BY sd.shiftdetailsid DESC";
                    }

                    using (var cmd = new NpgsqlCommand(query, connection))
                    {
                        if (currentShiftId.HasValue && currentShiftId.Value > 0)
                        {
                            cmd.Parameters.AddWithValue("@shiftid", currentShiftId.Value);
                        }

                        using (var reader = cmd.ExecuteReader())
                        {
                            List<ShiftDetails> items = new List<ShiftDetails>();
                            while (reader.Read())
                            {
                                TimeSpan workTime = reader.GetTimeSpan(4);
                                items.Add(new ShiftDetails
                                {
                                    ShiftDetailsId = reader.GetInt32(0),
                                    EquipmentId = reader.GetInt32(1),
                                    EquipmentName = reader.GetString(2),
                                    ShiftId = reader.GetInt32(3),
                                    EquipmentWorkTime = workTime,
                                    EquipmentWorkTimeDisplay = workTime.ToString(@"hh\:mm\:ss"),
                                    ProductionQuantity = reader.GetDouble(5),
                                    QualityProductionQuantity = reader.GetDouble(6)
                                });
                            }
                            ShiftDetailsDataGrid.ItemsSource = items;
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка загрузки данных: {ex.Message}", "Ошибка",
                                   MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private TimeSpan GetTotalDowntimeForEquipment(int equipmentId, DateTime shiftDate)
        {
            using (var connection = dbconnection.GetConnection())
            {
                try
                {
                    connection.Open();
                    string query = @"
                        SELECT SUM(downtimeendtime - downtimestarttime) as total_downtime
                        FROM downtime 
                        WHERE equipmentid = @equipmentid 
                        AND downtimedate = @downtimedate";

                    using (var cmd = new NpgsqlCommand(query, connection))
                    {
                        cmd.Parameters.AddWithValue("@equipmentid", equipmentId);
                        cmd.Parameters.AddWithValue("@downtimedate", shiftDate);
                        var result = cmd.ExecuteScalar();

                        if (result != DBNull.Value)
                        {
                            TimeSpan total = (TimeSpan)result;
                            if (total.TotalHours < 0)
                                total = total.Add(TimeSpan.FromHours(24));
                            return total;
                        }
                        return TimeSpan.Zero;
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Ошибка получения времени простоев: {ex.Message}");
                    return TimeSpan.Zero;
                }
            }
        }

        private DateTime? GetShiftDate(int shiftId)
        {
            using (var connection = dbconnection.GetConnection())
            {
                try
                {
                    connection.Open();
                    string query = "SELECT shiftdate FROM shift WHERE shiftid = @shiftid";
                    using (var cmd = new NpgsqlCommand(query, connection))
                    {
                        cmd.Parameters.AddWithValue("@shiftid", shiftId);
                        var result = cmd.ExecuteScalar();
                        if (result != null)
                            return Convert.ToDateTime(result);
                        return null;
                    }
                }
                catch
                {
                    return null;
                }
            }
        }

        private int? ShowShiftIdDialog()
        {
            var dialog = new Window
            {
                Title = "Ввод ID смены",
                Width = 350,
                Height = 150,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = this,
                Background = System.Windows.Media.Brushes.White,
                ResizeMode = ResizeMode.NoResize
            };

            var grid = new Grid { Margin = new Thickness(15) };
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            var label = new TextBlock
            {
                Text = "Введите ID смены:",
                Margin = new Thickness(0, 0, 0, 10),
                FontWeight = FontWeights.SemiBold
            };
            Grid.SetRow(label, 0);
            grid.Children.Add(label);

            var textBox = new TextBox { Height = 35, Margin = new Thickness(0, 0, 0, 15) };
            Grid.SetRow(textBox, 1);
            grid.Children.Add(textBox);

            var buttonPanel = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Center };
            var okButton = new Button { Content = "OK", Width = 80, Height = 30, Margin = new Thickness(5), Background = System.Windows.Media.Brushes.LightGreen };
            var cancelButton = new Button { Content = "Отмена", Width = 80, Height = 30, Margin = new Thickness(5) };

            int? result = null;
            okButton.Click += (s, e) =>
            {
                if (int.TryParse(textBox.Text, out int id) && id > 0)
                    result = id;
                dialog.Close();
            };
            cancelButton.Click += (s, e) => dialog.Close();

            buttonPanel.Children.Add(okButton);
            buttonPanel.Children.Add(cancelButton);
            Grid.SetRow(buttonPanel, 2);
            grid.Children.Add(buttonPanel);

            dialog.Content = grid;
            dialog.ShowDialog();

            return result;
        }

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            int? targetShiftId = currentShiftId;

            if (EquipmentComboBox.SelectedItem == null)
            {
                MessageBox.Show("Выберите оборудование", "Предупреждение",
                               MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!targetShiftId.HasValue)
            {
                var shiftIdInput = ShowShiftIdDialog();
                if (!shiftIdInput.HasValue)
                {
                    MessageBox.Show("Введите корректный ID смены", "Предупреждение",
                                   MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                targetShiftId = shiftIdInput.Value;
            }

            int equipmentId = ((SimpleItem)EquipmentComboBox.SelectedItem).Id;

            DateTime? shiftDate = GetShiftDate(targetShiftId.Value);
            if (!shiftDate.HasValue)
            {
                MessageBox.Show("Смена с указанным ID не найдена", "Ошибка",
                               MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            TimeSpan totalDowntime = GetTotalDowntimeForEquipment(equipmentId, shiftDate.Value);
            TimeSpan workTime = shiftDuration - totalDowntime;
            if (workTime.TotalSeconds < 0)
                workTime = TimeSpan.Zero;

            double productionQuantity = 0;
            if (!double.TryParse(ProductionQuantityTextBox.Text, out productionQuantity))
            {
                MessageBox.Show("Введите корректное количество продукции", "Предупреждение",
                               MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            double qualityQuantity = 0;
            if (!double.TryParse(QualityProductionQuantityTextBox.Text, out qualityQuantity))
            {
                MessageBox.Show("Введите корректное количество качественной продукции", "Предупреждение",
                               MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (qualityQuantity > productionQuantity)
            {
                MessageBox.Show("Качественная продукция не может превышать общее количество", "Предупреждение",
                               MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            using (var connection = dbconnection.GetConnection())
            {
                try
                {
                    connection.Open();

                    string query = @"INSERT INTO shiftdetails 
                                   (equipmentid, shiftid, equipmentworktime, productionquantity, qualityproductionquantity) 
                                   VALUES (@equipmentid, @shiftid, @equipmentworktime, @productionquantity, @qualityproductionquantity)";

                    using (var cmd = new NpgsqlCommand(query, connection))
                    {
                        cmd.Parameters.AddWithValue("@equipmentid", equipmentId);
                        cmd.Parameters.AddWithValue("@shiftid", targetShiftId.Value);
                        cmd.Parameters.AddWithValue("@equipmentworktime", workTime);
                        cmd.Parameters.AddWithValue("@productionquantity", productionQuantity);
                        cmd.Parameters.AddWithValue("@qualityproductionquantity", qualityQuantity);
                        cmd.ExecuteNonQuery();
                    }

                    MessageBox.Show("Данные добавлены", "Успех",
                                   MessageBoxButton.OK, MessageBoxImage.Information);
                    ClearFields();
                    LoadShiftDetails();
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
            if (ShiftDetailsDataGrid.SelectedItem is ShiftDetails selected)
            {
                if (EquipmentComboBox.SelectedItem == null)
                {
                    MessageBox.Show("Выберите оборудование", "Предупреждение",
                                   MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                int equipmentId = ((SimpleItem)EquipmentComboBox.SelectedItem).Id;

                TimeSpan workTime = selected.EquipmentWorkTime;

                double productionQuantity = 0;
                if (!double.TryParse(ProductionQuantityTextBox.Text, out productionQuantity))
                {
                    MessageBox.Show("Введите корректное количество продукции", "Предупреждение",
                                   MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                double qualityQuantity = 0;
                if (!double.TryParse(QualityProductionQuantityTextBox.Text, out qualityQuantity))
                {
                    MessageBox.Show("Введите корректное количество качественной продукции", "Предупреждение",
                                   MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (qualityQuantity > productionQuantity)
                {
                    MessageBox.Show("Качественная продукция не может превышать общее количество", "Предупреждение",
                                   MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                using (var connection = dbconnection.GetConnection())
                {
                    try
                    {
                        connection.Open();

                        string query = @"UPDATE shiftdetails 
                                       SET equipmentid = @equipmentid, 
                                           equipmentworktime = @equipmentworktime, 
                                           productionquantity = @productionquantity, 
                                           qualityproductionquantity = @qualityproductionquantity 
                                       WHERE shiftdetailsid = @shiftdetailsid";

                        using (var cmd = new NpgsqlCommand(query, connection))
                        {
                            cmd.Parameters.AddWithValue("@equipmentid", equipmentId);
                            cmd.Parameters.AddWithValue("@equipmentworktime", workTime);
                            cmd.Parameters.AddWithValue("@productionquantity", productionQuantity);
                            cmd.Parameters.AddWithValue("@qualityproductionquantity", qualityQuantity);
                            cmd.Parameters.AddWithValue("@shiftdetailsid", selected.ShiftDetailsId);
                            cmd.ExecuteNonQuery();
                        }

                        MessageBox.Show("Данные обновлены", "Успех",
                                       MessageBoxButton.OK, MessageBoxImage.Information);
                        ClearFields();
                        LoadShiftDetails();
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
            if (ShiftDetailsDataGrid.SelectedItem is ShiftDetails selected)
            {
                MessageBoxResult result = MessageBox.Show("Удалить выбранную запись?",
                    "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    using (var connection = dbconnection.GetConnection())
                    {
                        try
                        {
                            connection.Open();
                            string query = "DELETE FROM shiftdetails WHERE shiftdetailsid = @shiftdetailsid";
                            using (var cmd = new NpgsqlCommand(query, connection))
                            {
                                cmd.Parameters.AddWithValue("@shiftdetailsid", selected.ShiftDetailsId);
                                cmd.ExecuteNonQuery();
                            }
                            MessageBox.Show("Данные удалены", "Успех",
                                           MessageBoxButton.OK, MessageBoxImage.Information);
                            ClearFields();
                            LoadShiftDetails();
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
            LoadShiftInfo();
            LoadShiftDetails();
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
            ProductionQuantityTextBox.Text = "0";
            QualityProductionQuantityTextBox.Text = "0";
            if (EquipmentComboBox.Items.Count > 0)
                EquipmentComboBox.SelectedIndex = 0;
        }

        private void ShiftDetailsDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ShiftDetailsDataGrid.SelectedItem is ShiftDetails selected)
            {
                for (int i = 0; i < EquipmentComboBox.Items.Count; i++)
                {
                    var item = (SimpleItem)EquipmentComboBox.Items[i];
                    if (item.Id == selected.EquipmentId)
                    {
                        EquipmentComboBox.SelectedIndex = i;
                        break;
                    }
                }

                ProductionQuantityTextBox.Text = selected.ProductionQuantity.ToString();
                QualityProductionQuantityTextBox.Text = selected.QualityProductionQuantity.ToString();
            }
        }
    }
}