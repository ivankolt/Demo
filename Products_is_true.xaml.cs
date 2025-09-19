using Npgsql;
using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.InteropServices.ComTypes;
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
    /// Логика взаимодействия для Products_is_true.xaml
    /// </summary>
    /// 
    public class Products
    {
        public int ID { get; set; }
        public string NAME { get; set; }
        public string ARTICULE { get; set; }
        public decimal PRICE_PRODUCTS { get; set; }
        public decimal PRICE_SHIPPING { get; set; }
        public string STATUS { get; set; }

    }

    public class ComboBoxItemEx
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public override string ToString() => Name; 
    }
    public partial class Products_is_true : Window
    {
        private decimal baseValue = 0m;
        private double scrollStart = 0;
        private bool initialized = false;

        public ObservableCollection<Products> products { get; set; }
        public int Current_id { get; set; }
        public static string Connecting { get;  } = "Host=localhost;Database=MMM;Port=5432;Username=postgres;Password=12345\r\n"; 
        public Products_is_true()
        {
            InitializeComponent();
            products = new ObservableCollection<Products>();
            LoadDataProducts();
            DataProducts.ItemsSource = products;
            EditDeleteStackPanel.Visibility = Visibility.Collapsed;
            EditStackPanel.Visibility = Visibility.Collapsed;
            AddProducts.Visibility = Visibility.Collapsed;
            GroupBoxAddProducts.Visibility = Visibility.Collapsed;
            CloseButton.Visibility = Visibility.Collapsed;
            ButtonSeatch.Visibility = Visibility.Collapsed;
            ButtonExit.Visibility = Visibility.Collapsed;
            DataProducts.Width = 800;
            Grid.SetColumnSpan(DataProducts, 2);
            LoadDataName();
            LoadDataSuppliers();
            LoadCategoryProducts();
            LoadStatusProducts();

        }

        public int InsertOnUpdateProducts(int id)
        {
            var conn = new NpgsqlConnection(Connecting);
            conn.Open();
            int result = -1;
            using (var cmd = new NpgsqlCommand("SELECT product_suppliers.supplier_id FROM product_suppliers WHERE product_suppliers.product_id = @ID", conn))
            {
                cmd.Parameters.AddWithValue("ID", id);
                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        result = reader.IsDBNull(0) ? -1 : reader.GetInt32(0);
                    }
                }
            }
            return result;
        }
        public void LoadDataProducts()
        {
            var conn = new NpgsqlConnection(Connecting);
            conn.Open();

            using (var cmd = new NpgsqlCommand("SELECT products.id, products.products_name, \r\nproducts.product_article, products.price,\r\nproduct_suppliers.delivery_price, status.status_name\r\nFROM public.products\r\nleft join product_suppliers on product_suppliers.product_id = products.id\r\nleft join status on status.id = products.status_id", conn))
            using (var reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    products.Add(new Products()
                    {
                        ID = reader.GetInt32(0),
                        NAME = reader.GetString(1),
                        ARTICULE = reader.GetString(2),
                        PRICE_PRODUCTS = reader.GetDecimal(3),
                        PRICE_SHIPPING = reader.IsDBNull(4) ? 0m : reader.GetDecimal(4),
                        STATUS = reader.GetString(5)
                    });
                }
                reader.Close();
            }
        }

        public void LoadDataName()
        {
            var conn = new NpgsqlConnection(Connecting);
            conn.Open();

            using (var cmd = new NpgsqlCommand("SELECT id, products_name FROM public.products", conn))
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
        public void DeleteProducts()
        {
            var conn = new NpgsqlConnection(Connecting);
            conn.Open();
            using (var cmd = new NpgsqlCommand("DELETE FROM public.product_suppliers WHERE product_id = @ID", conn))
            {
                cmd.Parameters.AddWithValue("ID", Current_id);
                cmd.ExecuteNonQuery();
            }
            using (var cmd = new NpgsqlCommand("DELETE FROM public.products WHERE id = @ID", conn))
            {
                cmd.Parameters.AddWithValue("ID", Current_id);
                cmd.ExecuteNonQuery();
            }
            UpdateDataProducts();
        }
        public void SetData()
        { 
            var conn = new NpgsqlConnection(Connecting);
            conn.Open();

            using (var cmd = new NpgsqlCommand("SELECT products.products_name,suppliers.company_name, product_suppliers.delivery_price FROM product_suppliers\r\nright join products on products.id = product_suppliers.product_id\r\nleft join suppliers on suppliers.id = product_suppliers.supplier_id\r\nwhere products.id = @ID", conn))
            {
                cmd.Parameters.AddWithValue("ID", Current_id);
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var supplierName = reader.IsDBNull(1) ? null : reader.GetString(1);
                        if (supplierName == null)
                            EditSuppliersProducts.SelectedItem = null;
                        else
                            EditSuppliersProducts.SelectedItem = EditSuppliersProducts.Items
                                .OfType<ComboBoxItemEx>()
                                .FirstOrDefault(x => x.Name == supplierName);

                        var productName = reader.GetString(0);
                        EditNameProduct.SelectedItem = EditNameProduct.Items
                            .OfType<ComboBoxItemEx>()
                            .FirstOrDefault(x => x.Name == productName);
                        EditPriceProducts.Text = Convert.ToString(reader.IsDBNull(2) ? 0m : reader.GetDecimal(2));
                    }
                    reader.Close();
                }
            }
        }

        public void LoadDataSuppliers()
        {
            var conn = new NpgsqlConnection(Connecting);
            conn.Open();

            using (var cmd = new NpgsqlCommand("SELECT id, company_name FROM public.suppliers;", conn))
            using (var reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    EditSuppliersProducts.Items.Add(new ComboBoxItemEx
                    {
                        Id = reader.GetInt32(0),
                        Name = reader.GetString(1)
                    });
                }
                reader.Close();
            }
        }
        private void ScrollBar_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            var sb = sender as ScrollBar;
            if (!initialized)
            {
                decimal.TryParse(EditPriceProducts.Text, out baseValue);
                scrollStart = sb.Value;
                initialized = true;
            }

            decimal newValue = baseValue + Convert.ToDecimal(sb.Value - scrollStart);

            // Разрешаем опускаться до 0, но не ниже
            if (newValue < 0)
                newValue = 0m;

            EditPriceProducts.Text = newValue.ToString("F0");
        }


        private void DataProducts_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
           
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            EditStackPanel.Visibility = Visibility.Visible;
            EditDeleteStackPanel.Visibility = Visibility.Collapsed;
            AddProducts.Visibility = Visibility.Collapsed;
        }
        private void Button_Click_1(object sender, RoutedEventArgs e)
        { 
            EditDeleteStackPanel.Visibility = Visibility.Visible;
            EditStackPanel.Visibility = Visibility.Collapsed;
            AddProducts.Visibility = Visibility.Collapsed;
        }

        private void Button_Click_2(object sender, RoutedEventArgs e)
        {
            AddStackPanel.Visibility = Visibility.Visible;
            AddProducts.Visibility = Visibility.Collapsed;
            EditDeleteStackPanel.Visibility = Visibility.Collapsed;
        }

        private void DataProducts_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (OpenButtton.Visibility == Visibility.Collapsed)
            {
                Console.WriteLine("Я тут");
                EditDeleteStackPanel.Visibility = Visibility.Visible;
                AddStackPanel.Visibility = Visibility.Collapsed;
                EditStackPanel.Visibility = Visibility.Collapsed;
                AddProducts.Visibility = Visibility.Collapsed;
                if (DataProducts.SelectedItem is Products products)
                    Current_id = products.ID;
                Console.WriteLine(Current_id);
                SetData();
            }
        }

        private void Button_Click_3(object sender, RoutedEventArgs e)
        {
            if (DataProducts.SelectedItem is Products products)
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
        public void UpdateDataSupplierProducts(int product_id, decimal delivery_price, int supplier_id)
        {
            var conn = new NpgsqlConnection(Connecting);
            conn.Open();

            using (var cmd = new NpgsqlCommand("UPDATE public.product_suppliers\r\n\tSET product_id=@product_id, delivery_price=@delivery_price, supplier_id=@supplier_id\r\n\tWHERE product_id = @ID;", conn))
            {
                cmd.Parameters.AddWithValue("product_id", product_id);
                cmd.Parameters.AddWithValue("ID", product_id);
                cmd.Parameters.AddWithValue("delivery_price", delivery_price);
                cmd.Parameters.AddWithValue("supplier_id", supplier_id);
                
                cmd.ExecuteNonQuery();
            }
        }
        public void UpdateDataProducts()
        {
            products.Clear();

            var conn = new NpgsqlConnection(Connecting);
            conn.Open();
            using (var cmd = new NpgsqlCommand(
                "SELECT products.id, products.products_name, products.product_article, products.price, " +
                "product_suppliers.delivery_price, status.status_name " +
                "FROM public.products " +
                "LEFT JOIN product_suppliers ON product_suppliers.product_id = products.id " +
                "LEFT JOIN status ON status.id = products.status_id", conn))
            using (var reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    products.Add(new Products()
                    {
                        ID = reader.GetInt32(0),
                        NAME = reader.GetString(1),
                        ARTICULE = reader.GetString(2),
                        PRICE_PRODUCTS = reader.GetDecimal(3),
                        PRICE_SHIPPING = reader.IsDBNull(4) ? 0m : reader.GetDecimal(4),
                        STATUS = reader.GetString(5)
                    });
                }
                reader.Close();
            }
        }

        public void InsertDataSupplierProducts(int product_id, decimal delivery_price, int supplier_id)
        {
            var conn = new NpgsqlConnection(Connecting);
            conn.Open();

            using (var cmd = new NpgsqlCommand("INSERT INTO public.product_suppliers(\r\n\tproduct_id, delivery_price, supplier_id)\r\n\tVALUES (@product_id, @delivery_price, @supplier_id);", conn))
            {
                cmd.Parameters.AddWithValue("product_id", product_id);
                cmd.Parameters.AddWithValue("delivery_price", delivery_price);
                cmd.Parameters.AddWithValue("supplier_id", supplier_id);

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
            Console.WriteLine(id);

            int id_supplier = InsertOnUpdateProducts(id);

            
            
            Console.WriteLine(id_supplier);

            if (EditNameProduct.SelectedItem == null || EditSuppliersProducts.SelectedItem == null)
            {
                MessageBox.Show("Пожалуйста, введите данные", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            else 
            {
                if (id_supplier == -1)
                {
                    if (EditSuppliersProducts.SelectedItem is ComboBoxItemEx selectede)
                        id_supplier = selectede.Id;
                            
                    InsertDataSupplierProducts(id, Convert.ToDecimal(EditPriceProducts.Text), id_supplier);
                    MessageBox.Show("Данные обновлены", "Успешно", MessageBoxButton.OK, MessageBoxImage.Information);
                    UpdateDataProducts();
                }
                else
                {
                    UpdateDataSupplierProducts(id, Convert.ToDecimal(EditPriceProducts.Text), id_supplier);
                    MessageBox.Show("Данные обновлены", "Успешно", MessageBoxButton.OK, MessageBoxImage.Information);
                    UpdateDataProducts();

                }
            }
            //Если у меня сейчас на данном id нету поставщика товара, то я буду делать insert если же id уже есть то про update
        }
        public void LoadCategoryProducts()
        {
            CategoryProducts.Items.Clear();
            var conn = new NpgsqlConnection(Connecting);
            conn.Open();
            using (var cmd = new NpgsqlCommand("SELECT name FROM public.product_categories", conn))
            using (var reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    if (!reader.IsDBNull(0))
                        CategoryProducts.Items.Add(reader.GetString(0));
                }
                reader.Close();
            }
        }

        public void LoadStatusProducts()
        {
            StatusProducts.Items.Clear();
            var conn = new NpgsqlConnection(Connecting);
            conn.Open();
            using (var cmd = new NpgsqlCommand("SELECT status_name FROM public.status", conn))
            using (var reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    if (!reader.IsDBNull(0))
                        StatusProducts.Items.Add(reader.GetString(0));
                }
                reader.Close();
            }
        }

        public void InsertProducts(string name, int category_id, string article, decimal price, int status_id)
        {

            var conn = new NpgsqlConnection(Connecting);
            conn.Open();
            int newId = 1;
            using (var selectCmd = new NpgsqlCommand("SELECT COALESCE(MAX(id), 0) + 1 FROM public.products", conn))
            {
                newId = Convert.ToInt32(selectCmd.ExecuteScalar());
            }
            using (var cmd = new NpgsqlCommand(
                "INSERT INTO public.products (id, products_name, category_id, product_article, price, status_id) " +
                "VALUES (@id ,@name, @category_id, @article, @price, @status_id);", conn))
            {
                cmd.Parameters.AddWithValue("id", newId);
                cmd.Parameters.AddWithValue("name", name);
                cmd.Parameters.AddWithValue("category_id", category_id);
                cmd.Parameters.AddWithValue("article", article);
                cmd.Parameters.AddWithValue("price", price);
                cmd.Parameters.AddWithValue("status_id", status_id);
                cmd.ExecuteNonQuery();
            }
        }


        private void Button_Click_5(object sender, RoutedEventArgs e)
        {
            string name = NameProducts.Text;
            string article = ArticleProducts.Text;
            decimal price = 0m;
            decimal.TryParse(PriceProducts.Text, out price);

            int category_id = CategoryProducts.SelectedIndex + 1 ;
            int status_id = StatusProducts.SelectedIndex + 1;

            if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(article)
                || category_id < 0 || status_id < 0)
            {
                MessageBox.Show("Пожалуйста, заполните все поля", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            InsertProducts(name, category_id, article, price, status_id);
            MessageBox.Show("Товар добавлен", "Успешно", MessageBoxButton.OK, MessageBoxImage.Information);
            UpdateDataProducts();
        }

        private void Button_Click_6(object sender, RoutedEventArgs e)
        {
            AddProducts.Visibility = Visibility.Visible;
            AddStackPanel.Visibility = Visibility.Collapsed;
            EditDeleteStackPanel.Visibility = Visibility.Collapsed;
            EditStackPanel.Visibility = Visibility.Collapsed;
        }

        private void Button_Click_7(object sender, RoutedEventArgs e)
        {
            AddProducts.Visibility = Visibility.Collapsed;
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
            DataProducts.Width = 800;
            Grid.SetColumnSpan(DataProducts, 2);
        }

        private void Button_Click_10(object sender, RoutedEventArgs e)
        {
            GroupBoxAddProducts.Visibility = Visibility.Visible;
            CloseButton.Visibility = Visibility.Visible;
            OpenButtton.Visibility = Visibility.Collapsed;
            DataProducts.Width = 400;
            Grid.SetColumnSpan(DataProducts, 1);
        }

        

        private void TextBoxSeatch_GotFocus(object sender, RoutedEventArgs e)
        {
            if (TextBoxSeatch.Text == string.Empty || TextBoxSeatch.Text == "Нечеткий поиск")
            {
                TextBoxSeatch.Text = string.Empty;
            }
            ButtonSeatch.Visibility = Visibility.Visible;
            ButtonExit.Visibility = Visibility.Collapsed;
                
        }

        private void TextBoxSeatch_LostFocus(object sender, RoutedEventArgs e)
        {
            if(TextBoxSeatch.Text == string.Empty)
            {
                TextBoxSeatch.Text = "Нечеткий поиск";
                ButtonExit.Visibility = Visibility.Collapsed;
            }
            else
            {
                ButtonExit.Visibility = Visibility.Visible;
            }
            ButtonSeatch.Visibility = Visibility.Collapsed;
           
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

        public void SearchDataProducts(string word)
        {
            products.Clear();
            string word2 = String.Empty;

            var conn = new NpgsqlConnection(Connecting);
            conn.Open();
            using (var cmd = new NpgsqlCommand(
                "SELECT products.id, products.products_name, products.product_article, products.price,\r\nproduct_suppliers.delivery_price," +
                " status.status_name\r\nFROM public.products\r\nLEFT JOIN product_suppliers ON product_suppliers.product_id = products.id\r\nLEFT JOIN status ON " +
                "status.id = products.status_id\r\nWHERE abs(LENGTH(products.products_name) - LENGTH(@word)) < 4\r\n", conn))
            {
            cmd.Parameters.AddWithValue("word", word);
            using (var reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                        word2 = reader.GetString(1);
                        Console.WriteLine(LevenshteinDistance(word, word2));
                        if (LevenshteinDistance(word, word2) <= 3)
                        {
                            
                            products.Add(new Products()
                            {
                                ID = reader.GetInt32(0),
                                NAME = reader.GetString(1),
                                ARTICULE = reader.GetString(2),
                                PRICE_PRODUCTS = reader.GetDecimal(3),
                                PRICE_SHIPPING = reader.IsDBNull(4) ? 0m : reader.GetDecimal(4),
                                STATUS = reader.GetString(5)
                            });
                        }

                }

                reader.Close();
            }
        }
        }
        private void ButtonExit_Click(object sender, RoutedEventArgs e)
        {
            ButtonExit.Visibility = Visibility.Collapsed;
            UpdateDataProducts();
            TextBoxSeatch.Text = string.Empty;


        }

        private void ButtonSeatch_Click_1(object sender, RoutedEventArgs e)
        {
            ButtonExit.Visibility = Visibility.Visible;

            SearchDataProducts(TextBoxSeatch.Text);
            ButtonSeatch.Visibility = Visibility.Collapsed;
        }
    }
}
