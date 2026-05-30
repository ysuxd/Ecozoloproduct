using Ecozoloproduct;
using Npgsql;
using System;
using System.Windows;
using System.Windows.Input;
using WpfApp.Database;

namespace WpfApp2
{
    public partial class LoginWindow : Window
    {
        private DatabaseConnection dbconnection = new DatabaseConnection();
        private string currentCaptcha = "";

        public LoginWindow()
        {
            InitializeComponent();

            LoginTextBox.KeyDown += (s, e) => { if (e.Key == Key.Enter) LoginButton_Click(s, e); };
            PasswordBox.KeyDown += (s, e) => { if (e.Key == Key.Enter) LoginButton_Click(s, e); };
            CaptchaTextBox.KeyDown += (s, e) => { if (e.Key == Key.Enter) LoginButton_Click(s, e); };
            this.KeyDown += (s, e) => { if (e.Key == Key.Escape) Close(); };

            GenerateCaptcha();
        }

        // Генерация случайной капчи (с учетом регистра)
        private void GenerateCaptcha()
        {
            Random rand = new Random();
            string chars = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnpqrstuvwxyz0123456789";
            char[] captchaChars = new char[5];

            for (int i = 0; i < 5; i++)
            {
                captchaChars[i] = chars[rand.Next(chars.Length)];
            }

            currentCaptcha = new string(captchaChars);
            CaptchaTextBlock.Text = currentCaptcha;
        }

        private void RefreshCaptchaButton_Click(object sender, RoutedEventArgs e)
        {
            GenerateCaptcha();
            CaptchaTextBox.Text = "";
        }

        private void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            string login = LoginTextBox.Text.Trim();
            string password = PasswordBox.Password;
            string captcha = CaptchaTextBox.Text.Trim();

            if (string.IsNullOrWhiteSpace(login) || string.IsNullOrWhiteSpace(password))
            {
                ShowError("Введите логин и пароль");
                return;
            }

            // Проверка капчи (с учетом регистра)
            if (string.IsNullOrWhiteSpace(captcha) || captcha != currentCaptcha)
            {
                ShowError("Неверный код с картинки");
                GenerateCaptcha();
                CaptchaTextBox.Text = "";
                return;
            }

            using (var connection = dbconnection.GetConnection())
            {
                try
                {
                    connection.Open();

                    string checkUserQuery = @"
                        SELECT u.userid, u.login, u.password, u.isblocked, u.attempts, r.rolename 
                        FROM users u
                        LEFT JOIN role r ON u.roleid = r.roleid
                        WHERE u.login = @login";

                    using (var checkCmd = new NpgsqlCommand(checkUserQuery, connection))
                    {
                        checkCmd.Parameters.AddWithValue("@login", login);
                        using (var reader = checkCmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                int userId = reader.GetInt32(0);
                                string userLogin = reader.GetString(1);
                                string dbPassword = reader.GetString(2);
                                bool isBlocked = reader.GetBoolean(3);
                                int attempts = reader.IsDBNull(4) ? 0 : reader.GetInt32(4);
                                string role = reader.IsDBNull(5) ? "Пользователь" : reader.GetString(5);

                                if (isBlocked)
                                {
                                    ShowError("Ваш аккаунт заблокирован. Обратитесь к администратору.");
                                    return;
                                }

                                if (dbPassword == password)
                                {
                                    ResetAttempts(userId);
                                    UpdateLastLoginDate(userId);
                                    this.Hide();

                                    // Открываем окно в зависимости от роли
                                    switch (role)
                                    {
                                        case "Администратор":
                                            AdminWindow adminWindow = new AdminWindow(userLogin);
                                            adminWindow.Closed += (s, args) => this.Close();
                                            adminWindow.Show();
                                            break;

                                        case "Мастер смены":
                                            MasterWindow masterWindow = new MasterWindow(userLogin);
                                            masterWindow.Closed += (s, args) => this.Close();
                                            masterWindow.Show();
                                            break;

                                        case "Технолог":
                                            TechnologistWindow technologistWindow = new TechnologistWindow(userLogin);
                                            technologistWindow.Closed += (s, args) => this.Close();
                                            technologistWindow.Show();
                                            break;

                                        case "Оператор оборудования":
                                            OperatorWindow operatorWindow = new OperatorWindow(userLogin);
                                            operatorWindow.Closed += (s, args) => this.Close();
                                            operatorWindow.Show();
                                            break;

                                    }
                                }
                                else
                                {
                                    if (role != "Администратор")
                                    {
                                        int newAttempts = attempts + 1;
                                        UpdateAttempts(userId, newAttempts);

                                        if (newAttempts >= 3)
                                        {
                                            BlockUser(userId);
                                            ShowError("Вы превысили количество попыток входа. Аккаунт заблокирован.");
                                        }
                                        else
                                        {
                                            ShowError($"Неверный логин или пароль. Осталось попыток: {3 - newAttempts}");
                                            PasswordBox.Password = "";
                                        }
                                    }
                                    else
                                    {
                                        ShowError("Неверный логин или пароль");
                                        PasswordBox.Password = "";
                                    }

                                    GenerateCaptcha();
                                    CaptchaTextBox.Text = "";
                                }
                            }
                            else
                            {
                                ShowError("Неверный логин или пароль");
                                PasswordBox.Password = "";
                                GenerateCaptcha();
                                CaptchaTextBox.Text = "";
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

        private void UpdateAttempts(int userId, int attempts)
        {
            using (var connection = dbconnection.GetConnection())
            {
                try
                {
                    connection.Open();
                    string query = "UPDATE users SET attempts = @attempts WHERE userid = @userid";
                    using (var cmd = new NpgsqlCommand(query, connection))
                    {
                        cmd.Parameters.AddWithValue("@attempts", attempts);
                        cmd.Parameters.AddWithValue("@userid", userId);
                        cmd.ExecuteNonQuery();
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Ошибка обновления попыток: {ex.Message}");
                }
            }
        }

        private void ResetAttempts(int userId)
        {
            using (var connection = dbconnection.GetConnection())
            {
                try
                {
                    connection.Open();
                    string query = "UPDATE users SET attempts = 0 WHERE userid = @userid";
                    using (var cmd = new NpgsqlCommand(query, connection))
                    {
                        cmd.Parameters.AddWithValue("@userid", userId);
                        cmd.ExecuteNonQuery();
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Ошибка сброса попыток: {ex.Message}");
                }
            }
        }

        private void BlockUser(int userId)
        {
            using (var connection = dbconnection.GetConnection())
            {
                try
                {
                    connection.Open();
                    string query = "UPDATE users SET isblocked = TRUE, attempts = 3 WHERE userid = @userid";
                    using (var cmd = new NpgsqlCommand(query, connection))
                    {
                        cmd.Parameters.AddWithValue("@userid", userId);
                        cmd.ExecuteNonQuery();
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Ошибка блокировки пользователя: {ex.Message}");
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