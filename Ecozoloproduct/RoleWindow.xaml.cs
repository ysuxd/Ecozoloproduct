using Npgsql;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using WpfApp.Database;

namespace WpfApp2
{
    public partial class RoleWindow : Window
    {
        private DatabaseConnection dbconnection = new DatabaseConnection();

        public class Role
        {
            public int RoleId { get; set; }
            public string RoleName { get; set; }
        }

        public RoleWindow()
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
                    string query = "SELECT roleid, rolename FROM role ORDER BY roleid";
                    using (var cmd = new NpgsqlCommand(query, connection))
                    using (var reader = cmd.ExecuteReader())
                    {
                        List<Role> roles = new List<Role>();
                        while (reader.Read())
                        {
                            roles.Add(new Role
                            {
                                RoleId = reader.GetInt32(0),
                                RoleName = reader.GetString(1)
                            });
                        }
                        RolesDataGrid.ItemsSource = roles;
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка загрузки ролей: {ex.Message}", "Ошибка",
                                   MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            string roleName = RoleNameTextBox.Text.Trim();

            if (string.IsNullOrWhiteSpace(roleName))
            {
                MessageBox.Show("Введите название роли", "Предупреждение",
                               MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            using (var connection = dbconnection.GetConnection())
            {
                try
                {
                    connection.Open();

                    string checkQuery = "SELECT COUNT(*) FROM role WHERE rolename = @rolename";
                    using (var checkCmd = new NpgsqlCommand(checkQuery, connection))
                    {
                        checkCmd.Parameters.AddWithValue("@rolename", roleName);
                        int count = Convert.ToInt32(checkCmd.ExecuteScalar());
                        if (count > 0)
                        {
                            MessageBox.Show("Роль с таким названием уже существует", "Предупреждение",
                                           MessageBoxButton.OK, MessageBoxImage.Warning);
                            return;
                        }
                    }

                    string addQuery = "INSERT INTO role (rolename) VALUES (@rolename)";
                    using (var addCmd = new NpgsqlCommand(addQuery, connection))
                    {
                        addCmd.Parameters.AddWithValue("@rolename", roleName);
                        addCmd.ExecuteNonQuery();
                    }

                    MessageBox.Show("Роль успешно добавлена", "Успех",
                                   MessageBoxButton.OK, MessageBoxImage.Information);
                    ClearFields();
                    LoadRoles();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка добавления роли: {ex.Message}", "Ошибка",
                                   MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void UpdateButton_Click(object sender, RoutedEventArgs e)
        {
            if (RolesDataGrid.SelectedItem is Role selectedRole)
            {
                string roleName = RoleNameTextBox.Text.Trim();

                if (string.IsNullOrWhiteSpace(roleName))
                {
                    MessageBox.Show("Введите название роли", "Предупреждение",
                                   MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                using (var connection = dbconnection.GetConnection())
                {
                    try
                    {
                        connection.Open();

                        string checkQuery = "SELECT COUNT(*) FROM role WHERE rolename = @rolename AND roleid != @roleid";
                        using (var checkCmd = new NpgsqlCommand(checkQuery, connection))
                        {
                            checkCmd.Parameters.AddWithValue("@rolename", roleName);
                            checkCmd.Parameters.AddWithValue("@roleid", selectedRole.RoleId);
                            int count = Convert.ToInt32(checkCmd.ExecuteScalar());
                            if (count > 0)
                            {
                                MessageBox.Show("Роль с таким названием уже существует", "Предупреждение",
                                               MessageBoxButton.OK, MessageBoxImage.Warning);
                                return;
                            }
                        }

                        string updateQuery = "UPDATE role SET rolename = @rolename WHERE roleid = @roleid";
                        using (var updateCmd = new NpgsqlCommand(updateQuery, connection))
                        {
                            updateCmd.Parameters.AddWithValue("@rolename", roleName);
                            updateCmd.Parameters.AddWithValue("@roleid", selectedRole.RoleId);
                            updateCmd.ExecuteNonQuery();
                        }

                        MessageBox.Show("Роль успешно обновлена", "Успех",
                                       MessageBoxButton.OK, MessageBoxImage.Information);
                        ClearFields();
                        LoadRoles();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Ошибка обновления роли: {ex.Message}", "Ошибка",
                                       MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            else
            {
                MessageBox.Show("Выберите роль для обновления", "Предупреждение",
                               MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            if (RolesDataGrid.SelectedItem is Role selectedRole)
            {
                MessageBoxResult result = MessageBox.Show($"Удалить роль '{selectedRole.RoleName}'?",
                    "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    using (var connection = dbconnection.GetConnection())
                    {
                        try
                        {
                            connection.Open();

                            string checkQuery = "SELECT COUNT(*) FROM users WHERE roleid = @roleid";
                            using (var checkCmd = new NpgsqlCommand(checkQuery, connection))
                            {
                                checkCmd.Parameters.AddWithValue("@roleid", selectedRole.RoleId);
                                int count = Convert.ToInt32(checkCmd.ExecuteScalar());
                                if (count > 0)
                                {
                                    MessageBox.Show("Невозможно удалить роль, так как она используется пользователями",
                                                   "Предупреждение", MessageBoxButton.OK, MessageBoxImage.Warning);
                                    return;
                                }
                            }

                            string deleteQuery = "DELETE FROM role WHERE roleid = @roleid";
                            using (var deleteCmd = new NpgsqlCommand(deleteQuery, connection))
                            {
                                deleteCmd.Parameters.AddWithValue("@roleid", selectedRole.RoleId);
                                deleteCmd.ExecuteNonQuery();
                            }

                            MessageBox.Show("Роль успешно удалена", "Успех",
                                           MessageBoxButton.OK, MessageBoxImage.Information);
                            ClearFields();
                            LoadRoles();
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show($"Ошибка удаления роли: {ex.Message}", "Ошибка",
                                           MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                }
            }
            else
            {
                MessageBox.Show("Выберите роль для удаления", "Предупреждение",
                               MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            LoadRoles();
            ClearFields();
        }

        private void ClearFields()
        {
            RoleNameTextBox.Text = string.Empty;
        }

        private void RoleWindow_Loaded(object sender, RoutedEventArgs e)
        {
            LoadRoles();
        }

        private void RolesDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (RolesDataGrid.SelectedItem is Role selectedRole)
            {
                RoleNameTextBox.Text = selectedRole.RoleName;
            }
        }

        private void ExitButton_Click(object sender, RoutedEventArgs e)
        {
            MessageBoxResult result = MessageBox.Show("Вы уверены, что хотите выйти?",
                "Выход", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                AdminWindow adminWindow = new AdminWindow("");
                adminWindow.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                adminWindow.WindowState = WindowState.Maximized;
                adminWindow.Show();
                this.Close();
            }
        }
    }
}