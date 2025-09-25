using Npgsql;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
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
    public class Vibor
    {
        public int products_id {  get; set; }
        public int suppliers_id { get; set; }
        public bool CheckProducts {  get; set; }
        public string NAME { get; set; }
        public string SUPPLIER {  get; set; }
        public int QTY { get; set; }
    }
    public partial class Create_orders : Window
    {
        public ObservableCollection<Vibor> vibor { get; set; }
        public Create_orders()
        {
            InitializeComponent();
            vibor = new ObservableCollection<Vibor>();
            LoadComboBoxVibor();
            DataProducts.ItemsSource = vibor;
            ComboBoxPredOplata1.Visibility = Visibility.Collapsed;


        }
        public void LoadDataGrid(int status)
        {
            var conn = new NpgsqlConnection(Products_is_true.Connecting);
            vibor.Clear();
            conn.Open();

                using (var cmd = new NpgsqlCommand("select products.id, suppliers.id, products.products_name, suppliers.company_name\r\nfrom product_suppliers inner join products on products.id = product_suppliers.product_id\r\ninner join  suppliers on suppliers.id = product_suppliers.supplier_id\r\nwhere products.status_id = @id", conn))
                {
                    cmd.Parameters.AddWithValue("id", status);
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            vibor.Add(new Vibor()
                            {
                                products_id = reader.GetInt32(0),
                                suppliers_id = reader.GetInt32(1),
                                CheckProducts = false,
                                NAME = reader.GetString(2),
                                SUPPLIER = reader.GetString(3),
                                QTY = 0
                            });
                        }

                        reader.Close();
                    }
                }
            
        }
        private void NumberValidationTextBox(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !int.TryParse(e.Text, out int value) || value < 0;
        }

        public void LoadComboBoxVibor()
        {
            var conn = new NpgsqlConnection(Products_is_true.Connecting);
            conn.Open();

            using (var cmd = new NpgsqlCommand("SELECT id, status_name\r\n\tFROM public.status", conn))
            using (var reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    var item = new ComboBoxItemEx
                    {
                        Id = reader.GetInt32(0),
                        Name = reader.GetString(1)
                    };
                    ComboBoxTypeOrders.Items.Add(new ComboBoxItemEx
                    {
                        Id = item.Id,
                        Name = item.Name
                    });
                }
                reader.Close();
            }
        }

        private void ComboBoxTypeOrders_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (((ComboBoxItemEx)ComboBoxTypeOrders.SelectedItem).Name == "В наличии")
            {
                LoadDataGrid(1);
                ComboBoxPredOplata1.Visibility = Visibility.Collapsed;


            }
            else if (((ComboBoxItemEx)ComboBoxTypeOrders.SelectedItem).Name == "Под заказ")
            {
                LoadDataGrid(2);
                ComboBoxPredOplata1.Visibility = Visibility.Visible;
            }
            else
            {
                vibor.Clear();
            }
            
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (ComboBoxTypeOrders.SelectedItem == null)
                {
                    MessageBox.Show("Выберите тип продажи (В наличии / Под заказ)", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var selectedType = ((ComboBoxItemEx)ComboBoxTypeOrders.SelectedItem).Name;
                var selectedTypeId = ((ComboBoxItemEx)ComboBoxTypeOrders.SelectedItem).Id;

                var selectedProducts = vibor.Where(v => v.CheckProducts && v.QTY > 0).ToList();
                if (selectedProducts.Count == 0)
                {
                    MessageBox.Show("Выберите хотя бы один товар с количеством больше 0", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                int? prepaymentPercent = null;
                if (selectedType == "Под заказ")
                {
                    if (ComboBoxPredOplata1.SelectedItem == null)
                    {
                        MessageBox.Show("Выберите размер предоплаты (50% / 100%)", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }
                    var selectedPrepaymentItem = (ComboBoxItem)ComboBoxPredOplata1.SelectedItem;
                    var selectedPrepaymentText = ((TextBlock)selectedPrepaymentItem.Content).Text;
                    prepaymentPercent = selectedPrepaymentText == "50%" ? 50 : 100;
                }

                using (var conn = new NpgsqlConnection(Products_is_true.Connecting))
                {
                    conn.Open();
                    using (var transaction = conn.BeginTransaction())
                    {
                        try
                        {

                            if (selectedType == "В наличии")
                            {
                                foreach (var product in selectedProducts)
                                {
                                    using (var cmd = new NpgsqlCommand("SELECT COALESCE(available_quantity, 0) FROM inventory WHERE product_id = @productId", conn))
                                    {
                                        cmd.Parameters.AddWithValue("@productId", product.products_id);
                                        var available = Convert.ToInt32(cmd.ExecuteScalar() ?? 0);

                                        if (available < product.QTY)
                                        {
                                            MessageBox.Show($"Недостаточно товара '{product.NAME}' на складе. Доступно: {available}, требуется: {product.QTY}",
                                                "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                                            transaction.Rollback();
                                            return;
                                        }
                                    }
                                }
                            }

                            int personId;
                            using (var cmd = new NpgsqlCommand("SELECT person_id FROM users WHERE id = @userId", conn))
                            {
                                cmd.Parameters.AddWithValue("@userId", NewOrdersWindow.idUsers);
                                personId = (int)cmd.ExecuteScalar();
                            }

                            decimal totalAmount = 0;
                            foreach (var product in selectedProducts)
                            {
                                decimal productPrice = GetProductPrice(product.products_id, conn);
                                decimal deliveryPrice = selectedType == "Под заказ" ? GetDeliveryPrice(product.products_id, product.suppliers_id, conn) : 0;
                                totalAmount += (productPrice + deliveryPrice) * product.QTY;
                            }

                            int orderId;
                            using (var cmd = new NpgsqlCommand(@"
                        INSERT INTO orders (person_id, user_id, status_id, prepayment_percent, total_amount, order_status, order_date, order_number) 
                        VALUES (@personId, @userId, @statusId, @prepaymentPercent, @totalAmount, 'Создан', NOW(), @number) 
                        RETURNING id", conn))
                            {
                                cmd.Parameters.AddWithValue("@personId", personId);
                                cmd.Parameters.AddWithValue("@userId", NewOrdersWindow.idUsers);
                                cmd.Parameters.AddWithValue("@statusId", selectedTypeId);
                                cmd.Parameters.AddWithValue("@prepaymentPercent", (object)prepaymentPercent ?? DBNull.Value);
                                cmd.Parameters.AddWithValue("@totalAmount", totalAmount);
                                cmd.Parameters.AddWithValue("@number", GenerateOrderNumber());
                                orderId = (int)cmd.ExecuteScalar();
                            }

                            foreach (var product in selectedProducts)
                            {
                     
                                using (var cmd = new NpgsqlCommand("SELECT COALESCE(available_quantity, 0) FROM inventory WHERE product_id = @productId", conn))
                                {
                                    cmd.Parameters.AddWithValue("@productId", product.products_id);
                                    var available = Convert.ToInt32(cmd.ExecuteScalar() ?? 0);

                                    if (available > 0)
                                    {
                                        using (var reserveCmd = new NpgsqlCommand(
                                            @"UPDATE inventory
                                    SET reserved_quantity = reserved_quantity + @qty
                                    WHERE product_id = @productId", conn))
                                        {
                                            reserveCmd.Parameters.AddWithValue("@qty", product.QTY);
                                            reserveCmd.Parameters.AddWithValue("@productId", product.products_id);
                                            reserveCmd.ExecuteNonQuery();
                                        }

                                        using (var moveCmd = new NpgsqlCommand(
                                            @"INSERT INTO inventory_movements 
                                    (product_id, movement_type, quantity, order_id, movement_date, notes)
                                    VALUES (@productId, 'Резервирование', @qty, @orderId, NOW(), 'Резерв при создании заявки')", conn))
                                        {
                                            moveCmd.Parameters.AddWithValue("@productId", product.products_id);
                                            moveCmd.Parameters.AddWithValue("@qty", product.QTY);
                                            moveCmd.Parameters.AddWithValue("@orderId", orderId);
                                            moveCmd.ExecuteNonQuery();
                                        }
                                    }
                                }
                            }

                            foreach (var product in selectedProducts)
                            {
                                decimal productPrice = GetProductPrice(product.products_id, conn);
                                decimal deliveryPrice = selectedType == "Под заказ" ? GetDeliveryPrice(product.products_id, product.suppliers_id, conn) : 0;
                                decimal unitPrice = productPrice + deliveryPrice;

                                using (var cmd = new NpgsqlCommand(@"
                            INSERT INTO orders_products (order_id, product_id, supplier_id, quantity, unit_price) 
                            VALUES (@orderId, @productId, @supplierId, @quantity, @unitPrice)", conn))
                                {
                                    cmd.Parameters.AddWithValue("@orderId", orderId);
                                    cmd.Parameters.AddWithValue("@productId", product.products_id);
                                    cmd.Parameters.AddWithValue("@supplierId", product.suppliers_id);
                                    cmd.Parameters.AddWithValue("@quantity", product.QTY);
                                    cmd.Parameters.AddWithValue("@unitPrice", unitPrice);
                                    cmd.ExecuteNonQuery();
                                }
                            }

                            
                            if (selectedType == "Под заказ")
                            {
                          
                                foreach (var product in selectedProducts)
                                {
                  
                                    bool existsInInventory = false;
                                    using (var checkCmd = new NpgsqlCommand(
                                        "SELECT COUNT(*) FROM inventory WHERE product_id = @productId", conn, transaction))
                                    {
                                        checkCmd.Parameters.AddWithValue("@productId", product.products_id);
                                        existsInInventory = Convert.ToInt32(checkCmd.ExecuteScalar()) > 0;
                                    }

                                    if (!existsInInventory)
                                    {
                    
                                        using (var insertCmd = new NpgsqlCommand(
                                            @"INSERT INTO inventory (product_id, quantity_in_stock, reserved_quantity) 
          VALUES (@productId, @qty, @qty)", conn, transaction))
                                        {
                                            insertCmd.Parameters.AddWithValue("@productId", product.products_id);
                                            insertCmd.Parameters.AddWithValue("@qty", product.QTY);
                                            insertCmd.ExecuteNonQuery();
                                        }
                                    }
                                    else
                                    {
                                  
                                        using (var updateCmd = new NpgsqlCommand(
                                            @"UPDATE inventory 
          SET quantity_in_stock = quantity_in_stock + @qty,
              reserved_quantity = reserved_quantity + @qty
          WHERE product_id = @productId", conn, transaction))
                                        {
                                            updateCmd.Parameters.AddWithValue("@qty", product.QTY);
                                            updateCmd.Parameters.AddWithValue("@productId", product.products_id);
                                            updateCmd.ExecuteNonQuery();
                                        }
                                    }

                                    using (var moveCmd = new NpgsqlCommand(
                                        @"INSERT INTO inventory_movements 
      (product_id, movement_type, quantity, order_id, movement_date, notes)
      VALUES (@productId, 'Поступление', @qty, @orderId, NOW(), 'Поступление товара под заказ')", conn, transaction))
                                    {
                                        moveCmd.Parameters.AddWithValue("@productId", product.products_id);
                                        moveCmd.Parameters.AddWithValue("@qty", product.QTY);
                                        moveCmd.Parameters.AddWithValue("@orderId", orderId);
                                        moveCmd.ExecuteNonQuery();
                                    }
                                    using (var reserveCmd = new NpgsqlCommand(
                                        @"INSERT INTO inventory_movements 
      (product_id, movement_type, quantity, order_id, movement_date, notes)
      VALUES (@productId, 'Резервирование', @qty, @orderId, NOW(), 'Резерв при создании заявки под заказ')", conn, transaction))
                                    {
                                        reserveCmd.Parameters.AddWithValue("@productId", product.products_id);
                                        reserveCmd.Parameters.AddWithValue("@qty", product.QTY);
                                        reserveCmd.Parameters.AddWithValue("@orderId", orderId);
                                        reserveCmd.ExecuteNonQuery();
                                    }

                                }

                            }


                            decimal invoiceAmount = prepaymentPercent.HasValue
                                ? totalAmount * prepaymentPercent.Value / 100
                                : totalAmount;

                            string pdfFileName = $"invoice_{orderId}_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";
                            string pdfFilePath = System.IO.Path.Combine("invoices", pdfFileName);

                            using (var cmd = new NpgsqlCommand(@"
                        INSERT INTO invoices (order_id, invoice_date, amount_to_pay, pdf_file_path) 
                        VALUES (@orderId, NOW(), @amountToPay, @pdfFilePath)", conn))
                            {
                                cmd.Parameters.AddWithValue("@orderId", orderId);
                                cmd.Parameters.AddWithValue("@amountToPay", invoiceAmount);
                                cmd.Parameters.AddWithValue("@pdfFilePath", pdfFilePath);
                                cmd.ExecuteNonQuery();
                            }

                            transaction.Commit();

                            string orderNumber;
                            using (var cmd = new NpgsqlCommand("SELECT order_number FROM orders WHERE id = @orderId", conn))
                            {
                                cmd.Parameters.AddWithValue("@orderId", orderId);
                                orderNumber = cmd.ExecuteScalar()?.ToString() ?? orderId.ToString();
                            }

                            MessageBox.Show($"Заявка {orderNumber} успешно создана!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);

                            MainWindow main = new MainWindow();
                            main.Show();
                            this.Close();
                        }
                        catch (Exception ex)
                        {
                            transaction.Rollback();
                            MessageBox.Show($"Ошибка при создании заявки: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }


        private string GenerateOrderNumber()
        {
            return Guid.NewGuid().ToString();
        }
        private decimal GetProductPrice(int productId, NpgsqlConnection conn)
        {
            using (var cmd = new NpgsqlCommand("SELECT price FROM products WHERE id = @productId", conn))
            {
                cmd.Parameters.AddWithValue("@productId", productId);
                var result = cmd.ExecuteScalar();
                return result != null ? Convert.ToDecimal(result) : 0;
            }
        }

        private decimal GetDeliveryPrice(int productId, int supplierId, NpgsqlConnection conn)
        {
            using (var cmd = new NpgsqlCommand("SELECT delivery_price FROM product_suppliers WHERE product_id = @productId AND supplier_id = @supplierId", conn))
            {
                cmd.Parameters.AddWithValue("@productId", productId);
                cmd.Parameters.AddWithValue("@supplierId", supplierId);
                var result = cmd.ExecuteScalar();
                Console.WriteLine(result);
                return result != null ? Convert.ToDecimal(result) : 0;
            }
        }

        //private void GenerateInvoicePDF(int orderId, string pdfFilePath, NpgsqlConnection conn)
        //{
        //    System.IO.File.WriteAllText(pdfFilePath, $"Счет для заявки №{orderId}");
        //}


        private void Button_Click_8(object sender, RoutedEventArgs e)
        {
            NewOrdersWindow create_Orders = new NewOrdersWindow();
            create_Orders.Show();
            this.Close();
        }
    }
}
