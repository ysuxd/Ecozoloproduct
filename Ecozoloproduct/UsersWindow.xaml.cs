using Npgsql;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using WpfApp.Database;

namespace WpfApp2
{
    public partial class UsersWindow : Window
    {
        private DatabaseConnection dbconnection = new DatabaseConnection();

        public class User
        {
            public int UserId { get; set; }
            public string Login { get; set; }
            public string Password { get; set; }
            public string Role { get; set; }
            public bool IsBlocked { get; set; }
            public DateTime? LastLoginDate { get; set; }
        }

        public UsersWindow()
        {
            InitializeComponent();
            LoadRolesForComboBox();
            LoadUsers();
        }

        // Загрузка ролей для ComboBox из таблицы role
        private void LoadRolesForComboBox()
        {
            using (var connection = dbconnection.GetConnection())
            {
                try
                {
                    connection.Open();
                    string query = "SELECT rolename FROM role ORDER BY roleid";
                    using (var cmd = new NpgsqlCommand(query, connection))
                    using (var reader = cmd.ExecuteReader())
                    {
                        RoleBox.Items.Clear();
                        while (reader.Read())
                        {
                            RoleBox.Items.Add(new ComboBoxItem { Content = reader.GetString(0) });
                        }
                        if (RoleBox.Items.Count > 0)
                            RoleBox.SelectedIndex = 0;
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка загрузки ролей: {ex.Message}", "Ошибка",
                                   MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void LoadUsers()
        {
            using (var connection = dbconnection.GetConnection())
            {
                try
                {
                    connection.Open();
                    // Используем LEFT JOIN для получения названия роли из таблицы role
                    string query = @"
                        SELECT u.userid, u.login, u.password, r.rolename, u.isblocked, u.lastlogindate 
                        FROM users u
                        LEFT JOIN role r ON u.roleid = r.roleid
                        ORDER BY u.userid";

                    using (var cmd = new NpgsqlCommand(query, connection))
                    using (var reader = cmd.ExecuteReader())
                    {
                        List<User> users = new List<User>();
                        while (reader.Read())
                        {
                            users.Add(new User
                            {
                                UserId = reader.GetInt32(0),
                                Login = reader.GetString(1),
                                Password = reader.IsDBNull(2) ? string.Empty : reader.GetString(2),
                                Role = reader.IsDBNull(3) ? "Не указана" : reader.GetString(3),
                                IsBlocked = reader.GetBoolean(4),
                                LastLoginDate = reader.IsDBNull(5) ? (DateTime?)null : reader.GetDateTime(5)
                            });
                        }
                        UsersDataGrid.ItemsSource = users;
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка загрузки пользователей: {ex.Message}", "Ошибка",
                                   MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            string login = LoginTextBox.Text.Trim();
            string password = PassBox.Password;
            string role = (RoleBox.SelectedItem as ComboBoxItem)?.Content.ToString();

            if (string.IsNullOrWhiteSpace(login) || string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(role))
            {
                MessageBox.Show("Заполните все поля", "Предупреждение",
                               MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            using (var connection = dbconnection.GetConnection())
            {
                try
                {
                    connection.Open();

                    // Получаем roleid из таблицы role
                    string getRoleIdQuery = "SELECT roleid FROM role WHERE rolename = @rolename";
                    int roleId;
                    using (var getRoleCmd = new NpgsqlCommand(getRoleIdQuery, connection))
                    {
                        getRoleCmd.Parameters.AddWithValue("@rolename", role);
                        var result = getRoleCmd.ExecuteScalar();
                        if (result == null)
                        {
                            MessageBox.Show("Выбранная роль не существует", "Ошибка",
                                           MessageBoxButton.OK, MessageBoxImage.Error);
                            return;
                        }
                        roleId = Convert.ToInt32(result);
                    }

                    // Проверка на существование пользователя
                    string checkQuery = "SELECT COUNT(*) FROM users WHERE login = @login";
                    using (var checkcmd = new NpgsqlCommand(checkQuery, connection))
                    {
                        checkcmd.Parameters.AddWithValue("@login", login);
                        int count = Convert.ToInt32(checkcmd.ExecuteScalar());
                        if (count > 0)
                        {
                            MessageBox.Show("Пользователь с таким логином уже существует", "Предупреждение",
                                           MessageBoxButton.OK, MessageBoxImage.Warning);
                            return;
                        }
                    }

                    // Добавление пользователя
                    string addQuery = "INSERT INTO users (login, password, roleid, isblocked, attempts) VALUES (@login, @password, @roleid, FALSE, 0)";
                    using (var addcmd = new NpgsqlCommand(addQuery, connection))
                    {
                        addcmd.Parameters.AddWithValue("@login", login);
                        addcmd.Parameters.AddWithValue("@password", password);
                        addcmd.Parameters.AddWithValue("@roleid", roleId);
                        addcmd.ExecuteNonQuery();
                    }

                    MessageBox.Show("Пользователь успешно добавлен", "Успех",
                                   MessageBoxButton.OK, MessageBoxImage.Information);
                    ClearFields();
                    LoadUsers();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка добавления пользователя: {ex.Message}", "Ошибка",
                                   MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void UpdateButton_Click(object sender, RoutedEventArgs e)
        {
            if (UsersDataGrid.SelectedItem is User selectedUser)
            {
                string password = PassBox.Password;
                string role = (RoleBox.SelectedItem as ComboBoxItem)?.Content.ToString();

                if (string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(role))
                {
                    MessageBox.Show("Заполните пароль и роль", "Предупреждение",
                                   MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                using (var connection = dbconnection.GetConnection())
                {
                    try
                    {
                        connection.Open();

                        // Получаем roleid из таблицы role
                        string getRoleIdQuery = "SELECT roleid FROM role WHERE rolename = @rolename";
                        int roleId;
                        using (var getRoleCmd = new NpgsqlCommand(getRoleIdQuery, connection))
                        {
                            getRoleCmd.Parameters.AddWithValue("@rolename", role);
                            var result = getRoleCmd.ExecuteScalar();
                            if (result == null)
                            {
                                MessageBox.Show("Выбранная роль не существует", "Ошибка",
                                               MessageBoxButton.OK, MessageBoxImage.Error);
                                return;
                            }
                            roleId = Convert.ToInt32(result);
                        }

                        string query = "UPDATE users SET password = @password, roleid = @roleid, isblocked = @isBlocked WHERE userid = @userId";
                        using (var cmd = new NpgsqlCommand(query, connection))
                        {
                            cmd.Parameters.AddWithValue("@password", password);
                            cmd.Parameters.AddWithValue("@roleid", roleId);
                            cmd.Parameters.AddWithValue("@isBlocked", selectedUser.IsBlocked);
                            cmd.Parameters.AddWithValue("@userId", selectedUser.UserId);
                            cmd.ExecuteNonQuery();
                        }
                        MessageBox.Show("Данные успешно изменены", "Успех",
                                       MessageBoxButton.OK, MessageBoxImage.Information);
                        ClearFields();
                        LoadUsers();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Ошибка изменения данных: {ex.Message}", "Ошибка",
                                       MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            else
            {
                MessageBox.Show("Выберите пользователя для обновления", "Предупреждение",
                               MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            if (UsersDataGrid.SelectedItem is User selectedUser)
            {
                MessageBoxResult result = MessageBox.Show($"Удалить пользователя '{selectedUser.Login}'?",
                    "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    using (var connection = dbconnection.GetConnection())
                    {
                        try
                        {
                            connection.Open();
                            string query = "DELETE FROM users WHERE userid = @userId";
                            using (var cmd = new NpgsqlCommand(query, connection))
                            {
                                cmd.Parameters.AddWithValue("@userId", selectedUser.UserId);
                                cmd.ExecuteNonQuery();
                            }
                            MessageBox.Show("Пользователь успешно удален", "Успех",
                                           MessageBoxButton.OK, MessageBoxImage.Information);
                            ClearFields();
                            LoadUsers();
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show($"Ошибка удаления пользователя: {ex.Message}", "Ошибка",
                                           MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                }
            }
            else
            {
                MessageBox.Show("Выберите пользователя для удаления", "Предупреждение",
                               MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void BlockButton_Click(object sender, RoutedEventArgs e)
        {
            if (UsersDataGrid.SelectedItem is User selectedUser)
            {
                if (selectedUser.IsBlocked)
                {
                    MessageBox.Show("Пользователь уже заблокирован", "Информация",
                                   MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                MessageBoxResult result = MessageBox.Show($"Блокировать пользователя '{selectedUser.Login}'?",
                    "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    using (var connection = dbconnection.GetConnection())
                    {
                        try
                        {
                            connection.Open();
                            string query = "UPDATE users SET isblocked = TRUE, attempts = 3 WHERE userid = @userId";
                            using (var cmd = new NpgsqlCommand(query, connection))
                            {
                                cmd.Parameters.AddWithValue("@userId", selectedUser.UserId);
                                cmd.ExecuteNonQuery();
                            }
                            MessageBox.Show("Пользователь заблокирован", "Успех",
                                           MessageBoxButton.OK, MessageBoxImage.Information);
                            LoadUsers();
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show($"Ошибка блокировки пользователя: {ex.Message}", "Ошибка",
                                           MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                }
            }
            else
            {
                MessageBox.Show("Выберите пользователя для блокировки", "Предупреждение",
                               MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void UnblockButton_Click(object sender, RoutedEventArgs e)
        {
            if (UsersDataGrid.SelectedItem is User selectedUser)
            {
                if (!selectedUser.IsBlocked)
                {
                    MessageBox.Show("Пользователь не заблокирован", "Информация",
                                   MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                MessageBoxResult result = MessageBox.Show($"Разблокировать пользователя '{selectedUser.Login}'?",
                    "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    using (var connection = dbconnection.GetConnection())
                    {
                        try
                        {
                            connection.Open();
                            string query = "UPDATE users SET isblocked = FALSE, attempts = 0 WHERE userid = @userId";
                            using (var cmd = new NpgsqlCommand(query, connection))
                            {
                                cmd.Parameters.AddWithValue("@userId", selectedUser.UserId);
                                cmd.ExecuteNonQuery();
                            }
                            MessageBox.Show("Пользователь разблокирован", "Успех",
                                           MessageBoxButton.OK, MessageBoxImage.Information);
                            LoadUsers();
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show($"Ошибка разблокировки пользователя: {ex.Message}", "Ошибка",
                                           MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                }
            }
            else
            {
                MessageBox.Show("Выберите пользователя для разблокировки", "Предупреждение",
                               MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            LoadRolesForComboBox();
            LoadUsers();
            ClearFields();
        }

        private void ClearFields()
        {
            LoginTextBox.Text = string.Empty;
            PassBox.Password = string.Empty;
            if (RoleBox.Items.Count > 0)
                RoleBox.SelectedIndex = 0;
        }

        private void UsersWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // Уже загружено в конструкторе
        }

        // При выборе строки в DataGrid заполняем поля для редактирования
        private void UsersDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (UsersDataGrid.SelectedItem is User selectedUser)
            {
                LoginTextBox.Text = selectedUser.Login;
                PassBox.Password = selectedUser.Password;
                LoginTextBox.IsEnabled = false; // Блокируем изменение логина при редактировании

                // Установка роли в ComboBox
                for (int i = 0; i < RoleBox.Items.Count; i++)
                {
                    var item = RoleBox.Items[i] as ComboBoxItem;
                    if (item != null && item.Content.ToString() == selectedUser.Role)
                    {
                        RoleBox.SelectedIndex = i;
                        break;
                    }
                }
            }
            else
            {
                LoginTextBox.IsEnabled = true;
                LoginTextBox.Text = string.Empty;
                PassBox.Password = string.Empty;
            }
        }
    }
}