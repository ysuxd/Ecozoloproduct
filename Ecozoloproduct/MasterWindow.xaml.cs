using System.Windows;

namespace WpfApp2
{
    public partial class MasterWindow : Window
    {
        private string currentUser;

        public MasterWindow(string userName)
        {
            InitializeComponent();
            currentUser = userName;
            this.Title = $"Панель мастера смены - {userName}";
        }

        private void EquipmentButton_Click(object sender, RoutedEventArgs e)
        {
            EquipmentWindow equipmentWindow = new EquipmentWindow("Мастер смены", currentUser);
            equipmentWindow.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            equipmentWindow.WindowState = WindowState.Maximized;
            equipmentWindow.Show();
            this.Close();
        }

        private void ShiftsButton_Click(object sender, RoutedEventArgs e)
        {
            ShiftWindow shiftWindow = new ShiftWindow("Мастер смены", currentUser);
            shiftWindow.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            shiftWindow.WindowState = WindowState.Maximized;
            shiftWindow.Show();
            this.Close();
        }

        private void ShiftDetailsButton_Click(object sender, RoutedEventArgs e)
        {
            ShiftDetailsWindow shiftDetailsWindow = new ShiftDetailsWindow("Мастер смены", currentUser);
            shiftDetailsWindow.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            shiftDetailsWindow.WindowState = WindowState.Maximized;
            shiftDetailsWindow.Show();
            this.Close();
        }

        private void DowntimeRecordButton_Click(object sender, RoutedEventArgs e)
        {
            DowntimeRecordWindow downtimeRecordWindow = new DowntimeRecordWindow("Мастер смены", currentUser);
            downtimeRecordWindow.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            downtimeRecordWindow.WindowState = WindowState.Maximized;
            downtimeRecordWindow.Show();
            this.Close();
        }

        private void EfficiencyButton_Click(object sender, RoutedEventArgs e)
        {
            EfficiencyWindow efficiencyWindow = new EfficiencyWindow("Мастер смены", currentUser);
            efficiencyWindow.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            efficiencyWindow.WindowState = WindowState.Maximized;
            efficiencyWindow.Show();
            this.Close();
        }

        private void ExitButton_Click(object sender, RoutedEventArgs e)
        {
            MessageBoxResult result = MessageBox.Show("Вы уверены, что хотите выйти?",
                "Выход", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                LoginWindow loginWindow = new LoginWindow();
                loginWindow.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                loginWindow.WindowState = WindowState.Maximized;
                loginWindow.Show();
                this.Close();
            }
        }
    }
}