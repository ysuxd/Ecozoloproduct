using System.Windows;

namespace WpfApp2
{
    public partial class OperatorWindow : Window
    {
        private string currentUser;

        public OperatorWindow(string userName)
        {
            InitializeComponent();
            currentUser = userName;
            this.Title = $"Панель оператора - {userName}";
        }

        private void EquipmentButton_Click(object sender, RoutedEventArgs e)
        {
            EquipmentWindow equipmentWindow = new EquipmentWindow("Оператор", currentUser);
            equipmentWindow.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            equipmentWindow.WindowState = WindowState.Maximized;
            equipmentWindow.Show();
            this.Close();
        }

        private void StatusButton_Click(object sender, RoutedEventArgs e)
        {
            StatusWindow statusWindow = new StatusWindow("Оператор", currentUser);
            statusWindow.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            statusWindow.WindowState = WindowState.Maximized;
            statusWindow.Show();
            this.Close();
        }

        private void DowntimeRecordButton_Click(object sender, RoutedEventArgs e)
        {
            DowntimeRecordWindow downtimeRecordWindow = new DowntimeRecordWindow("Оператор", currentUser);
            downtimeRecordWindow.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            downtimeRecordWindow.WindowState = WindowState.Maximized;
            downtimeRecordWindow.Show();
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