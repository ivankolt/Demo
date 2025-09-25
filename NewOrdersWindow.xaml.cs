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
    /// Логика взаимодействия для NewOrdersWindow.xaml
    /// </summary>
    public partial class NewOrdersWindow : Window
    {
        public static int idUsers = -1;
        public NewOrdersWindow()
        {
            InitializeComponent();
            LoadDataUsers();
        }
        public int AddNewPersons(string first_name,string last_name,string middle_name,DateTime? birth)
        {
            var conn = new NpgsqlConnection(Products_is_true.Connecting);
            conn.Open();

            using (var cmd = new NpgsqlCommand("INSERT INTO \r\npublic.persons(first_name, last_name, middle_name, birth_date)\r\nVALUES (@FirstName, @LastName, @MiddleName, @Birth)\r\nRETURNING id;", conn))
            {
                cmd.Parameters.AddWithValue("FirstName", first_name);
                cmd.Parameters.AddWithValue("LastName", last_name);
                cmd.Parameters.AddWithValue("MiddleName", middle_name);
                cmd.Parameters.AddWithValue("Birth", birth);
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {

                        idUsers = reader.GetInt32(0);
                           
                    }
                    reader.Close();
                }
            }
            return idUsers;
        }

        public int AddNewUsers(int idPersons, string userName, string password)
        {
            var conn = new NpgsqlConnection(Products_is_true.Connecting);
            conn.Open();

            using (var cmd = new NpgsqlCommand("INSERT INTO public.users(\r\n\tperson_id, username, password_hash, role)\r\n\tVALUES (@Person_id, @UserName, crypt(@Password, gen_salt('md5')), 'customers') RETURNING id", conn))
            {
                cmd.Parameters.AddWithValue("Person_id", idPersons);
                cmd.Parameters.AddWithValue("UserName", userName);
                cmd.Parameters.AddWithValue("Password", password);
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {

                        idUsers = reader.GetInt32(0);

                    }
                    reader.Close();
                }
            }
            return idUsers;
        }
        public void LoadDataUsers()
        {
            var conn = new NpgsqlConnection(Products_is_true.Connecting);
            conn.Open();

            using (var cmd = new NpgsqlCommand("select users.id, (persons.first_name || ' ' || persons.last_name ||  ' ' || persons.middle_name) as fio\r\nfrom persons\r\nRIGHT join users on persons.id = users.person_id where role = 'customers'", conn))
            using (var reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    var item = new ComboBoxItemEx
                    {
                        Id = reader.GetInt32(0),
                        Name = reader.GetString(1)
                    };
                    CustomersComboBox.Items.Add(new ComboBoxItemEx
                    {
                        Id = item.Id,
                        Name = item.Name
                    });
                }
                reader.Close();
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            idUsers = ((ComboBoxItemEx)CustomersComboBox.SelectedItem).Id;
            Create_orders create_Orders = new Create_orders();
            create_Orders.Show();
            this.Close();
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
           
                DateTime? dateTime = Birth.SelectedDate;
                int idPersons = AddNewPersons(FirstName.Text, LastName.Text, MiddleName.Text, dateTime);
                int idUSers = AddNewUsers(idPersons, Login.Text, Password.Text);
                MessageBox.Show("Пользовател с логином: " + Login.Text + " создан");
            Create_orders create_Orders = new Create_orders();
            create_Orders.Show();
            this.Close();
            
        }

        private void Button_Click_8(object sender, RoutedEventArgs e)
        {
            MainWindow mainWindow = new MainWindow();
            mainWindow.Show();
            this.Close();
        }
    }
}
