using Npgsql;
using System;
using System.Windows;
using System.Windows.Controls;
using WpfApp.Database;

namespace WpfApp2
{
    public partial class RegistrationWindow : Window
    {
        private DatabaseConnection dbconnection = new DatabaseConnection();

        public RegistrationWindow()
        {
            InitializeComponent();
            LoadRoles();
        }

        private void LoadRoles()
        {
            using (var connection = dbconnection.GetConnection())
            {
                try
                {
                    connection.Open();
                    // Загружаем ВСЕ роли из БД
                    string query = "SELECT roleid, rolename FROM role ORDER BY roleid";

                    using (var cmd = new NpgsqlCommand(query, connection))
                    using (var reader = cmd.ExecuteReader())
                    {
                        RoleComboBox.Items.Clear();

                        while (reader.Read())
                        {
                            int roleId = reader.GetInt32(0);
                            string roleName = reader.GetString(1);

                            // Создаем ComboBoxItem с правильным содержимым
                            ComboBoxItem item = new ComboBoxItem();
                            item.Content = roleName;
                            item.Tag = roleId; // Сохраняем ID роли в Tag
                            RoleComboBox.Items.Add(item);
                        }

                        // Если нет ролей в БД, добавляем стандартные
                        if (RoleComboBox.Items.Count == 0)
                        {
                            ComboBoxItem adminItem = new ComboBoxItem();
                            adminItem.Content = "Администратор";
                            adminItem.Tag = 1;
                            RoleComboBox.Items.Add(adminItem);

                            ComboBoxItem userItem = new ComboBoxItem();
                            userItem.Content = "Пользователь";
                            userItem.Tag = 2;
                            RoleComboBox.Items.Add(userItem);
                        }

                        RoleComboBox.SelectedIndex = 0;

                        // Для отладки - выводим количество загруженных ролей
                        System.Diagnostics.Debug.WriteLine($"Загружено ролей: {RoleComboBox.Items.Count}");
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Ошибка загрузки ролей: {ex.Message}");
                    // В случае ошибки добавляем стандартные роли
                    RoleComboBox.Items.Clear();

                    ComboBoxItem adminItem = new ComboBoxItem();
                    adminItem.Content = "Администратор";
                    adminItem.Tag = 1;
                    RoleComboBox.Items.Add(adminItem);

                    ComboBoxItem userItem = new ComboBoxItem();
                    userItem.Content = "Пользователь";
                    userItem.Tag = 2;
                    RoleComboBox.Items.Add(userItem);

                    ComboBoxItem modItem = new ComboBoxItem();
                    modItem.Content = "Модератор";
                    modItem.Tag = 3;
                    RoleComboBox.Items.Add(modItem);

                    RoleComboBox.SelectedIndex = 0;
                }
            }
        }

        private void RegisterButton_Click(object sender, RoutedEventArgs e)
        {
            string login = LoginTextBox.Text.Trim();
            string password = PasswordBox.Password;
            string confirmPassword = ConfirmPasswordBox.Password;

            // Получаем выбранную роль из ComboBox
            if (RoleComboBox.SelectedItem == null)
            {
                ShowError("Выберите роль");
                return;
            }

            ComboBoxItem selectedRole = (ComboBoxItem)RoleComboBox.SelectedItem;
            string role = selectedRole.Content.ToString();

            // Валидация
            if (string.IsNullOrWhiteSpace(login))
            {
                ShowError("Введите логин");
                return;
            }

            if (login.Length < 3)
            {
                ShowError("Логин должен содержать не менее 3 символов");
                return;
            }

            if (string.IsNullOrWhiteSpace(password))
            {
                ShowError("Введите пароль");
                return;
            }

            if (password.Length < 4)
            {
                ShowError("Пароль должен содержать не менее 4 символов");
                return;
            }

            if (password != confirmPassword)
            {
                ShowError("Пароли не совпадают");
                return;
            }

            using (var connection = dbconnection.GetConnection())
            {
                try
                {
                    connection.Open();

                    // Проверка существующего пользователя
                    string checkQuery = "SELECT COUNT(*) FROM users WHERE login = @login";
                    using (var checkCmd = new NpgsqlCommand(checkQuery, connection))
                    {
                        checkCmd.Parameters.AddWithValue("@login", login);
                        int count = Convert.ToInt32(checkCmd.ExecuteScalar());
                        if (count > 0)
                        {
                            ShowError("Пользователь с таким логином уже существует");
                            return;
                        }
                    }

                    // Получаем roleid из таблицы role по названию
                    string getRoleIdQuery = "SELECT roleid FROM role WHERE rolename = @rolename";
                    int roleId;
                    using (var getRoleCmd = new NpgsqlCommand(getRoleIdQuery, connection))
                    {
                        getRoleCmd.Parameters.AddWithValue("@rolename", role);
                        var result = getRoleCmd.ExecuteScalar();
                        if (result == null)
                        {
                            ShowError("Выбранная роль не существует");
                            return;
                        }
                        roleId = Convert.ToInt32(result);
                    }

                    // Добавление пользователя
                    string insertQuery = "INSERT INTO users (login, password, roleid, isblocked) VALUES (@login, @password, @roleid, FALSE)";
                    using (var insertCmd = new NpgsqlCommand(insertQuery, connection))
                    {
                        insertCmd.Parameters.AddWithValue("@login", login);
                        insertCmd.Parameters.AddWithValue("@password", password);
                        insertCmd.Parameters.AddWithValue("@roleid", roleId);
                        insertCmd.ExecuteNonQuery();
                    }

                    ShowSuccess("Регистрация прошла успешно!");

                    var timer = new System.Windows.Threading.DispatcherTimer();
                    timer.Interval = TimeSpan.FromSeconds(1.5);
                    timer.Tick += (s, args) =>
                    {
                        timer.Stop();
                        this.DialogResult = true;
                        this.Close();
                    };
                    timer.Start();
                }
                catch (Exception ex)
                {
                    ShowError($"Ошибка регистрации: {ex.Message}");
                }
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }

        private void ShowError(string message)
        {
            ErrorTextBlock.Text = message;
            ErrorTextBlock.Visibility = Visibility.Visible;
            SuccessTextBlock.Visibility = Visibility.Collapsed;

            var timer = new System.Windows.Threading.DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(3);
            timer.Tick += (s, e) =>
            {
                ErrorTextBlock.Visibility = Visibility.Collapsed;
                timer.Stop();
            };
            timer.Start();
        }

        private void ShowSuccess(string message)
        {
            SuccessTextBlock.Text = message;
            SuccessTextBlock.Visibility = Visibility.Visible;
            ErrorTextBlock.Visibility = Visibility.Collapsed;
        }
    }
}