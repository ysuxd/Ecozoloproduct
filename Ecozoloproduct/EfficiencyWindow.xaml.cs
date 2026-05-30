using Npgsql;
using PdfSharp;
using PdfSharp.Drawing;
using PdfSharp.Fonts;
using PdfSharp.Pdf;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using WpfApp.Database;
using Microsoft.Win32;

namespace WpfApp2
{
    public partial class EfficiencyWindow : Window
    {
        private DatabaseConnection dbconnection = new DatabaseConnection();
        private string currentUserRole;
        private string currentUserLogin;
        private List<EfficiencyData> currentResults = new List<EfficiencyData>();

        public class EfficiencyData
        {
            public int EfficiencyId { get; set; }
            public int EquipmentId { get; set; }
            public string EquipmentName { get; set; }
            public int StatusId { get; set; }
            public string StatusName { get; set; }
            public int ShiftId { get; set; }
            public string ShiftInfo { get; set; }
            public int ShiftDetailId { get; set; }
            public int DowntimeId { get; set; }
            public string DowntimeName { get; set; }
            public int Availability { get; set; }
            public int Performance { get; set; }
            public int Quality { get; set; }
            public int FinalIndex { get; set; }
            public string AvailabilityDisplay => $"{Availability}%";
            public string PerformanceDisplay => $"{Performance}%";
            public string QualityDisplay => $"{Quality}%";
            public string FinalIndexDisplay => $"{FinalIndex}%";
        }

        public class SimpleItem
        {
            public int Id { get; set; }
            public string Name { get; set; }
        }

        public EfficiencyWindow(string userRole = "Пользователь", string userLogin = "")
        {
            InitializeComponent();
            currentUserRole = userRole;
            currentUserLogin = userLogin;

            StartDatePicker.SelectedDate = null;
            EndDatePicker.SelectedDate = null;

            LoadEquipment();
            LoadShifts();

            StartDatePicker.SelectedDateChanged += DatePicker_SelectedDateChanged;
            EndDatePicker.SelectedDateChanged += DatePicker_SelectedDateChanged;
            GlobalFontSettings.FontResolver = new FontResolver();
        }

        public class FontResolver : IFontResolver
        {
            public byte[] GetFont(string faceName)
            {
                using (var ms = new MemoryStream())
                {
                    // Используем стандартный шрифт Windows
                    var fontPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Fonts), "times.ttf");
                    if (File.Exists(fontPath))
                    {
                        return File.ReadAllBytes(fontPath);
                    }
                    return null;
                }
            }

            public FontResolverInfo ResolveTypeface(string familyName, bool isBold, bool isItalic)
            {
                return new FontResolverInfo("times.ttf");
            }
        }

        public void SetDateRange(DateTime startDate, DateTime endDate)
        {
            StartDatePicker.SelectedDate = startDate;
            EndDatePicker.SelectedDate = endDate;
            ShiftComboBox.IsEnabled = false;
            ShiftComboBox.SelectedIndex = 0;

            CalculateEfficiency();
        }

        private void DatePicker_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            bool hasStartDate = StartDatePicker.SelectedDate.HasValue;
            bool hasEndDate = EndDatePicker.SelectedDate.HasValue;

            if (hasStartDate && hasEndDate)
            {
                ShiftComboBox.IsEnabled = false;
                ShiftComboBox.SelectedIndex = 0;
                CalculateEfficiency();
            }
            else
            {
                ShiftComboBox.IsEnabled = true;
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
                        items.Add(new SimpleItem { Id = 0, Name = "--- Все оборудование ---" });
                        while (reader.Read())
                        {
                            items.Add(new SimpleItem
                            {
                                Id = reader.GetInt32(0),
                                Name = reader.GetString(1)
                            });
                        }
                        EquipmentComboBox.ItemsSource = items;
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
                        var items = new List<SimpleItem>();
                        items.Add(new SimpleItem { Id = 0, Name = "--- Все смены ---" });
                        while (reader.Read())
                        {
                            int id = reader.GetInt32(0);
                            DateTime date = reader.GetDateTime(1);
                            TimeSpan start = reader.GetTimeSpan(2);
                            TimeSpan end = reader.GetTimeSpan(3);
                            string info = $"{date:dd.MM.yyyy} {start:hh\\:mm}-{end:hh\\:mm}";
                            items.Add(new SimpleItem { Id = id, Name = info });
                        }
                        ShiftComboBox.ItemsSource = items;
                        ShiftComboBox.SelectedIndex = 0;
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка загрузки смен: {ex.Message}", "Ошибка",
                                   MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private List<int> GetShiftIdsInPeriod(DateTime startDate, DateTime endDate)
        {
            var shiftIds = new List<int>();
            using (var connection = dbconnection.GetConnection())
            {
                try
                {
                    connection.Open();
                    string query = "SELECT shiftid FROM shift WHERE shiftdate >= @startdate AND shiftdate <= @enddate ORDER BY shiftdate";
                    using (var cmd = new NpgsqlCommand(query, connection))
                    {
                        cmd.Parameters.AddWithValue("@startdate", startDate);
                        cmd.Parameters.AddWithValue("@enddate", endDate);
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                shiftIds.Add(reader.GetInt32(0));
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Ошибка получения смен за период: {ex.Message}");
                }
            }
            return shiftIds;
        }

        private double CalculateAvailability(TimeSpan workTime)
        {
            TimeSpan shiftDuration = TimeSpan.FromHours(12);
            if (workTime.TotalHours == 0) return 0;
            double result = (workTime.TotalHours / shiftDuration.TotalHours) * 100;
            if (result > 100) result = 100;
            return Math.Round(result, 0);
        }

        private double CalculatePerformance(double productionQuantity, double norm, TimeSpan workTime)
        {
            if (norm <= 0 || workTime.TotalHours == 0) return 0;
            double productionPerHour = productionQuantity / workTime.TotalHours;
            double result = (productionPerHour / norm) * 100;
            if (result > 100) result = 100;
            return Math.Round(result, 0);
        }

        private double CalculateQuality(double qualityQuantity, double productionQuantity)
        {
            if (productionQuantity <= 0) return 0;
            double result = (qualityQuantity / productionQuantity) * 100;
            if (result > 100) result = 100;
            return Math.Round(result, 0);
        }

        private double GetEquipmentNorm(int equipmentId)
        {
            using (var connection = dbconnection.GetConnection())
            {
                try
                {
                    connection.Open();
                    string query = "SELECT norm FROM equipment WHERE equipmentid = @equipmentid";
                    using (var cmd = new NpgsqlCommand(query, connection))
                    {
                        cmd.Parameters.AddWithValue("@equipmentid", equipmentId);
                        var result = cmd.ExecuteScalar();
                        if (result != DBNull.Value && result != null)
                            return Convert.ToDouble(result);
                        return 0;
                    }
                }
                catch
                {
                    return 0;
                }
            }
        }

        private string GetEquipmentName(int equipmentId)
        {
            using (var connection = dbconnection.GetConnection())
            {
                try
                {
                    connection.Open();
                    string query = "SELECT equipmentname FROM equipment WHERE equipmentid = @equipmentid";
                    using (var cmd = new NpgsqlCommand(query, connection))
                    {
                        cmd.Parameters.AddWithValue("@equipmentid", equipmentId);
                        var result = cmd.ExecuteScalar();
                        if (result != null)
                            return result.ToString();
                    }
                }
                catch { }
            }
            return $"Оборудование {equipmentId}";
        }

        private int? GetStatusIdForEquipment(int equipmentId)
        {
            using (var connection = dbconnection.GetConnection())
            {
                try
                {
                    connection.Open();
                    string query = "SELECT statusid FROM equipment WHERE equipmentid = @equipmentid";
                    using (var cmd = new NpgsqlCommand(query, connection))
                    {
                        cmd.Parameters.AddWithValue("@equipmentid", equipmentId);
                        var result = cmd.ExecuteScalar();
                        if (result != null)
                            return Convert.ToInt32(result);
                        return null;
                    }
                }
                catch
                {
                    return null;
                }
            }
        }

        private List<SimpleItem> GetAllEquipment()
        {
            var items = new List<SimpleItem>();
            using (var connection = dbconnection.GetConnection())
            {
                try
                {
                    connection.Open();
                    string query = "SELECT equipmentid, equipmentname FROM equipment ORDER BY equipmentname";
                    using (var cmd = new NpgsqlCommand(query, connection))
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            items.Add(new SimpleItem
                            {
                                Id = reader.GetInt32(0),
                                Name = reader.GetString(1)
                            });
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка загрузки оборудования: {ex.Message}", "Ошибка",
                                   MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            return items;
        }

        private void SaveEfficiencyToDatabase(EfficiencyData data)
        {
            using (var connection = dbconnection.GetConnection())
            {
                try
                {
                    connection.Open();

                    string checkQuery = "SELECT COUNT(*) FROM efficiency WHERE equipmentid = @equipmentid AND shiftid = @shiftid";
                    using (var checkCmd = new NpgsqlCommand(checkQuery, connection))
                    {
                        checkCmd.Parameters.AddWithValue("@equipmentid", data.EquipmentId);
                        checkCmd.Parameters.AddWithValue("@shiftid", data.ShiftId);
                        int count = Convert.ToInt32(checkCmd.ExecuteScalar());

                        if (count > 0)
                        {
                            string updateQuery = @"
                                UPDATE efficiency 
                                SET availability = @availability, 
                                    performance = @performance, 
                                    quantity = @quantity, 
                                    finalindex = @finalindex
                                WHERE equipmentid = @equipmentid AND shiftid = @shiftid";

                            using (var updateCmd = new NpgsqlCommand(updateQuery, connection))
                            {
                                updateCmd.Parameters.AddWithValue("@availability", data.Availability);
                                updateCmd.Parameters.AddWithValue("@performance", data.Performance);
                                updateCmd.Parameters.AddWithValue("@quantity", data.Quality);
                                updateCmd.Parameters.AddWithValue("@finalindex", data.FinalIndex);
                                updateCmd.Parameters.AddWithValue("@equipmentid", data.EquipmentId);
                                updateCmd.Parameters.AddWithValue("@shiftid", data.ShiftId);
                                updateCmd.ExecuteNonQuery();
                            }
                        }
                        else
                        {
                            string insertQuery = @"
                                INSERT INTO efficiency 
                                (equipmentid, statusid, shiftid, shiftdetailid, downtimeid, availability, performance, quantity, finalindex) 
                                VALUES (@equipmentid, @statusid, @shiftid, @shiftdetailid, @downtimeid, @availability, @performance, @quantity, @finalindex)";

                            using (var insertCmd = new NpgsqlCommand(insertQuery, connection))
                            {
                                insertCmd.Parameters.AddWithValue("@equipmentid", data.EquipmentId);
                                insertCmd.Parameters.AddWithValue("@statusid", data.StatusId);
                                insertCmd.Parameters.AddWithValue("@shiftid", data.ShiftId);
                                insertCmd.Parameters.AddWithValue("@shiftdetailid", data.ShiftDetailId);
                                insertCmd.Parameters.AddWithValue("@downtimeid", data.DowntimeId);
                                insertCmd.Parameters.AddWithValue("@availability", data.Availability);
                                insertCmd.Parameters.AddWithValue("@performance", data.Performance);
                                insertCmd.Parameters.AddWithValue("@quantity", data.Quality);
                                insertCmd.Parameters.AddWithValue("@finalindex", data.FinalIndex);
                                insertCmd.ExecuteNonQuery();
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Ошибка сохранения эффективности: {ex.Message}");
                }
            }
        }
        private (TimeSpan totalWorkTime, double totalProduction, double totalQuality, List<int> shiftDetailIds)
    GetShiftDetailsDataInPeriod(List<int> shiftIds, int? equipmentId = null)
        {
            if (shiftIds.Count == 0) return (TimeSpan.Zero, 0, 0, new List<int>());

            using (var connection = dbconnection.GetConnection())
            {
                try
                {
                    connection.Open();
                    string query = @"
                SELECT 
                    COALESCE(SUM(sd.equipmentworktime), '0 seconds'::interval) as total_worktime,
                    COALESCE(SUM(sd.productionquantity), 0) as total_production,
                    COALESCE(SUM(sd.qualityproductionquantity), 0) as total_quality,
                    ARRAY_AGG(sd.shiftdetailsid) as shiftdetail_ids
                FROM shiftdetails sd
                WHERE sd.shiftid = ANY(@shiftids)";

                    if (equipmentId.HasValue && equipmentId.Value > 0)
                    {
                        query += " AND sd.equipmentid = @equipmentid";
                    }

                    using (var cmd = new NpgsqlCommand(query, connection))
                    {
                        cmd.Parameters.AddWithValue("@shiftids", shiftIds.ToArray());
                        if (equipmentId.HasValue && equipmentId.Value > 0)
                        {
                            cmd.Parameters.AddWithValue("@equipmentid", equipmentId.Value);
                        }

                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                TimeSpan workTime = reader.GetTimeSpan(0);
                                double production = reader.GetDouble(1);
                                double quality = reader.GetDouble(2);
                                List<int> shiftDetailIds = new List<int>();
                                if (!reader.IsDBNull(3))
                                {
                                    int[] ids = (int[])reader.GetValue(3);
                                    shiftDetailIds.AddRange(ids);
                                }
                                return (workTime, production, quality, shiftDetailIds);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Ошибка получения данных: {ex.Message}");
                }
            }
            return (TimeSpan.Zero, 0, 0, new List<int>());
        }

        private List<(int equipmentId, string equipmentName, TimeSpan workTime, double production, double quality, List<int> shiftDetailIds)>
    GetAllEquipmentDataInPeriod(List<int> shiftIds)
        {
            var result = new List<(int, string, TimeSpan, double, double, List<int>)>();

            if (shiftIds.Count == 0) return result;

            using (var connection = dbconnection.GetConnection())
            {
                try
                {
                    connection.Open();
                    string query = @"
                SELECT 
                    sd.equipmentid,
                    e.equipmentname,
                    SUM(sd.equipmentworktime) as total_worktime,
                    SUM(sd.productionquantity) as total_production,
                    SUM(sd.qualityproductionquantity) as total_quality,
                    ARRAY_AGG(sd.shiftdetailsid) as shiftdetail_ids
                FROM shiftdetails sd
                LEFT JOIN equipment e ON sd.equipmentid = e.equipmentid
                WHERE sd.shiftid = ANY(@shiftids)
                GROUP BY sd.equipmentid, e.equipmentname
                HAVING SUM(sd.equipmentworktime) > '0 seconds'::interval 
                   OR SUM(sd.productionquantity) > 0 
                   OR SUM(sd.qualityproductionquantity) > 0
                ORDER BY e.equipmentname";

                    using (var cmd = new NpgsqlCommand(query, connection))
                    {
                        cmd.Parameters.AddWithValue("@shiftids", shiftIds.ToArray());

                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                int equipmentId = reader.GetInt32(0);
                                string equipmentName = reader.GetString(1);
                                TimeSpan workTime = reader.IsDBNull(2) ? TimeSpan.Zero : reader.GetTimeSpan(2);
                                double production = reader.IsDBNull(3) ? 0 : reader.GetDouble(3);
                                double quality = reader.IsDBNull(4) ? 0 : reader.GetDouble(4);
                                List<int> shiftDetailIds = new List<int>();
                                if (!reader.IsDBNull(5))
                                {
                                    int[] ids = (int[])reader.GetValue(5);
                                    shiftDetailIds.AddRange(ids);
                                }
                                result.Add((equipmentId, equipmentName, workTime, production, quality, shiftDetailIds));
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Ошибка получения данных: {ex.Message}");
                }
            }
            return result;
        }

        private void CalculateEfficiency()
        {
            int selectedEquipmentId = 0;
            if (EquipmentComboBox.SelectedItem != null)
                selectedEquipmentId = ((SimpleItem)EquipmentComboBox.SelectedItem).Id;

            int selectedShiftId = 0;
            if (ShiftComboBox.SelectedItem != null)
                selectedShiftId = ((SimpleItem)ShiftComboBox.SelectedItem).Id;

            DateTime? startDate = StartDatePicker.SelectedDate;
            DateTime? endDate = EndDatePicker.SelectedDate;

            List<EfficiencyData> results = new List<EfficiencyData>();
            bool hasPeriod = startDate.HasValue && endDate.HasValue;

            if (hasPeriod)
            {
                if (startDate.Value > endDate.Value)
                {
                    MessageBox.Show("Дата начала не может быть позже даты окончания", "Предупреждение",
                                   MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var shiftIds = GetShiftIdsInPeriod(startDate.Value, endDate.Value);

                if (shiftIds.Count == 0)
                {
                    EfficiencyDataGrid.ItemsSource = null;
                    TotalOEETextBlock.Text = "0";
                    MessageBox.Show("Нет смен за выбранный период", "Информация",
                                   MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                if (selectedEquipmentId > 0)
                {
                    var data = GetShiftDetailsDataInPeriod(shiftIds, selectedEquipmentId);
                    if (data.totalProduction == 0 && data.totalWorkTime.TotalHours == 0)
                    {
                        MessageBox.Show("Нет данных по выбранному оборудованию за указанный период", "Информация",
                                       MessageBoxButton.OK, MessageBoxImage.Information);
                        return;
                    }

                    double norm = GetEquipmentNorm(selectedEquipmentId);
                    string equipmentName = GetEquipmentName(selectedEquipmentId);
                    int? statusId = GetStatusIdForEquipment(selectedEquipmentId);

                    double availability = CalculateAvailability(data.totalWorkTime);
                    double performance = CalculatePerformance(data.totalProduction, norm, data.totalWorkTime);
                    double quality = CalculateQuality(data.totalQuality, data.totalProduction);
                    double oee = Math.Round(availability * performance * quality / 10000, 0);

                    var efficiencyData = new EfficiencyData
                    {
                        EquipmentId = selectedEquipmentId,
                        EquipmentName = equipmentName,
                        StatusId = statusId ?? 0,
                        ShiftId = shiftIds.Count == 1 ? shiftIds[0] : 0,
                        ShiftDetailId = data.shiftDetailIds.Count > 0 ? data.shiftDetailIds[0] : 0,
                        DowntimeId = 0,
                        Availability = (int)availability,
                        Performance = (int)performance,
                        Quality = (int)quality,
                        FinalIndex = (int)oee
                    };

                    SaveEfficiencyToDatabase(efficiencyData);
                    results.Add(efficiencyData);
                }
                else
                {
                    var allEquipmentData = GetAllEquipmentDataInPeriod(shiftIds);

                    if (allEquipmentData.Count == 0)
                    {
                        EfficiencyDataGrid.ItemsSource = null;
                        TotalOEETextBlock.Text = "0";
                        MessageBox.Show("Нет данных по оборудованию за выбранный период", "Информация",
                                       MessageBoxButton.OK, MessageBoxImage.Information);
                        return;
                    }

                    double totalOEE = 0;

                    foreach (var eq in allEquipmentData)
                    {
                        double norm = GetEquipmentNorm(eq.equipmentId);
                        int? statusId = GetStatusIdForEquipment(eq.equipmentId);
                        double availability = CalculateAvailability(eq.workTime);
                        double performance = CalculatePerformance(eq.production, norm, eq.workTime);
                        double quality = CalculateQuality(eq.quality, eq.production);
                        double oee = Math.Round(availability * performance * quality / 10000, 0);

                        var efficiencyData = new EfficiencyData
                        {
                            EquipmentId = eq.equipmentId,
                            EquipmentName = eq.equipmentName,
                            StatusId = statusId ?? 0,
                            ShiftId = shiftIds.Count == 1 ? shiftIds[0] : 0,
                            ShiftDetailId = eq.shiftDetailIds.Count > 0 ? eq.shiftDetailIds[0] : 0,
                            DowntimeId = 0,
                            Availability = (int)availability,
                            Performance = (int)performance,
                            Quality = (int)quality,
                            FinalIndex = (int)oee
                        };

                        SaveEfficiencyToDatabase(efficiencyData);
                        results.Add(efficiencyData);
                        totalOEE += oee;
                    }

                    if (results.Count > 0)
                    {
                        TotalOEETextBlock.Text = $"{Math.Round(totalOEE / results.Count, 0)}";
                    }
                }
            }
            else
            {
                if (selectedShiftId == 0)
                {
                    EfficiencyDataGrid.ItemsSource = null;
                    TotalOEETextBlock.Text = "0";
                    MessageBox.Show("Выберите смену или укажите период", "Предупреждение",
                                   MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var shiftIds = new List<int> { selectedShiftId };

                if (selectedEquipmentId > 0)
                {
                    var data = GetShiftDetailsDataInPeriod(shiftIds, selectedEquipmentId);
                    if (data.totalProduction == 0 && data.totalWorkTime.TotalHours == 0)
                    {
                        MessageBox.Show("Нет данных по выбранному оборудованию за указанную смену", "Информация",
                                       MessageBoxButton.OK, MessageBoxImage.Information);
                        return;
                    }

                    double norm = GetEquipmentNorm(selectedEquipmentId);
                    string equipmentName = GetEquipmentName(selectedEquipmentId);
                    int? statusId = GetStatusIdForEquipment(selectedEquipmentId);

                    double availability = CalculateAvailability(data.totalWorkTime);
                    double performance = CalculatePerformance(data.totalProduction, norm, data.totalWorkTime);
                    double quality = CalculateQuality(data.totalQuality, data.totalProduction);
                    double oee = Math.Round(availability * performance * quality / 10000, 0);

                    var efficiencyData = new EfficiencyData
                    {
                        EquipmentId = selectedEquipmentId,
                        EquipmentName = equipmentName,
                        StatusId = statusId ?? 0,
                        ShiftId = selectedShiftId,
                        ShiftDetailId = data.shiftDetailIds.Count > 0 ? data.shiftDetailIds[0] : 0,
                        DowntimeId = 0,
                        Availability = (int)availability,
                        Performance = (int)performance,
                        Quality = (int)quality,
                        FinalIndex = (int)oee
                    };

                    SaveEfficiencyToDatabase(efficiencyData);
                    results.Add(efficiencyData);
                }
                else
                {
                    var allEquipmentData = GetAllEquipmentDataInPeriod(shiftIds);

                    if (allEquipmentData.Count == 0)
                    {
                        EfficiencyDataGrid.ItemsSource = null;
                        TotalOEETextBlock.Text = "0";
                        MessageBox.Show("Нет данных по оборудованию за указанную смену", "Информация",
                                       MessageBoxButton.OK, MessageBoxImage.Information);
                        return;
                    }

                    double totalOEE = 0;

                    foreach (var eq in allEquipmentData)
                    {
                        double norm = GetEquipmentNorm(eq.equipmentId);
                        int? statusId = GetStatusIdForEquipment(eq.equipmentId);
                        double availability = CalculateAvailability(eq.workTime);
                        double performance = CalculatePerformance(eq.production, norm, eq.workTime);
                        double quality = CalculateQuality(eq.quality, eq.production);
                        double oee = Math.Round(availability * performance * quality / 10000, 0);

                        var efficiencyData = new EfficiencyData
                        {
                            EquipmentId = eq.equipmentId,
                            EquipmentName = eq.equipmentName,
                            StatusId = statusId ?? 0,
                            ShiftId = selectedShiftId,
                            ShiftDetailId = eq.shiftDetailIds.Count > 0 ? eq.shiftDetailIds[0] : 0,
                            DowntimeId = 0,
                            Availability = (int)availability,
                            Performance = (int)performance,
                            Quality = (int)quality,
                            FinalIndex = (int)oee
                        };

                        SaveEfficiencyToDatabase(efficiencyData);
                        results.Add(efficiencyData);
                        totalOEE += oee;
                    }

                    if (results.Count > 0)
                    {
                        TotalOEETextBlock.Text = $"{Math.Round(totalOEE / results.Count, 0)}";
                    }
                }
            }

            currentResults = results;
            EfficiencyDataGrid.ItemsSource = results;

            if (results.Count == 0)
            {
                TotalOEETextBlock.Text = "0";
            }
        }

        private void CalculateButton_Click(object sender, RoutedEventArgs e)
        {
            CalculateEfficiency();
        }

        private void CompareButton_Click(object sender, RoutedEventArgs e)
        {
            if (currentResults == null || currentResults.Count == 0)
            {
                MessageBox.Show("Сначала выполните расчет эффективности", "Предупреждение",
                               MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var chartData = new List<EfficiencyData>();
            var grouped = currentResults.GroupBy(x => x.EquipmentId);
            foreach (var group in grouped)
            {
                var avgData = new EfficiencyData
                {
                    EquipmentId = group.Key,
                    EquipmentName = group.First().EquipmentName,
                    FinalIndex = (int)Math.Round(group.Average(x => x.FinalIndex))
                };
                chartData.Add(avgData);
            }

            var chartWindow = new ChartWindow(chartData, $"Сравнение OEE оборудования", "bar");
            chartWindow.Owner = this;
            chartWindow.ShowDialog();
        }

        private void TrendButton_Click(object sender, RoutedEventArgs e)
        {
            DateTime? startDate = StartDatePicker.SelectedDate;
            DateTime? endDate = EndDatePicker.SelectedDate;

            if (!startDate.HasValue || !endDate.HasValue)
            {
                MessageBox.Show("Выберите период для анализа динамики", "Предупреждение",
                               MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (startDate.Value > endDate.Value)
            {
                MessageBox.Show("Дата начала не может быть позже даты окончания", "Предупреждение",
                               MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var shiftIds = GetShiftIdsInPeriod(startDate.Value, endDate.Value);
            if (shiftIds.Count == 0)
            {
                MessageBox.Show("Нет смен за выбранный период", "Предупреждение",
                               MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var trendData = new List<EfficiencyData>();
            int selectedEquipmentId = 0;
            if (EquipmentComboBox.SelectedItem != null)
                selectedEquipmentId = ((SimpleItem)EquipmentComboBox.SelectedItem).Id;

            if (selectedEquipmentId > 0)
            {
                double norm = GetEquipmentNorm(selectedEquipmentId);
                string equipmentName = GetEquipmentName(selectedEquipmentId);
                int? statusId = GetStatusIdForEquipment(selectedEquipmentId);

                foreach (int shiftId in shiftIds)
                {
                    var data = GetShiftDetailsDataInPeriod(new List<int> { shiftId }, selectedEquipmentId);
                    if (data.totalProduction == 0 && data.totalWorkTime.TotalHours == 0) continue;

                    double availability = CalculateAvailability(data.totalWorkTime);
                    double performance = CalculatePerformance(data.totalProduction, norm, data.totalWorkTime);
                    double quality = CalculateQuality(data.totalQuality, data.totalProduction);
                    double oee = Math.Round(availability * performance * quality / 10000, 0);

                    DateTime? shiftDate = GetShiftDate(shiftId);
                    string shiftInfo = shiftDate.HasValue ? shiftDate.Value.ToString("dd.MM.yyyy") : shiftId.ToString();

                    trendData.Add(new EfficiencyData
                    {
                        EquipmentId = selectedEquipmentId,
                        EquipmentName = equipmentName,
                        StatusId = statusId ?? 0,
                        ShiftId = shiftId,
                        ShiftInfo = shiftInfo,
                        FinalIndex = (int)oee
                    });
                }
            }
            else
            {
                var allEquipment = GetAllEquipmentDataInPeriod(shiftIds);

                foreach (var equipment in allEquipment)
                {
                    double norm = GetEquipmentNorm(equipment.equipmentId);
                    int? statusId = GetStatusIdForEquipment(equipment.equipmentId);

                    foreach (int shiftId in shiftIds)
                    {
                        var data = GetShiftDetailsDataInPeriod(new List<int> { shiftId }, equipment.equipmentId);
                        if (data.totalProduction == 0 && data.totalWorkTime.TotalHours == 0) continue;

                        double availability = CalculateAvailability(data.totalWorkTime);
                        double performance = CalculatePerformance(data.totalProduction, norm, data.totalWorkTime);
                        double quality = CalculateQuality(data.totalQuality, data.totalProduction);
                        double oee = Math.Round(availability * performance * quality / 10000, 0);

                        DateTime? shiftDate = GetShiftDate(shiftId);
                        string shiftInfo = shiftDate.HasValue ? shiftDate.Value.ToString("dd.MM.yyyy") : shiftId.ToString();

                        trendData.Add(new EfficiencyData
                        {
                            EquipmentId = equipment.equipmentId,
                            EquipmentName = equipment.equipmentName,
                            StatusId = statusId ?? 0,
                            ShiftId = shiftId,
                            ShiftInfo = shiftInfo,
                            FinalIndex = (int)oee
                        });
                    }
                }
            }

            if (trendData.Count == 0)
            {
                MessageBox.Show("Нет данных для отображения динамики", "Предупреждение",
                               MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string title = selectedEquipmentId > 0
                ? $"Динамика OEE оборудования '{GetEquipmentName(selectedEquipmentId)}'"
                : "Динамика OEE всего оборудования";

            var chartWindow = new ChartWindow(trendData, title, "line");
            chartWindow.Owner = this;
            chartWindow.ShowDialog();
        }
        private List<EfficiencyData> GetLineChartData()
        {
            var lineData = new List<EfficiencyData>();

            DateTime? startDate = StartDatePicker.SelectedDate;
            DateTime? endDate = EndDatePicker.SelectedDate;

            if (!startDate.HasValue || !endDate.HasValue)
                return lineData;

            var shiftIds = GetShiftIdsInPeriod(startDate.Value, endDate.Value);
            if (shiftIds.Count == 0)
                return lineData;

            int selectedEquipmentId = 0;
            if (EquipmentComboBox.SelectedItem != null)
                selectedEquipmentId = ((SimpleItem)EquipmentComboBox.SelectedItem).Id;

            var shiftDates = new Dictionary<int, DateTime>();
            foreach (int shiftId in shiftIds)
            {
                DateTime? shiftDate = GetShiftDate(shiftId);
                if (shiftDate.HasValue)
                    shiftDates[shiftId] = shiftDate.Value;
            }

            if (selectedEquipmentId > 0)
            {
                double norm = GetEquipmentNorm(selectedEquipmentId);
                string equipmentName = GetEquipmentName(selectedEquipmentId);

                foreach (int shiftId in shiftIds)
                {
                    var data = GetShiftDetailsDataInPeriod(new List<int> { shiftId }, selectedEquipmentId);

                    double availability = CalculateAvailability(data.totalWorkTime);
                    double performance = CalculatePerformance(data.totalProduction, norm, data.totalWorkTime);
                    double quality = CalculateQuality(data.totalQuality, data.totalProduction);
                    double oee = Math.Round(availability * performance * quality / 10000, 0);

                    DateTime shiftDate = shiftDates.ContainsKey(shiftId) ? shiftDates[shiftId] : DateTime.Now;
                    string shiftInfo = shiftDate.ToString("dd.MM.yyyy");

                    lineData.Add(new EfficiencyData
                    {
                        EquipmentId = selectedEquipmentId,
                        EquipmentName = equipmentName,
                        ShiftId = shiftId,
                        ShiftInfo = shiftInfo,
                        FinalIndex = (int)oee
                    });
                }
            }
            else
            {
                var allEquipment = GetAllEquipmentDataInPeriod(shiftIds);

                foreach (var equipment in allEquipment)
                {
                    double norm = GetEquipmentNorm(equipment.equipmentId);
                    string equipmentName = equipment.equipmentName;

                    foreach (int shiftId in shiftIds)
                    {
                        var data = GetShiftDetailsDataInPeriod(new List<int> { shiftId }, equipment.equipmentId);

                        double availability = CalculateAvailability(data.totalWorkTime);
                        double performance = CalculatePerformance(data.totalProduction, norm, data.totalWorkTime);
                        double quality = CalculateQuality(data.totalQuality, data.totalProduction);
                        double oee = Math.Round(availability * performance * quality / 10000, 0);

                        DateTime shiftDate = shiftDates.ContainsKey(shiftId) ? shiftDates[shiftId] : DateTime.Now;
                        string shiftInfo = shiftDate.ToString("dd.MM.yyyy");

                        lineData.Add(new EfficiencyData
                        {
                            EquipmentId = equipment.equipmentId,
                            EquipmentName = equipmentName,
                            ShiftId = shiftId,
                            ShiftInfo = shiftInfo,
                            FinalIndex = (int)oee
                        });
                    }
                }
            }

            return lineData;
        }
        private void ExportPdfButton_Click(object sender, RoutedEventArgs e)
        {
            if (currentResults == null || currentResults.Count == 0)
            {
                MessageBox.Show("Нет данных для экспорта. Сначала выполните расчет эффективности.", "Предупреждение",
                               MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                // Путь для сохранения отчетов
                string reportsPath = @"C:\Users\Константин Сергеевич\Desktop\Диплом\Ecozoloproduct\Отчеты";

                // Создаем папку, если её нет
                if (!Directory.Exists(reportsPath))
                {
                    Directory.CreateDirectory(reportsPath);
                }

                string fileName = $"Отчет_по_эффективности_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";
                string filePath = Path.Combine(reportsPath, fileName);

                using (PdfDocument document = new PdfDocument())
                {
                    document.Info.Title = "Отчет по эффективности оборудования";

                    // СТРАНИЦА 1: Таблица и столбчатая диаграмма
                    PdfPage page1 = document.AddPage();
                    page1.Orientation = PageOrientation.Landscape;

                    using (XGraphics gfx = XGraphics.FromPdfPage(page1))
                    {
                        XFont titleFont = new XFont("Arial", 16, XFontStyleEx.Bold);
                        XFont headerFont = new XFont("Arial", 10, XFontStyleEx.Bold);
                        XFont regularFont = new XFont("Arial", 9, XFontStyleEx.Regular);
                        XFont smallFont = new XFont("Arial", 8, XFontStyleEx.Regular);

                        double yPos = 30;
                        double leftMargin = 30;
                        double pageWidth = page1.Width.Point;

                        // Заголовок
                        gfx.DrawString("ОТЧЕТ ПО ЭФФЕКТИВНОСТИ ОБОРУДОВАНИЯ", titleFont, XBrushes.DarkGreen,
                            new XRect(0, yPos, pageWidth, 30), XStringFormats.TopCenter);
                        yPos += 35;

                        // Параметры отчета
                        DateTime? startDate = StartDatePicker.SelectedDate;
                        DateTime? endDate = EndDatePicker.SelectedDate;
                        gfx.DrawString($"Период: {(startDate.HasValue ? startDate.Value.ToString("dd.MM.yyyy") : "не выбран")} - {(endDate.HasValue ? endDate.Value.ToString("dd.MM.yyyy") : "не выбран")}",
                            regularFont, XBrushes.Black, leftMargin, yPos);
                        yPos += 20;

                        string equipmentInfo = EquipmentComboBox.SelectedItem is SimpleItem selectedEq && selectedEq.Id > 0
                            ? $"Оборудование: {selectedEq.Name}"
                            : "Оборудование: Все";
                        gfx.DrawString(equipmentInfo, regularFont, XBrushes.Black, leftMargin, yPos);
                        yPos += 20;
                        gfx.DrawString($"Дата отчета: {DateTime.Now:dd.MM.yyyy HH:mm:ss}", regularFont, XBrushes.Black, leftMargin, yPos);
                        yPos += 25;

                        // Таблица
                        double tableWidth = pageWidth - leftMargin - 40;
                        double[] colWidths = { tableWidth * 0.35, tableWidth * 0.15, tableWidth * 0.15, tableWidth * 0.15, tableWidth * 0.2 };
                        double cellHeight = 22;
                        double currentX = leftMargin;

                        // Заголовки
                        string[] headers = { "Оборудование", "Доступность", "Производительность", "Качество", "OEE (%)" };
                        for (int i = 0; i < headers.Length; i++)
                        {
                            XRect cellRect = new XRect(currentX, yPos, colWidths[i], cellHeight);
                            gfx.DrawRectangle(XBrushes.DarkGreen, cellRect);
                            gfx.DrawString(headers[i], headerFont, XBrushes.White, cellRect, XStringFormats.Center);
                            currentX += colWidths[i];
                        }
                        yPos += cellHeight;

                        // Данные
                        foreach (var item in currentResults)
                        {
                            currentX = leftMargin;
                            string name = item.EquipmentName.Length > 35 ? item.EquipmentName.Substring(0, 32) + "..." : item.EquipmentName;
                            string[] rowData = {
                        name,
                        $"{item.Availability}%",
                        $"{item.Performance}%",
                        $"{item.Quality}%",
                        $"{item.FinalIndex}%"
                    };

                            for (int i = 0; i < rowData.Length; i++)
                            {
                                XRect cellRect = new XRect(currentX, yPos, colWidths[i], cellHeight);
                                gfx.DrawRectangle(i % 2 == 0 ? XBrushes.White : XBrushes.LightGray, cellRect);
                                gfx.DrawRectangle(XPens.Gray, cellRect);
                                gfx.DrawString(rowData[i], regularFont, XBrushes.Black, cellRect, XStringFormats.Center);
                                currentX += colWidths[i];
                            }
                            yPos += cellHeight;
                        }

                        yPos += 15;

                        // Итоговый OEE
                        double avgOEE = currentResults.Average(x => x.FinalIndex);
                        gfx.DrawString($"Средний OEE за период: {Math.Round(avgOEE, 1)}%", headerFont, XBrushes.DarkGreen, leftMargin, yPos);
                        yPos += 30;

                        // Столбчатая диаграмма
                        gfx.DrawString("СТОЛБЧАТАЯ ДИАГРАММА (сравнение OEE)", headerFont, XBrushes.DarkGreen, leftMargin, yPos);
                        yPos += 20;

                        DrawSimpleBarChart(gfx, currentResults, leftMargin, yPos, pageWidth - 80, 180, regularFont, smallFont);
                        yPos += 200;
                    }

                    // СТРАНИЦА 2: Линейная диаграмма (динамика)
                    PdfPage page2 = document.AddPage();
                    page2.Orientation = PageOrientation.Landscape;

                    using (XGraphics gfx = XGraphics.FromPdfPage(page2))
                    {
                        XFont headerFont = new XFont("Arial", 10, XFontStyleEx.Bold);
                        XFont titleFont = new XFont("Arial", 14, XFontStyleEx.Bold);

                        double leftMargin = 30;
                        double pageWidth = page2.Width.Point;
                        double yPos = 40;

                        // Заголовок страницы
                        gfx.DrawString("ДИНАМИКА OEE ПО СМЕНАМ", titleFont, XBrushes.DarkGreen,
                            new XRect(0, yPos, pageWidth, 30), XStringFormats.TopCenter);
                        yPos += 35;

                        // Получаем данные для линейной диаграммы
                        var lineData = GetLineChartData();

                        if (lineData.Count > 0)
                        {
                            DrawSimpleLineChart(gfx, lineData, leftMargin, yPos, pageWidth - 80, 300, new XFont("Arial", 8, XFontStyleEx.Regular));
                        }
                        else
                        {
                            gfx.DrawString("Нет данных для отображения динамики", new XFont("Arial", 9, XFontStyleEx.Regular), XBrushes.Black, leftMargin, yPos);
                        }
                    }

                    document.Save(filePath);

                    MessageBox.Show($"Отчет успешно сохранен!\nПуть: {filePath}", "Успех",
                                   MessageBoxButton.OK, MessageBoxImage.Information);

                    // Открываем папку с отчетом
                    Process.Start("explorer.exe", reportsPath);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при создании PDF: {ex.Message}", "Ошибка",
                               MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        // Столбчатая диаграмма
        private void DrawSimpleBarChart(XGraphics gfx, List<EfficiencyData> data, double x, double y, double width, double height, XFont regularFont, XFont smallFont)
        {
            if (data == null || data.Count == 0) return;

            double chartHeight = height - 40;
            double barWidth = (width - 60) / data.Count;
            double maxOEE = 100;
            double currentX = x + 40;

            // Рамка диаграммы
            gfx.DrawRectangle(XPens.Black, x + 35, y, width - 35, height);

            // Ось Y (метки)
            for (int i = 0; i <= 100; i += 20)
            {
                double yPos = y + chartHeight - (chartHeight * i / maxOEE);
                gfx.DrawString($"{i}%", smallFont, XBrushes.Black, x + 5, yPos + 3);

                XPen lightGrayPen = new XPen(XColors.LightGray, 0.5);
                gfx.DrawLine(lightGrayPen, x + 35, yPos, x + width - 5, yPos);
            }

            // Столбцы
            for (int i = 0; i < data.Count; i++)
            {
                double barHeight = chartHeight * data[i].FinalIndex / maxOEE;
                double barX = currentX + i * barWidth;
                double barY = y + chartHeight - barHeight;

                gfx.DrawRectangle(XBrushes.Green, barX, barY, barWidth - 2, barHeight);

                string name = data[i].EquipmentName.Length > 10 ? data[i].EquipmentName.Substring(0, 8) + ".." : data[i].EquipmentName;
                gfx.DrawString(name, smallFont, XBrushes.Black, barX + 2, y + chartHeight + 12);
            }

            gfx.DrawString("Оборудование", smallFont, XBrushes.Black, x + width / 2 - 30, y + height - 5);
            gfx.DrawString("OEE (%)", smallFont, XBrushes.Black, x + 5, y + height / 2, XStringFormats.CenterLeft);
        }

        // Линейная диаграмма
        private void DrawSimpleLineChart(XGraphics gfx, List<EfficiencyData> data, double x, double y, double width, double height, XFont font)
        {
            if (data == null || data.Count == 0) return;

            double chartHeight = height - 50;
            double chartWidth = width - 60;
            double maxOEE = 100;

            var grouped = data.GroupBy(d => d.EquipmentId);
            var allDates = data.Select(d => d.ShiftInfo).Where(d => !string.IsNullOrEmpty(d)).Distinct().OrderBy(d => d).ToList();

            if (allDates.Count == 0) return;

            double stepX = allDates.Count > 1 ? chartWidth / (allDates.Count - 1) : chartWidth;

            // Рамка
            gfx.DrawRectangle(XPens.Black, x + 35, y, chartWidth, chartHeight);

            // Ось Y
            for (int i = 0; i <= 100; i += 20)
            {
                double yPos = y + chartHeight - (chartHeight * i / maxOEE);
                gfx.DrawString($"{i}%", font, XBrushes.Black, x + 5, yPos + 3);
                XPen lightGrayPen = new XPen(XColors.LightGray, 0.5);
                gfx.DrawLine(lightGrayPen, x + 35, yPos, x + width - 5, yPos);
            }

            // Ось X
            for (int i = 0; i < allDates.Count; i++)
            {
                double xPos = x + 35 + i * stepX;
                string dateStr = allDates[i].Length > 10 ? allDates[i].Substring(0, 8) : allDates[i];
                gfx.DrawString(dateStr, font, XBrushes.Black, xPos - 15, y + chartHeight + 12);
            }

            // Цвета для линий
            XColor[] colors = new XColor[]
            {
        XColors.Green, XColors.Blue, XColors.Red, XColors.Orange,
        XColors.Purple, XColors.Teal, XColors.Brown, XColors.Pink
            };

            int colorIndex = 0;
            foreach (var equipment in grouped)
            {
                var valuesByDate = equipment.ToDictionary(d => d.ShiftInfo, d => d.FinalIndex);
                List<XPoint> points = new List<XPoint>();

                for (int i = 0; i < allDates.Count; i++)
                {
                    double xPos = x + 35 + i * stepX;
                    int oeeValue = valuesByDate.ContainsKey(allDates[i]) ? valuesByDate[allDates[i]] : 0;
                    double yPos = y + chartHeight - (chartHeight * oeeValue / maxOEE);
                    points.Add(new XPoint(xPos, yPos));
                }

                XPen linePen = new XPen(colors[colorIndex % colors.Length], 1.5);
                gfx.DrawLines(linePen, points.ToArray());

                // Легенда
                XSolidBrush legendBrush = new XSolidBrush(colors[colorIndex % colors.Length]);
                gfx.DrawRectangle(legendBrush, x + width - 120, y + colorIndex * 15, 10, 10);

                string name = equipment.First().EquipmentName;
                name = name.Length > 18 ? name.Substring(0, 15) + "..." : name;
                gfx.DrawString(name, font, XBrushes.Black, x + width - 105, y + colorIndex * 15 + 8);

                colorIndex++;
            }

            gfx.DrawString("Дата смены", font, XBrushes.Black, x + width / 2 - 30, y + height - 5);
            gfx.DrawString("OEE (%)", font, XBrushes.Black, x + 5, y + height / 2, XStringFormats.CenterLeft);
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

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            LoadEquipment();
            LoadShifts();

            StartDatePicker.SelectedDate = null;
            EndDatePicker.SelectedDate = null;

            ShiftComboBox.IsEnabled = true;

            EfficiencyDataGrid.ItemsSource = null;
            TotalOEETextBlock.Text = "0";
            currentResults.Clear();
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
    }
}