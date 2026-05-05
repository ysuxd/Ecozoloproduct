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
            WelcomeTextBlock.Text = $"Добро пожаловать, {userName}!";
            this.Title = $"Панель оператора - {userName}";
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