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
    using System.Collections.ObjectModel;
    using System.ComponentModel;

    public class OrderRow : INotifyPropertyChanged
    {
        public int NumberOrdersId { get; set; }     
        public string NAME { get; set; }          
        public string SUPPLIER { get; set; }          
        public string PAYMETNS { get; set; }           
        public string STATUS { get; set; }             
        public string MOVE { get; set; }  
        public List<string> StatusList { get; set; }
        public int PrepaymentPercent { get; set; }


        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged(string prop) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
    }

    public partial class EditOrders : Window
    {
        private static readonly List<string> StatusSteps = new List<string>
        {
            "Создан",
            "Оплачен частично",
            "Оплачен полностью",
            "Поступил на склад",
            "Поступил на склад ожидает оплаты",
            "Готов к отгрузке",
            "Завершен"
        };

        public ObservableCollection<OrderRow> OrdersList { get; set; } = new ObservableCollection<OrderRow>();

        public EditOrders()
        {
            InitializeComponent();
            FillAllCustomersComboBox();
            DataProducts.ItemsSource = OrdersList;
            LoadAllOrders();
        }

        public void LoadAllOrders()
        {
            OrdersList.Clear();

            using (var conn = new NpgsqlConnection(Products_is_true.Connecting))
            {
                conn.Open();
                List<string> statusList = new List<string>();
                using (var cmdStatuses = new NpgsqlCommand("SELECT status_name FROM status ORDER BY id", conn))
                using (var readerStatuses = cmdStatuses.ExecuteReader())
                {
                    while (readerStatuses.Read())
                    {
                        statusList.Add(readerStatuses.GetString(0));
                    }
                }
                 
                string sql =
    @"SELECT o.id, o.order_date, p.last_name || ' ' || p.first_name AS fio, o.total_amount, o.order_status, o.prepayment_percent
      FROM orders o
      INNER JOIN persons p ON p.id = o.person_id
      ORDER BY o.order_date DESC";

                using (var cmd = new NpgsqlCommand(sql, conn))
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        OrdersList.Add(new OrderRow
                        {
                            NumberOrdersId = reader.GetInt32(0),
                            NAME = reader.GetDateTime(1).ToString("yyyy-MM-dd HH:mm"),
                            SUPPLIER = reader.GetString(2),
                            PAYMETNS = reader.GetDecimal(3).ToString("F2"),
                            STATUS = reader.GetString(4),
                            PrepaymentPercent = reader.IsDBNull(5) ? 0 : reader.GetInt32(5),
                            MOVE = "Действие"
                        });

                    }
                }
            }
        }

        public void FillAllCustomersComboBox()
        {
            AllCustomers.Items.Clear();

            using (var conn = new NpgsqlConnection(Products_is_true.Connecting))
            {
                conn.Open();
                using (var cmd = new NpgsqlCommand("SELECT id, last_name || ' ' || first_name AS fio FROM persons ORDER BY last_name, first_name", conn))
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        int id = reader.GetInt32(0);
                        string fio = reader.GetString(1);

                        ComboBoxItem item = new ComboBoxItem
                        {
                            Content = fio,
                            Tag = id
                        };
                        AllCustomers.Items.Add(item);
                    }
                }
            }
        }

        private void Button_Click_8(object sender, RoutedEventArgs e)
        {
            MainWindow mainWindow = new MainWindow();
            mainWindow.Show();
            this.Close();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as Button;
            var orderRow = btn.DataContext as OrderRow;
            if (orderRow == null) return;

            if (orderRow.STATUS != "Завершен")
            {
                UpdateOrderStatus(orderRow.NumberOrdersId, "Отменен");
                orderRow.STATUS = "Отменен";
            }

            orderRow.OnPropertyChanged(nameof(orderRow.STATUS));
        }


        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            var btn = sender as Button;
            var orderRow = btn.DataContext as OrderRow;
            if (orderRow == null) return;

            string nextStatus = null;
            switch (orderRow.STATUS)
            {
                case "Создан":
                    if (orderRow.PrepaymentPercent == 100)
                        nextStatus = "Оплачен полностью";
                    else if (orderRow.PrepaymentPercent == 50)
                        nextStatus = "Оплачен частично";
                    break;
                case "Оплачен частично":
                    nextStatus = "Поступил на склад, ожидает оплаты";
                    break;
                case "Оплачен полностью":
                    nextStatus = "Поступил на склад";
                    break;
                case "Поступил на склад":
                case "Поступил на склад, ожидает оплаты":
                    nextStatus = "Готов к отгрузке";
                    break;
                case "Готов к отгрузке":
                    nextStatus = "Завершен";
                    break;
                case "Завершен":
                    MessageBox.Show("Заявка уже завершена!");
                    return;
                default:
                    return;
            }

            UpdateOrderStatus(orderRow.NumberOrdersId, nextStatus);
            orderRow.STATUS = nextStatus;
            orderRow.OnPropertyChanged(nameof(orderRow.STATUS));

            if (nextStatus == "Завершен")
            {
                UpdateInventoryOnOrderComplete(orderRow.NumberOrdersId);
            }
        }

        private void UpdateOrderStatus(int orderId, string newStatus)
        {
            using (var conn = new NpgsqlConnection(Products_is_true.Connecting))
            {
                conn.Open();
                using (var cmd = new NpgsqlCommand("UPDATE orders SET order_status = @status::order_status WHERE id = @id", conn))
                {
                    cmd.Parameters.AddWithValue("@status", newStatus);
                    cmd.Parameters.AddWithValue("@id", orderId);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        private void UpdateInventoryOnOrderComplete(int orderId)
        {
            using (var conn = new NpgsqlConnection(Products_is_true.Connecting))
            {
                conn.Open();
                using (var cmd = new NpgsqlCommand(@"
            UPDATE inventory
            SET reserved_quantity = reserved_quantity - op.quantity
            FROM orders_products op
            WHERE op.order_id = @orderId AND inventory.product_id = op.product_id", conn))
                {
                    cmd.Parameters.AddWithValue("@orderId", orderId);
                    cmd.ExecuteNonQuery();
                }
                using (var cmd = new NpgsqlCommand(@"
            INSERT INTO inventory_movements (product_id, movement_type, quantity, order_id, movement_date, notes)
            SELECT op.product_id, 'Продажа', op.quantity, @orderId, NOW(), 'Завершение заказа'
            FROM orders_products op
            WHERE op.order_id = @orderId", conn))
                {
                    cmd.Parameters.AddWithValue("@orderId", orderId);
                    cmd.ExecuteNonQuery();
                }
            }
        }


    }
}
