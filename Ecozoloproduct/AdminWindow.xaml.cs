using System.Windows;

namespace WpfApp2
{
    public partial class AdminWindow : Window
    {
        private string currentUser;

        public AdminWindow(string userName)
        {
            InitializeComponent();
            currentUser = userName;
            this.Title = $"Панель администратора - {userName}";
        }

        private void UsersButton_Click(object sender, RoutedEventArgs e)
        {
            UsersWindow usersWindow = new UsersWindow();
            usersWindow.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            usersWindow.WindowState = WindowState.Maximized;
            usersWindow.Show();
            this.Close();
        }

        private void RolesButton_Click(object sender, RoutedEventArgs e)
        {
            RoleWindow roleWindow = new RoleWindow();
            roleWindow.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            roleWindow.WindowState = WindowState.Maximized;
            roleWindow.Show();
            this.Close();
        }

        private void StatusButton_Click(object sender, RoutedEventArgs e)
        {
            StatusWindow statusWindow = new StatusWindow("Администратор", currentUser);
            statusWindow.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            statusWindow.WindowState = WindowState.Maximized;
            statusWindow.Show();
            this.Close();
        }

        private void EquipmentButton_Click(object sender, RoutedEventArgs e)
        {
            EquipmentWindow equipmentWindow = new EquipmentWindow("Администратор", currentUser);
            equipmentWindow.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            equipmentWindow.WindowState = WindowState.Maximized;
            equipmentWindow.Show();
            this.Close();
        }

        
        private void ShiftsButton_Click(object sender, RoutedEventArgs e)
        {
            ShiftWindow shiftWindow = new ShiftWindow("Администратор", currentUser);
            shiftWindow.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            shiftWindow.WindowState = WindowState.Maximized;
            shiftWindow.Show();
            this.Close();
        }

        private void ShiftDetailsButton_Click(object sender, RoutedEventArgs e)
        {
            ShiftDetailsWindow shiftDetailsWindow = new ShiftDetailsWindow("Администратор", currentUser);
            shiftDetailsWindow.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            shiftDetailsWindow.WindowState = WindowState.Maximized;
            shiftDetailsWindow.Show();
            this.Close();
        }

        private void DowntimeRecordButton_Click(object sender, RoutedEventArgs e)
        {
            DowntimeRecordWindow downtimeRecordWindow = new DowntimeRecordWindow("Администратор", currentUser);
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
                MainWindow mainWindow = new MainWindow();
                mainWindow.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                mainWindow.WindowState = WindowState.Maximized;
                mainWindow.Show();
                this.Close();
            }
        }
    }
}