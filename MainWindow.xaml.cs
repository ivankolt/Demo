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
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace Demo
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private DispatcherTimer ordersTimer;
        private NewOrders newOrdersChecker;
        private bool notificationActive = false;


        public MainWindow()
        {
            InitializeComponent();
            int count = GetCountOrders();
            newOrdersChecker = new NewOrders(count);

            GridNoti.Visibility = Visibility.Collapsed;

            ordersTimer = new DispatcherTimer();
            ordersTimer.Interval = TimeSpan.FromSeconds(10);
            ordersTimer.Tick += OrdersTimer_Tick;
            ordersTimer.Start();
        }
        private async void OrdersTimer_Tick(object sender, EventArgs e)
        {
            if (notificationActive) return;

            List<string> newOrders = newOrdersChecker.NewOrdersF();

            if (newOrders != null && newOrders.Count > 0)
            {
                ListNewOrders.Children.Clear();
                foreach (var txt in newOrders)
                {
                    string newTxt = "У вас новый заказ от клиента " + txt;
                    ListNewOrders.Children.Add(new TextBlock
                    {
                        Text = newTxt,
                        FontWeight = FontWeights.Bold,
                        FontSize = 15,
                        Margin = new Thickness(4, 4, 0, 0)
                    });
                }
                GridNoti.Visibility = Visibility.Visible;
                notificationActive = true;

                await Task.Delay(10000);

                GridNoti.Visibility = Visibility.Collapsed;
                ListNewOrders.Children.Clear();
                notificationActive = false;
            }
        }


        public int GetCountOrders()
        {
            int count = 0;
            using (var conn = new NpgsqlConnection(Products_is_true.Connecting))
            {
                conn.Open();

                string sql = @"select count(*) as count_ from orders";

                using (var cmd = new NpgsqlCommand(sql, conn))
                {

                    using (var read = cmd.ExecuteReader())
                    {
                        while (read.Read())
                        {
                            count = read.GetInt32(0);
                        }
                    }
                }
            }
            return count;
        }
        

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Products_is_true products_Is_True = new Products_is_true();
            products_Is_True.Show();
            this.Close();
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            SupplierWindow supplierWindow = new SupplierWindow();
            supplierWindow.Show();
            this.Close();
        }

        private void Button_Click_2(object sender, RoutedEventArgs e)
        {
            Products_Suppliers products_Suppliers = new Products_Suppliers();
            products_Suppliers.Show();
            this.Close();
        }

        private void Button_Click_3(object sender, RoutedEventArgs e)
        {
            NewOrdersWindow newOrdersWindow = new NewOrdersWindow();
            newOrdersWindow.Show();
            this.Close();
        }

        private void Button_Click_4(object sender, RoutedEventArgs e)
        {
            New_payments new_Payments = new New_payments();
            new_Payments.Show();
            this.Close();
        }

        private void Button_Click_5(object sender, RoutedEventArgs e)
        {
            EditOrders editOrders = new EditOrders();
            editOrders.Show();
            this.Close();
        }
    }
}
