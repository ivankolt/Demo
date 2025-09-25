using Npgsql;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Demo
{
    /// <summary>
    /// Логика взаимодействия для Authentication.xaml
    /// </summary>
    public partial class Authentication : Window
    {
        public Authentication()
        {
            InitializeComponent();
        }
        public int? GetUserIdByLogin(string Login)
        {
            using (var conn = new NpgsqlConnection(Products_is_true.Connecting))
            {
                conn.Open();
                using (var cmd = new NpgsqlCommand("SELECT id FROM users WHERE username = @Login", conn))
                {
                    cmd.Parameters.AddWithValue("Login", Login);
                    var result = cmd.ExecuteScalar();
                    Console.WriteLine(result);
                    if (result != null && result != DBNull.Value)
                        return Convert.ToInt32(result);
                    else
                        return null;
                }
            }
        }

        public bool IsPassword(int? IdUsers, string Password)
        {
            using (var conn = new NpgsqlConnection(Products_is_true.Connecting))
            {
                conn.Open();
                using (var cmd = new NpgsqlCommand(
                    "SELECT id FROM users WHERE id = @idUsers AND password_hash = crypt(@Password, password_hash)", conn))
                {
                    cmd.Parameters.AddWithValue("idUsers", IdUsers);
                    cmd.Parameters.AddWithValue("Password", Password);
                    var result = cmd.ExecuteScalar();
                    if (result != null && result != DBNull.Value)
                    {
                        return true;
                    }
                    else
                    {
                        MessageBox.Show("Пароль не верный", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Information);
                        return false;
                    }
                }
            }
        }

        public string RoleUsers(int? idUSers)
        {
            if (IsPassword(idUSers, Password.Text))
            {
                using (var conn = new NpgsqlConnection(Products_is_true.Connecting))
                {
                    conn.Open();
                    using (var cmd = new NpgsqlCommand("SELECT role FROM users WHERE id = @idUSers", conn))
                    {
                        cmd.Parameters.AddWithValue("idUSers", idUSers);
                        var result = cmd.ExecuteScalar();
                        if (result != null && result != DBNull.Value)
                            return (string)result;
                    }
                }
            }
            return null;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (Login.Text != null && Password.Text != null)
            {
                int? idUsers = GetUserIdByLogin(Login.Text);
                if(idUsers == null)
                {
                    MessageBox.Show("Данный клиент не обнаружен. Пожалуйста,\r\n обратитесь к менеджеру нашей компании.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    string role = RoleUsers(idUsers);
                    Console.WriteLine(role);
                    if(role == "admin")
                    {
                        MainWindow mainWindow = new MainWindow();
                        mainWindow.Show();
                        this.Close();
                    }
                    else if (role == "customers")
                    {
                    }
                }
            }
            else
            {
                MessageBox.Show("Заполните все поля");
            }
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            MainWindow mainWindow = new MainWindow();
            mainWindow.Show();
            this.Close();
        }
    }
}
