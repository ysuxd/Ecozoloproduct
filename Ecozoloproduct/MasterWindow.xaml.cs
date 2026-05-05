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
            WelcomeTextBlock.Text = $"Добро пожаловать, {userName}!";
            this.Title = $"Панель мастера смены - {userName}";
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