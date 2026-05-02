using Npgsql;
using System;
using System.Windows;
using WpfApp.Database;

namespace WpfApp2
{
    public partial class UserWindow : Window
    {
        private string currentUser;
        private DatabaseConnection dbconnection = new DatabaseConnection();

        public UserWindow(string userName)
        {
            InitializeComponent();
            currentUser = userName;
            this.Title = $"Личный кабинет - {userName}";
            
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