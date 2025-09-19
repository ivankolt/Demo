using Npgsql;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Demo
{
    /// <summary>
    /// Логика взаимодействия для Products_Suppliers.xaml
    /// </summary>

    public class ProductSupplier
    {
        public int PRODUCT_ID { get; set; }
        public int SUPPLIER_ID { get; set; }
        public string SUPPLIER_NAME { get; set; }
        public string PRODUCT_NAME { get; set; }
        public decimal DELIVERY_PRICE { get; set; }
    }

    public partial class Products_Suppliers : Window
    {
        private decimal baseValue = 0m;
        private double scrollStart = 0;
        private bool initialized = false;

        public ObservableCollection<ProductSupplier> productSuppliers { get; set; }
        public int Current_product_id { get; set; }
        public int Current_supplier_id { get; set; }
        public static string Connecting { get; } = "Host=localhost;Database=MMM;Port=5432;Username=postgres;Password=12345\r\n";

        public Products_Suppliers()
        {
            InitializeComponent();
            productSuppliers = new ObservableCollection<ProductSupplier>();
            LoadDataProductSuppliers();
            DataProductsSuppliers.ItemsSource = productSuppliers;
            EditDeleteStackPanel.Visibility = Visibility.Collapsed;
            EditStackPanel.Visibility = Visibility.Collapsed;
            AddProductSupplier.Visibility = Visibility.Collapsed;
            GroupBoxAddProducts.Visibility = Visibility.Collapsed;
            CloseButton.Visibility = Visibility.Collapsed;
            DataProductsSuppliers.Width = 800;
            Grid.SetColumnSpan(DataProductsSuppliers, 2);
            LoadSuppliersData();
            LoadProductsData();
        }

        public void LoadDataProductSuppliers()
        {
            var conn = new NpgsqlConnection(Connecting);
            conn.Open();

            using (var cmd = new NpgsqlCommand("SELECT ps.product_id, ps.supplier_id, " +
                "s.company_name, p.products_name, ps.delivery_price " +
                "FROM public.product_suppliers ps " +
                "LEFT JOIN suppliers s ON s.id = ps.supplier_id " +
                "LEFT JOIN products p ON p.id = ps.product_id", conn))
            using (var reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    productSuppliers.Add(new ProductSupplier()
                    {
                        PRODUCT_ID = reader.GetInt32(0),
                        SUPPLIER_ID = reader.GetInt32(1),
                        SUPPLIER_NAME = reader.GetString(2),
                        PRODUCT_NAME = reader.GetString(3),
                        DELIVERY_PRICE = reader.GetDecimal(4)
                    });
                }
                reader.Close();
            }
        }

        public void LoadSuppliersData()
        {
            SupplierProducts.Items.Clear();
            EditSupplierProduct.Items.Clear();
            var conn = new NpgsqlConnection(Connecting);
            conn.Open();

            using (var cmd = new NpgsqlCommand("SELECT id, company_name FROM public.suppliers", conn))
            using (var reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    var item = new ComboBoxItemEx
                    {
                        Id = reader.GetInt32(0),
                        Name = reader.GetString(1)
                    };
                    SupplierProducts.Items.Add(item);
                    EditSupplierProduct.Items.Add(new ComboBoxItemEx
                    {
                        Id = item.Id,
                        Name = item.Name
                    });
                }
                reader.Close();
            }
        }

        public void LoadProductsData()
        {
            ProductProducts.Items.Clear();
            EditProductProduct.Items.Clear();
            var conn = new NpgsqlConnection(Connecting);
            conn.Open();

            using (var cmd = new NpgsqlCommand("SELECT id, products_name FROM public.products", conn))
            using (var reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    var item = new ComboBoxItemEx
                    {
                        Id = reader.GetInt32(0),
                        Name = reader.GetString(1)
                    };
                    ProductProducts.Items.Add(item);
                    EditProductProduct.Items.Add(new ComboBoxItemEx
                    {
                        Id = item.Id,
                        Name = item.Name
                    });
                }
                reader.Close();
            }
        }

        public void DeleteProductSupplier()
        {
            var conn = new NpgsqlConnection(Connecting);
            conn.Open();
            using (var cmd = new NpgsqlCommand("DELETE FROM public.product_suppliers WHERE product_id = @product_id AND supplier_id = @supplier_id", conn))
            {
                cmd.Parameters.AddWithValue("product_id", Current_product_id);
                cmd.Parameters.AddWithValue("supplier_id", Current_supplier_id);
                cmd.ExecuteNonQuery();
            }
            UpdateDataProductSuppliers();
        }

        public void SetData()
        {
            var conn = new NpgsqlConnection(Connecting);
            conn.Open();

            using (var cmd = new NpgsqlCommand("SELECT ps.supplier_id, ps.product_id, ps.delivery_price, " +
                "s.company_name, p.products_name " +
                "FROM product_suppliers ps " +
                "LEFT JOIN suppliers s ON s.id = ps.supplier_id " +
                "LEFT JOIN products p ON p.id = ps.product_id " +
                "WHERE ps.product_id = @product_id AND ps.supplier_id = @supplier_id", conn))
            {
                cmd.Parameters.AddWithValue("product_id", Current_product_id);
                cmd.Parameters.AddWithValue("supplier_id", Current_supplier_id);
                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        var supplierName = reader.GetString(3);
                        var productName = reader.GetString(4);

                        EditSupplierProduct.SelectedItem = EditSupplierProduct.Items
                            .OfType<ComboBoxItemEx>()
                            .FirstOrDefault(x => x.Name == supplierName);

                        EditProductProduct.SelectedItem = EditProductProduct.Items
                            .OfType<ComboBoxItemEx>()
                            .FirstOrDefault(x => x.Name == productName);

                        EditDeliveryPrice.Text = reader.GetDecimal(2).ToString();
                    }
                    reader.Close();
                }
            }
        }

        private void ScrollBar_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            var sb = sender as ScrollBar;
            if (!initialized)
            {
                decimal.TryParse(EditDeliveryPrice.Text, out baseValue);
                scrollStart = sb.Value;
                initialized = true;
            }

            decimal newValue = baseValue + Convert.ToDecimal(sb.Value - scrollStart);

            if (newValue < 0)
                newValue = 0m;

            EditDeliveryPrice.Text = newValue.ToString("F0");
        }

        private void DataProductsSuppliers_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            // Можно добавить логику двойного клика если нужно
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            EditStackPanel.Visibility = Visibility.Visible;
            EditDeleteStackPanel.Visibility = Visibility.Collapsed;
            AddProductSupplier.Visibility = Visibility.Collapsed;
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            EditDeleteStackPanel.Visibility = Visibility.Visible;
            EditStackPanel.Visibility = Visibility.Collapsed;
            AddProductSupplier.Visibility = Visibility.Collapsed;
        }

        private void Button_Click_2(object sender, RoutedEventArgs e)
        {
            AddStackPanel.Visibility = Visibility.Visible;
            AddProductSupplier.Visibility = Visibility.Collapsed;
            EditDeleteStackPanel.Visibility = Visibility.Collapsed;
        }

        private void DataProductsSuppliers_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (OpenButtton.Visibility == Visibility.Collapsed)
            {
                EditDeleteStackPanel.Visibility = Visibility.Visible;
                AddStackPanel.Visibility = Visibility.Collapsed;
                EditStackPanel.Visibility = Visibility.Collapsed;
                AddProductSupplier.Visibility = Visibility.Collapsed;
                if (DataProductsSuppliers.SelectedItem is ProductSupplier productSupplier)
                {
                    Current_product_id = productSupplier.PRODUCT_ID;
                    Current_supplier_id = productSupplier.SUPPLIER_ID;
                }
                SetData();
            }
        }

        private void Button_Click_3(object sender, RoutedEventArgs e)
        {
            if (DataProductsSuppliers.SelectedItem is ProductSupplier productSupplier)
            {
                Current_product_id = productSupplier.PRODUCT_ID;
                Current_supplier_id = productSupplier.SUPPLIER_ID;
            }

            var YesOrNo = MessageBox.Show(
                "Вы точно хотите удалить эту связь товар-поставщик?",
                "Удаление записи",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question
            );

            if (YesOrNo == MessageBoxResult.Yes)
            {
                DeleteProductSupplier();
                MessageBox.Show("Вы удалили запись", "Успешно", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        public void UpdateDataProductSuppliers()
        {
            productSuppliers.Clear();

            var conn = new NpgsqlConnection(Connecting);
            conn.Open();
            using (var cmd = new NpgsqlCommand(
                "SELECT ps.product_id, ps.supplier_id, " +
                "s.company_name, p.products_name, ps.delivery_price " +
                "FROM public.product_suppliers ps " +
                "LEFT JOIN suppliers s ON s.id = ps.supplier_id " +
                "LEFT JOIN products p ON p.id = ps.product_id", conn))
            using (var reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    productSuppliers.Add(new ProductSupplier()
                    {
                        PRODUCT_ID = reader.GetInt32(0),
                        SUPPLIER_ID = reader.GetInt32(1),
                        SUPPLIER_NAME = reader.GetString(2),
                        PRODUCT_NAME = reader.GetString(3),
                        DELIVERY_PRICE = reader.GetDecimal(4)
                    });
                }
                reader.Close();
            }
        }

        public void InsertDataProductSupplier(int product_id, int supplier_id, decimal delivery_price)
        {
            var conn = new NpgsqlConnection(Connecting);
            conn.Open();

            // Проверяем, существует ли уже такая запись
            using (var checkCmd = new NpgsqlCommand("SELECT COUNT(*) FROM public.product_suppliers WHERE product_id = @product_id AND supplier_id = @supplier_id", conn))
            {
                checkCmd.Parameters.AddWithValue("product_id", product_id);
                checkCmd.Parameters.AddWithValue("supplier_id", supplier_id);
                int count = Convert.ToInt32(checkCmd.ExecuteScalar());

                if (count > 0)
                {
                    MessageBox.Show("Такая связь товар-поставщик уже существует!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
            }

            using (var cmd = new NpgsqlCommand("INSERT INTO public.product_suppliers(product_id, supplier_id, delivery_price) VALUES (@product_id, @supplier_id, @delivery_price);", conn))
            {
                cmd.Parameters.AddWithValue("product_id", product_id);
                cmd.Parameters.AddWithValue("supplier_id", supplier_id);
                cmd.Parameters.AddWithValue("delivery_price", delivery_price);

                cmd.ExecuteNonQuery();
            }
        }

        public void UpdateDataProductSupplier(int old_product_id, int old_supplier_id, int new_product_id, int new_supplier_id, decimal delivery_price)
        {
            var conn = new NpgsqlConnection(Connecting);
            conn.Open();

            // Если изменились ключи, то удаляем старую запись и добавляем новую
            if (old_product_id != new_product_id || old_supplier_id != new_supplier_id)
            {
                // Проверяем, не существует ли уже новая комбинация
                using (var checkCmd = new NpgsqlCommand("SELECT COUNT(*) FROM public.product_suppliers WHERE product_id = @new_product_id AND supplier_id = @new_supplier_id", conn))
                {
                    checkCmd.Parameters.AddWithValue("new_product_id", new_product_id);
                    checkCmd.Parameters.AddWithValue("new_supplier_id", new_supplier_id);
                    int count = Convert.ToInt32(checkCmd.ExecuteScalar());

                    if (count > 0)
                    {
                        MessageBox.Show("Такая связь товар-поставщик уже существует!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }
                }

                // Удаляем старую запись
                using (var deleteCmd = new NpgsqlCommand("DELETE FROM public.product_suppliers WHERE product_id = @old_product_id AND supplier_id = @old_supplier_id", conn))
                {
                    deleteCmd.Parameters.AddWithValue("old_product_id", old_product_id);
                    deleteCmd.Parameters.AddWithValue("old_supplier_id", old_supplier_id);
                    deleteCmd.ExecuteNonQuery();
                }

                // Добавляем новую запись
                using (var insertCmd = new NpgsqlCommand("INSERT INTO public.product_suppliers(product_id, supplier_id, delivery_price) VALUES (@product_id, @supplier_id, @delivery_price)", conn))
                {
                    insertCmd.Parameters.AddWithValue("product_id", new_product_id);
                    insertCmd.Parameters.AddWithValue("supplier_id", new_supplier_id);
                    insertCmd.Parameters.AddWithValue("delivery_price", delivery_price);
                    insertCmd.ExecuteNonQuery();
                }
            }
            else
            {
                // Просто обновляем цену доставки
                using (var cmd = new NpgsqlCommand("UPDATE public.product_suppliers SET delivery_price=@delivery_price WHERE product_id = @product_id AND supplier_id = @supplier_id;", conn))
                {
                    cmd.Parameters.AddWithValue("product_id", old_product_id);
                    cmd.Parameters.AddWithValue("supplier_id", old_supplier_id);
                    cmd.Parameters.AddWithValue("delivery_price", delivery_price);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        private void Button_Click_4(object sender, RoutedEventArgs e)
        {
            if (EditSupplierProduct.SelectedItem == null || EditProductProduct.SelectedItem == null)
            {
                MessageBox.Show("Пожалуйста, выберите поставщика и товар", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            int new_supplier_id = ((ComboBoxItemEx)EditSupplierProduct.SelectedItem).Id;
            int new_product_id = ((ComboBoxItemEx)EditProductProduct.SelectedItem).Id;
            decimal delivery_price = 0m;
            decimal.TryParse(EditDeliveryPrice.Text, out delivery_price);

            UpdateDataProductSupplier(Current_product_id, Current_supplier_id, new_product_id, new_supplier_id, delivery_price);
            MessageBox.Show("Данные обновлены", "Успешно", MessageBoxButton.OK, MessageBoxImage.Information);
            UpdateDataProductSuppliers();
        }

        private void Button_Click_5(object sender, RoutedEventArgs e)
        {
            if (SupplierProducts.SelectedItem == null || ProductProducts.SelectedItem == null)
            {
                MessageBox.Show("Пожалуйста, выберите поставщика и товар", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            int supplier_id = ((ComboBoxItemEx)SupplierProducts.SelectedItem).Id;
            int product_id = ((ComboBoxItemEx)ProductProducts.SelectedItem).Id;
            decimal delivery_price = 0m;
            decimal.TryParse(DeliveryPriceProducts.Text, out delivery_price);

            InsertDataProductSupplier(product_id, supplier_id, delivery_price);
            MessageBox.Show("Запись добавлена", "Успешно", MessageBoxButton.OK, MessageBoxImage.Information);
            UpdateDataProductSuppliers();

            // Очищаем поля
            SupplierProducts.SelectedItem = null;
            ProductProducts.SelectedItem = null;
            DeliveryPriceProducts.Text = string.Empty;
        }

        private void Button_Click_6(object sender, RoutedEventArgs e)
        {
            AddProductSupplier.Visibility = Visibility.Visible;
            AddStackPanel.Visibility = Visibility.Collapsed;
            EditDeleteStackPanel.Visibility = Visibility.Collapsed;
            EditStackPanel.Visibility = Visibility.Collapsed;
        }

        private void Button_Click_7(object sender, RoutedEventArgs e)
        {
            AddProductSupplier.Visibility = Visibility.Collapsed;
            AddStackPanel.Visibility = Visibility.Visible;
            EditDeleteStackPanel.Visibility = Visibility.Collapsed;
            EditStackPanel.Visibility = Visibility.Collapsed;
        }

        private void Button_Click_8(object sender, RoutedEventArgs e)
        {
            MainWindow mainWindow = new MainWindow();
            mainWindow.Show();
            this.Close();
        }

        private void Button_Click_9(object sender, RoutedEventArgs e)
        {
            GroupBoxAddProducts.Visibility = Visibility.Collapsed;
            CloseButton.Visibility = Visibility.Collapsed;
            OpenButtton.Visibility = Visibility.Visible;
            DataProductsSuppliers.Width = 800;
            Grid.SetColumnSpan(DataProductsSuppliers, 2);
        }

        private void Button_Click_10(object sender, RoutedEventArgs e)
        {
            GroupBoxAddProducts.Visibility = Visibility.Visible;
            CloseButton.Visibility = Visibility.Visible;
            OpenButtton.Visibility = Visibility.Collapsed;
            DataProductsSuppliers.Width = 400;
            Grid.SetColumnSpan(DataProductsSuppliers, 1);
        }

    

   
        public int LevenshteinDistance(string a, string b)
        {
            int[,] d = new int[a.Length + 1, b.Length + 1];
            for (int i = 0; i <= a.Length; i++)
                d[i, 0] = i;
            for (int j = 0; j <= b.Length; j++)
                d[0, j] = j;
            for (int i = 1; i <= a.Length; i++)
            {
                for (int j = 1; j <= b.Length; j++)
                {
                    int cost = a[i - 1] == b[j - 1] ? 0 : 1;
                    d[i, j] = Math.Min(
                        Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1),
                        d[i - 1, j - 1] + cost
                    );
                }
            }
            return d[a.Length, b.Length];
        }

        public void SearchDataProductSuppliers(string word)
        {
            productSuppliers.Clear();

            var conn = new NpgsqlConnection(Connecting);
            conn.Open();
            using (var cmd = new NpgsqlCommand(
                "SELECT ps.product_id, ps.supplier_id, " +
                "s.company_name, p.products_name, ps.delivery_price " +
                "FROM public.product_suppliers ps " +
                "LEFT JOIN suppliers s ON s.id = ps.supplier_id " +
                "LEFT JOIN products p ON p.id = ps.product_id " +
                "WHERE abs(LENGTH(s.company_name) - LENGTH(@word)) < 4 OR abs(LENGTH(p.products_name) - LENGTH(@word)) < 4", conn))
            {
                cmd.Parameters.AddWithValue("word", word);
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        string supplierName = reader.GetString(2);
                        string productName = reader.GetString(3);

                        if (LevenshteinDistance(word, supplierName) <= 3 || LevenshteinDistance(word, productName) <= 3)
                        {
                            productSuppliers.Add(new ProductSupplier()
                            {
                                PRODUCT_ID = reader.GetInt32(0),
                                SUPPLIER_ID = reader.GetInt32(1),
                                SUPPLIER_NAME = supplierName,
                                PRODUCT_NAME = productName,
                                DELIVERY_PRICE = reader.GetDecimal(4)
                            });
                        }
                    }
                    reader.Close();
                }
            }
        }

      

    }
}
