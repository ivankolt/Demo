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
using System.Windows.Threading;

namespace Demo
{
    /// <summary>
    /// Логика взаимодействия для MainWindow_Users.xaml
    /// </summary>
    /// 

    class Orders_Products
    {
        public int product_id { get; set; }
        public string NameProduct { get; set; }
        public int id_suppliers { get; set; }
        public string Suppliers { get; set; }
        public int Qty { get; set; }

    }
    public partial class MainWindow_Users : Window
    {
        private static List<Orders_Products> orders_Products;
       
        public MainWindow_Users(int? idUsers)
        {
            InitializeComponent();
            orders_Products = new List<Orders_Products>();


            string Data = LoadUserData(idUsers);
            TextHello.Text = "Аутентификация прошла успешно, добро пожаловать " +  Data + "!";

            var timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(20)
            };
            timer.Tick += (s, e) =>
            {
                StackPanelHello.Visibility = Visibility.Collapsed;
                timer.Stop();
            };
            timer.Start();
        }
        public void AddList(int Id,string Name, int id_suppliers, string suppliers)
        {
            orders_Products.Add(new Orders_Products() {product_id = Id, NameProduct = Name, Qty = 0, Suppliers = suppliers, id_suppliers = id_suppliers });
        }
        public string LoadUserData(int? idUsers)
        {
            using (var conn = new NpgsqlConnection(Products_is_true.Connecting))
            {
                conn.Open();
                using (var cmd = new NpgsqlCommand("select persons.last_name || ' ' || persons.first_name as data_fio \r\nfrom users inner join persons ON persons.id = users.person_id\r\nwhere users.id = @User_id", conn))
                {
                    cmd.Parameters.AddWithValue("User_id", idUsers);
                    var result = cmd.ExecuteScalar();
                    if (result != null && result != DBNull.Value)
                       NameUsers.Text = result.ToString();
                        return result.ToString();
                }
            }
          
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Authentication mainWindow = new Authentication();
            mainWindow.Show();
            this.Close();
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {

        }

        private void Button_Click_2(object sender, RoutedEventArgs e)
        {
            string Name = ((ComboBoxItemEx)ComboBoxProducts.SelectedItem).Name;
            int Id = ((ComboBoxItemEx)ComboBoxProducts.SelectedItem).Id;
            string Suppliers = string.Empty;
            int IdSuppliers = 0;
            (IdSuppliers, Suppliers) = GetDataSuppliers(Id);
            AddList(Id,Name,IdSuppliers,Suppliers);

            foreach (var item in orders_Products)
            {
                StackPanel stackPanel = new StackPanel();
                stackPanel.Orientation = Orientation.Vertical;
                TextBlock textBlock = new TextBlock();

            }
        }
        public void LoadComboBox(int status)
        {
            var conn = new NpgsqlConnection(Products_is_true.Connecting);
            ComboBoxProducts.Items.Clear();
            conn.Open();

            using (var cmd = new NpgsqlCommand("select products.id, products.products_name from product_suppliers inner join products on products.id = product_suppliers.product_id\r\ninner join  suppliers on suppliers.id = product_suppliers.supplier_id\r\nwhere products.status_id = @id", conn))
            {
                cmd.Parameters.AddWithValue("id", status);
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var item = new ComboBoxItemEx
                        {
                            Id = reader.GetInt32(0),
                            Name = reader.GetString(1)
                        };
                        ComboBoxProducts.Items.Add(new ComboBoxItemEx
                        {
                            Id = item.Id,
                            Name = item.Name
                        });
                    }

                    reader.Close();
                }
            }

        }
        public (int,string) GetDataSuppliers(int idProducts)
        {
            var conn = new NpgsqlConnection(Products_is_true.Connecting);
            ComboBoxProducts.Items.Clear();
            conn.Open();

            using (var cmd = new NpgsqlCommand("select suppliers.id, suppliers.company_name from product_suppliers inner join products on products.id = product_suppliers.product_id\r\ninner join  suppliers on suppliers.id = product_suppliers.supplier_id\r\nwhere products.id = @id", conn))
            {
                cmd.Parameters.AddWithValue("id", idProducts);
                using (var reader = cmd.ExecuteReader())
                {
                    int id = 0;
                    string name = string.Empty;
                    while (reader.Read())
                    {
                        id = reader.GetInt32(0);
                        name = reader.GetString(1);
                       
                    }
                    reader.Close();
                    return (id,name);

                   
                }
            }

        }
        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (((ComboBoxItemEx)Products_Status.SelectedItem).Name == "В наличии")
            {
                LoadComboBox(1);
            }
            else if (((ComboBoxItemEx)Products_Status.SelectedItem).Name == "Под заказ")
            {
                LoadComboBox(2);
            }
            else
            {
                ComboBoxProducts.Items.Clear();
            }
        }
    }
}
