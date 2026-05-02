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
            usersWindow.Show();
        }

        private void RolesButton_Click(object sender, RoutedEventArgs e)
        {
            RoleWindow roleWindow = new RoleWindow();
            roleWindow.Show();
        }

        private void ExitButton_Click(object sender, RoutedEventArgs e)
        {
            MessageBoxResult result = MessageBox.Show("Вы уверены, что хотите выйти?",
                "Выход", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                MainWindow mainWindow = new MainWindow();
                mainWindow.Show();
                this.Close();
            }
        }
    }
}