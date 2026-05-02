using Npgsql;
using System;
using System.Windows;
using System.Windows.Input;
using WpfApp.Database;

namespace WpfApp2
{
    public partial class MainWindow : Window
    {
        private DatabaseConnection dbconnection = new DatabaseConnection();

        public MainWindow()
        {
            InitializeComponent();

            LoginTextBox.KeyDown += (s, e) => { if (e.Key == Key.Enter) LoginButton_Click(s, e); };
            PasswordBox.KeyDown += (s, e) => { if (e.Key == Key.Enter) LoginButton_Click(s, e); };
            this.KeyDown += (s, e) => { if (e.Key == Key.Escape) Close(); };
        }

        private void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            string login = LoginTextBox.Text.Trim();
            string password = PasswordBox.Password;

            if (string.IsNullOrWhiteSpace(login) || string.IsNullOrWhiteSpace(password))
            {
                ShowError("Введите логин и пароль");
                return;
            }

            using (var connection = dbconnection.GetConnection())
            {
                try
                {
                    connection.Open();

                    string query = @"
                        SELECT u.userid, u.login, u.password, u.isblocked, r.rolename 
                        FROM users u
                        LEFT JOIN role r ON u.roleid = r.roleid
                        WHERE u.login = @login AND u.password = @password";

                    using (var cmd = new NpgsqlCommand(query, connection))
                    {
                        cmd.Parameters.AddWithValue("@login", login);
                        cmd.Parameters.AddWithValue("@password", password);

                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                int userId = reader.GetInt32(0);
                                string userLogin = reader.GetString(1);
                                bool isBlocked = reader.GetBoolean(3);
                                string role = reader.IsDBNull(4) ? "Пользователь" : reader.GetString(4);

                                if (isBlocked)
                                {
                                    ShowError("Ваш аккаунт заблокирован. Обратитесь к администратору.");
                                    return;
                                }

                                UpdateLastLoginDate(userId);
                                this.Hide();

                                if (role == "Администратор")
                                {
                                    AdminWindow adminWindow = new AdminWindow(userLogin);
                                    adminWindow.Closed += (s, args) => this.Close();
                                    adminWindow.Show();
                                }
                                else
                                {
                                    UserWindow userWindow = new UserWindow(userLogin);
                                    userWindow.Closed += (s, args) => this.Close();
                                    userWindow.Show();
                                }
                            }
                            else
                            {
                                ShowError("Неверный логин или пароль");
                                PasswordBox.Password = "";
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    ShowError($"Ошибка авторизации: {ex.Message}");
                }
            }
        }

        private void RegisterButton_Click(object sender, RoutedEventArgs e)
        {
            RegistrationWindow registrationWindow = new RegistrationWindow();
            registrationWindow.Owner = this;
            registrationWindow.ShowDialog();
        }

        private void UpdateLastLoginDate(int userId)
        {
            using (var connection = dbconnection.GetConnection())
            {
                try
                {
                    connection.Open();
                    string query = "UPDATE users SET lastlogindate = @lastlogindate WHERE userid = @userid";
                    using (var cmd = new NpgsqlCommand(query, connection))
                    {
                        cmd.Parameters.AddWithValue("@lastlogindate", DateTime.Now.Date);
                        cmd.Parameters.AddWithValue("@userid", userId);
                        cmd.ExecuteNonQuery();
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Ошибка обновления даты входа: {ex.Message}");
                }
            }
        }

        private void ShowError(string message)
        {
            ErrorTextBlock.Text = message;
            ErrorTextBlock.Visibility = Visibility.Visible;

            var timer = new System.Windows.Threading.DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(3);
            timer.Tick += (s, e) =>
            {
                ErrorTextBlock.Visibility = Visibility.Collapsed;
                timer.Stop();
            };
            timer.Start();
        }
    }
}