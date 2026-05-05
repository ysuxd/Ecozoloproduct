using LiveCharts;
using LiveCharts.Wpf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace WpfApp2
{
    public partial class ChartWindow : Window
    {
        private List<EfficiencyWindow.EfficiencyData> efficiencyData;
        private string chartType;

        // Массив цветов для разных линий
        private Brush[] colors = new Brush[]
        {
            new SolidColorBrush(Color.FromRgb(46, 125, 50)),   // Зеленый
            new SolidColorBrush(Color.FromRgb(25, 118, 210)),  // Синий
            new SolidColorBrush(Color.FromRgb(211, 47, 47)),   // Красный
            new SolidColorBrush(Color.FromRgb(245, 124, 0)),   // Оранжевый
            new SolidColorBrush(Color.FromRgb(156, 39, 176)),  // Фиолетовый
            new SolidColorBrush(Color.FromRgb(0, 131, 143)),   // Бирюзовый
            new SolidColorBrush(Color.FromRgb(121, 85, 72)),   // Коричневый
            new SolidColorBrush(Color.FromRgb(233, 30, 99)),   // Розовый
            new SolidColorBrush(Color.FromRgb(96, 125, 139)),  // Серо-голубой
            new SolidColorBrush(Color.FromRgb(255, 193, 7))    // Желтый
        };

        public ChartWindow(List<EfficiencyWindow.EfficiencyData> data, string title, string type = "bar")
        {
            InitializeComponent();

            if (data == null || data.Count == 0)
            {
                MessageBox.Show("Нет данных для отображения диаграммы", "Предупреждение",
                               MessageBoxButton.OK, MessageBoxImage.Warning);
                Close();
                return;
            }

            efficiencyData = data;
            TitleTextBlock.Text = title;

            // Настройка осей
            MainChart.AxisX?.Clear();
            MainChart.AxisY?.Clear();
            MainChart.AxisX = new AxesCollection();
            MainChart.AxisY = new AxesCollection();

            if (type == "bar")
            {
                MainChart.AxisX.Add(new Axis { Title = "Оборудование" });
                MainChart.AxisY.Add(new Axis { Title = "Средний OEE (%)", MinValue = 0, MaxValue = 100 });
                DrawBarChart();
            }
            else
            {
                MainChart.AxisX.Add(new Axis { Title = "Дата смены" });
                MainChart.AxisY.Add(new Axis { Title = "OEE (%)", MinValue = 0, MaxValue = 100 });
                DrawLineChart();
            }
        }

        private void UpdateLegend(Dictionary<string, Brush> legendItems)
        {
            LegendPanel.Children.Clear();

            foreach (var item in legendItems)
            {
                var legendItem = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(5, 3, 5, 3) };

                var colorBox = new Rectangle
                {
                    Width = 20,
                    Height = 20,
                    Fill = item.Value,
                    Margin = new Thickness(0, 0, 8, 0),
                    RadiusX = 3,
                    RadiusY = 3
                };

                var textBlock = new TextBlock
                {
                    Text = item.Key,
                    FontSize = 12,
                    VerticalAlignment = VerticalAlignment.Center,
                    Foreground = new SolidColorBrush(Color.FromRgb(74, 55, 40))
                };

                legendItem.Children.Add(colorBox);
                legendItem.Children.Add(textBlock);
                LegendPanel.Children.Add(legendItem);
            }
        }

        private void DrawBarChart()
        {
            try
            {
                MainChart.Series?.Clear();

                if (efficiencyData == null || efficiencyData.Count == 0) return;

                var equipmentNames = efficiencyData.Select(x => x.EquipmentName).ToList();
                var oeeValues = new ChartValues<int>();

                foreach (var item in efficiencyData)
                {
                    oeeValues.Add(item.FinalIndex);
                }

                MainChart.Series.Add(new ColumnSeries
                {
                    Title = "Средний OEE (%)",
                    Values = oeeValues,
                    Fill = Brushes.Green,
                    Stroke = Brushes.DarkGreen,
                    ColumnPadding = 5
                });

                if (MainChart.AxisX != null && MainChart.AxisX.Count > 0)
                {
                    MainChart.AxisX[0].Labels = equipmentNames;
                    MainChart.AxisX[0].Title = "Оборудование";
                }

                // Легенда для столбчатой диаграммы
                var legendItems = new Dictionary<string, Brush>();
                legendItems["Средний OEE (%)"] = Brushes.Green;
                UpdateLegend(legendItems);

                BarChartButton.IsEnabled = false;
                LineChartButton.IsEnabled = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при построении диаграммы: {ex.Message}", "Ошибка",
                               MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void DrawLineChart()
        {
            try
            {
                MainChart.Series?.Clear();

                if (efficiencyData == null || efficiencyData.Count == 0) return;

                // Группируем данные по оборудованию
                var groupedByEquipment = efficiencyData.GroupBy(x => x.EquipmentId);

                // Получаем все уникальные даты смен для оси X
                var allShiftDates = efficiencyData
                    .Select(x => x.ShiftInfo)
                    .Where(x => !string.IsNullOrEmpty(x))
                    .Distinct()
                    .OrderBy(x => x)
                    .ToList();

                int colorIndex = 0;
                var legendItems = new Dictionary<string, Brush>();

                foreach (var equipment in groupedByEquipment)
                {
                    var equipmentName = equipment.First().EquipmentName;

                    // Создаем словарь значений по датам
                    var valuesByDate = equipment.ToDictionary(x => x.ShiftInfo, x => x.FinalIndex);

                    // Формируем значения в правильном порядке по датам
                    var oeeValues = new ChartValues<int>();
                    foreach (var date in allShiftDates)
                    {
                        oeeValues.Add(valuesByDate.ContainsKey(date) ? valuesByDate[date] : 0);
                    }

                    var lineColor = colors[colorIndex % colors.Length];
                    legendItems[equipmentName] = lineColor;

                    var lineSeries = new LineSeries
                    {
                        Title = equipmentName,
                        Values = oeeValues,
                        PointGeometrySize = 8,
                        StrokeThickness = 2,
                        Stroke = lineColor,
                        Fill = Brushes.Transparent,
                        PointGeometry = DefaultGeometries.Circle
                    };

                    MainChart.Series.Add(lineSeries);
                    colorIndex++;
                }

                if (MainChart.AxisX != null && MainChart.AxisX.Count > 0)
                {
                    MainChart.AxisX[0].Labels = allShiftDates;
                    MainChart.AxisX[0].Title = "Дата смены";
                }

                UpdateLegend(legendItems);

                BarChartButton.IsEnabled = true;
                LineChartButton.IsEnabled = false;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при построении диаграммы: {ex.Message}", "Ошибка",
                               MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BarChartButton_Click(object sender, RoutedEventArgs e) => DrawBarChart();
        private void LineChartButton_Click(object sender, RoutedEventArgs e) => DrawLineChart();
        private void CloseButton_Click(object sender, RoutedEventArgs e) => Close();
    }
}