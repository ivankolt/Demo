using Npgsql;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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

    public class Supplier
    {
        public int ID { get; set; }
        public string NAME { get; set; }
        public string PHONE { get; set; }
        public string EMAIL { get; set; }
        public string CITY { get; set; }

    }

    public partial class SupplierWindow : Window
    {
        public ObservableCollection<Supplier> supplier { get; set; }
        public int Current_id { get; set; }

        public void LoadDataSupplier()
        {
            var conn = new NpgsqlConnection(Products_is_true.Connecting);
            conn.Open();

            using (var cmd = new NpgsqlCommand("SELECT suppliers.id, suppliers.company_name, cities.name, suppliers.email, suppliers.phone_number\r\n\tFROM public.suppliers inner join public.cities ON cities.id = suppliers.city_id ", conn))
            using (var reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    supplier.Add(new Supplier()
                    {
                        ID = reader.GetInt32(0),
                        NAME = reader.GetString(1),
                        PHONE = reader.GetString(4),
                        EMAIL = reader.GetString(3),
                        CITY = reader.GetString(2)
                    });
                }
                reader.Close();
            }
        }


        public SupplierWindow()
        {
            InitializeComponent();
            supplier = new ObservableCollection<Supplier>();
            LoadDataSupplier();
            DataSuppliers.ItemsSource = supplier;
            EditDeleteStackPanel.Visibility = Visibility.Collapsed;
            EditStackPanel.Visibility = Visibility.Collapsed;
            AddSupplier.Visibility = Visibility.Collapsed;
            GroupBoxAddProducts.Visibility = Visibility.Collapsed;
            CloseButton.Visibility = Visibility.Collapsed;

            DataSuppliers.Width = 800;
            Grid.SetColumnSpan(DataSuppliers, 2);
           
            LoadCity();
            LoadName();
        }

        public void LoadCity()
        {
            var conn = new NpgsqlConnection(Products_is_true.Connecting);
            conn.Open();

            using (var cmd = new NpgsqlCommand("SELECT id, name\r\n\tFROM public.cities;", conn))
            using (var reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    ComboBoxCities.Items.Add(new ComboBoxItemEx
                    {
                        Id = reader.GetInt32(0),
                        Name = reader.GetString(1)
                    });
                    ComboBoxCities2.Items.Add(new ComboBoxItemEx
                    {
                        Id = reader.GetInt32(0),
                        Name = reader.GetString(1)
                    });
                }
                reader.Close();
            }
        }

        public void LoadName()
        {
            var conn = new NpgsqlConnection(Products_is_true.Connecting);
            conn.Open();

            using (var cmd = new NpgsqlCommand("SELECT id, company_name\r\n\tFROM public.suppliers;", conn))
            using (var reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    EditNameProduct.Items.Add(new ComboBoxItemEx
                    {
                        Id = reader.GetInt32(0),
                        Name = reader.GetString(1)
                    });   
                }
                reader.Close();
            }
        }
        public void InsertCompany(string name, string email, string phone, int city)
        {

            var conn = new NpgsqlConnection(Products_is_true.Connecting);
            conn.Open();
            int newId = 1;
            using (var selectCmd = new NpgsqlCommand("SELECT COALESCE(MAX(id), 0) + 1 FROM public.suppliers", conn))
            {
                newId = Convert.ToInt32(selectCmd.ExecuteScalar());
            }
            using (var cmd = new NpgsqlCommand(
                "INSERT INTO public.suppliers(\r\n\tid, company_name, city_id, email, phone_number)\r\n\t" +
                "VALUES (@id, @name, @city, @email, @phone);", conn))
            {
                cmd.Parameters.AddWithValue("id", newId);
                cmd.Parameters.AddWithValue("name", name);
                cmd.Parameters.AddWithValue ("email", email);
                cmd.Parameters.AddWithValue("city", city);
                cmd.Parameters.AddWithValue("phone",phone);
                cmd.ExecuteNonQuery();
            }
        }
        private void Button_Click_5(object sender, RoutedEventArgs e)
        {
            string name = NameSuppliers.Text;
            string email = Email.Text;
            int city = ComboBoxCities.SelectedIndex + 1;
            string phone = PhoneNumber.Text;

            if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(email) || city < 0
                || string.IsNullOrWhiteSpace(phone)
               )
            {
                MessageBox.Show("Пожалуйста, заполните все поля", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            InsertCompany(name,email,phone,city);
            MessageBox.Show("Товар добавлен", "Успешно", MessageBoxButton.OK, MessageBoxImage.Information);
            UpdateCompany();
        }

        public void UpdateCompany()
        {
            supplier.Clear();

            var conn = new NpgsqlConnection(Products_is_true.Connecting);
            conn.Open();
            using (var cmd = new NpgsqlCommand(
                "SELECT suppliers.id, suppliers.company_name, cities.name, suppliers.email, " +
                "suppliers.phone_number\r\n\tFROM public.suppliers inner join" +
                " public.cities ON cities.id = suppliers.city_id ", conn))
            using (var reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    supplier.Add(new Supplier()
                    {
                        ID = reader.GetInt32(0),
                        NAME = reader.GetString(1),
                        PHONE = reader.GetString(4),
                        EMAIL = reader.GetString(3),
                        CITY = reader.GetString(2)
                    });
                }
                reader.Close();
            }
        }
        private void Button_Click_9(object sender, RoutedEventArgs e)
        {
            GroupBoxAddProducts.Visibility = Visibility.Collapsed;
            CloseButton.Visibility = Visibility.Collapsed;
            OpenButtton.Visibility = Visibility.Visible;
            DataSuppliers.Width = 800;
            Grid.SetColumnSpan(DataSuppliers, 2);
        }

        private void Button_Click_10(object sender, RoutedEventArgs e)
        {
            GroupBoxAddProducts.Visibility = Visibility.Visible;
            CloseButton.Visibility = Visibility.Visible;
            OpenButtton.Visibility = Visibility.Collapsed;
            DataSuppliers.Width = 400;
            Grid.SetColumnSpan(DataSuppliers, 1);
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            EditStackPanel.Visibility = Visibility.Visible;
            EditDeleteStackPanel.Visibility = Visibility.Collapsed;
            AddSupplier.Visibility = Visibility.Collapsed;
        }

        private void Button_Click_2(object sender, RoutedEventArgs e)
        {
            AddStackPanel.Visibility = Visibility.Visible;
            AddSupplier.Visibility = Visibility.Collapsed;
            EditDeleteStackPanel.Visibility = Visibility.Collapsed;
        }
        public void DeleteProducts()
        {
            var conn = new NpgsqlConnection(Products_is_true.Connecting);
            conn.Open();
            using (var cmd = new NpgsqlCommand("DELETE FROM public.product_suppliers WHERE supplier_id = @ID", conn))
            {
                cmd.Parameters.AddWithValue("ID", Current_id);
                cmd.ExecuteNonQuery();
            }
            using (var cmd = new NpgsqlCommand("DELETE FROM public.suppliers WHERE id = @ID", conn))
            {
                cmd.Parameters.AddWithValue("ID", Current_id);
                cmd.ExecuteNonQuery();
            }
            UpdateCompany();
        }
        private void Button_Click_3(object sender, RoutedEventArgs e)
        {
            if (DataSuppliers.SelectedItem is Supplier products)
                Current_id = products.ID;

            var YesOrNo = MessageBox.Show(
                "Вы точно хотите удалить этот товар?",
                "Удаление товара",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question
            );

            if (YesOrNo == MessageBoxResult.Yes)
            {
                DeleteProducts();
                MessageBox.Show("Вы удалили товар", "Успешно", MessageBoxButton.OK, MessageBoxImage.Information);
            }

        }
        
        private void Button_Click_6(object sender, RoutedEventArgs e)
        {
            AddSupplier.Visibility = Visibility.Visible;
            AddStackPanel.Visibility = Visibility.Collapsed;
            EditDeleteStackPanel.Visibility = Visibility.Collapsed;
            EditStackPanel.Visibility = Visibility.Collapsed;
        }


        public void UpdateDataSupplierProducts(int name_id,string name , string phone, string email, int id_city)
        {
            var conn = new NpgsqlConnection(Products_is_true.Connecting);
            conn.Open();

            using (var cmd = new NpgsqlCommand("UPDATE public.suppliers\r\n\t set company_name=@name, city_id=@city, " +
                "email=@email,\r\n\tphone_number=@phone_number\r\n\tWHERE id = @ID;", conn))
            {
                cmd.Parameters.AddWithValue("ID", name_id);
                cmd.Parameters.AddWithValue("phone_number", phone);
                cmd.Parameters.AddWithValue("name", name);
                cmd.Parameters.AddWithValue("email", email);
                cmd.Parameters.AddWithValue("city", id_city);

                cmd.ExecuteNonQuery();
            }
        }
        private void Button_Click_4(object sender, RoutedEventArgs e)
        {
            int id = 0;
            if (EditNameProduct.SelectedItem is ComboBoxItemEx selected)
            {
                id = selected.Id; /// получим id с combo box
            }

            int city_id = 0;
            if (ComboBoxCities2.SelectedItem is ComboBoxItemEx selected2)
            {
                city_id = selected2.Id; /// получим id с combo box
            }
            Console.WriteLine(id);

            if (EditNameProduct.SelectedItem == null || ComboBoxCities2.SelectedItem == null || PhoneNumber.Text == null || Email.Text == null)
            {
                MessageBox.Show("Пожалуйста, введите данные", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            else
            {


                UpdateDataSupplierProducts(id, EditNameProduct.SelectedItem.ToString(), EditSuppliersProducts.Text, EditPriceProducts.Text, city_id);
                    MessageBox.Show("Данные обновлены", "Успешно", MessageBoxButton.OK, MessageBoxImage.Information);
                    UpdateCompany();

 
            }
           
        }

        private void Button_Click_8(object sender, RoutedEventArgs e)
        {
            MainWindow mainWindow = new MainWindow();
            mainWindow.Show();
            this.Close();
        }
        public void SetData()
        {
            var conn = new NpgsqlConnection(Products_is_true.Connecting);
            conn.Open();

            using (var cmd = new NpgsqlCommand("SELECT suppliers.company_name, cities.name, suppliers.email, suppliers.phone_number\r\n\tFROM public.suppliers inner join public.cities ON cities.id = suppliers.city_id \r\n\twhere suppliers.id = @ID;", conn))
            {
                cmd.Parameters.AddWithValue("ID", Current_id);
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var supplierName = reader.IsDBNull(0) ? null : reader.GetString(0);

                        EditNameProduct.SelectedItem = EditNameProduct.Items
                                .OfType<ComboBoxItemEx>()
                                .FirstOrDefault(x => x.Name == supplierName);

                        var productName = reader.GetString(1);
                        ComboBoxCities2.SelectedItem = ComboBoxCities2.Items
                            .OfType<ComboBoxItemEx>()
                            .FirstOrDefault(x => x.Name == productName);

                        EditPriceProducts.Text = reader.GetString(2);
                        EditSuppliersProducts.Text = reader.GetString(3);
                    }
                    reader.Close();
                }
            }
        }
        private void DataProducts_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (OpenButtton.Visibility == Visibility.Collapsed)
            {
                Console.WriteLine("Я тут");
                EditDeleteStackPanel.Visibility = Visibility.Visible;
                AddStackPanel.Visibility = Visibility.Collapsed;
                EditStackPanel.Visibility = Visibility.Collapsed;
                AddSupplier.Visibility = Visibility.Collapsed;
                if (DataSuppliers.SelectedItem is Supplier products)
                    Current_id = products.ID;
                Console.WriteLine(Current_id);
                SetData();
            }
        }

        private void DataProducts_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {

        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            EditDeleteStackPanel.Visibility = Visibility.Visible;
            EditStackPanel.Visibility = Visibility.Collapsed;
            AddSupplier.Visibility = Visibility.Collapsed;
        }

        private void Button_Click_7(object sender, RoutedEventArgs e)
        {
            AddSupplier.Visibility = Visibility.Collapsed;
            AddStackPanel.Visibility = Visibility.Visible;
            EditDeleteStackPanel.Visibility = Visibility.Collapsed;
            EditStackPanel.Visibility = Visibility.Collapsed;
        }
    }
}
