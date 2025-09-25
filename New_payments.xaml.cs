using iTextSharp.text;
using iTextSharp.text.pdf;
using Npgsql;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.ComTypes;
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
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Diagnostics;
using Org.BouncyCastle.Asn1.Cms;

namespace Demo
{
    /// <summary>
    /// Логика взаимодействия для New_payments.xaml
    /// </summary>
     public class OrderInvoiceInfo
    {
        public string OrderNumber { get; set; }
    public int OrderId { get; set; }
    public string ClientFIO { get; set; }
    public DateTime OrderDate { get; set; }
    public DateTime InvoiceDate { get; set; }
    public decimal AmountToPay { get; set; }
}

public partial class New_payments : Window
    {


private List<OrderInvoiceInfo> allInvoices = new List<OrderInvoiceInfo>();

        public New_payments()
        {
            InitializeComponent();
            LoadOrderNumbers();
            ComboPaymetns.SelectionChanged += ComboPaymetns_SelectionChanged;
        }

        private void LoadOrderNumbers()
        {
            allInvoices.Clear();
            ComboPaymetns.Items.Clear();
            using (var conn = new NpgsqlConnection(Products_is_true.Connecting))
            {
                conn.Open();
                // Берём только заказы, по которым уже есть счета
                string sql = @"
            SELECT 
            o.order_number, o.id, 
            p.last_name || ' ' || p.first_name || COALESCE(' '||p.middle_name, '') AS client_fio,
            o.order_date,
            i.invoice_date,
            i.amount_to_pay
            FROM orders o
            INNER JOIN invoices i ON i.order_id = o.id
            INNER JOIN persons p ON p.id = o.person_id
            where order_status = 'Создан'
            ORDER BY i.invoice_date DESC";

                using (var cmd = new NpgsqlCommand(sql, conn))
                using (var rdr = cmd.ExecuteReader())
                {
                    while (rdr.Read())
                    {
                        var info = new OrderInvoiceInfo
                        {
                            OrderNumber = rdr.GetString(0),
                            OrderId = rdr.GetInt32(1),
                            ClientFIO = rdr.GetString(2),
                            OrderDate = rdr.GetDateTime(3),
                            InvoiceDate = rdr.GetDateTime(4),
                            AmountToPay = rdr.GetDecimal(5)
                        };
                        allInvoices.Add(info);
                        ComboPaymetns.Items.Add(info.OrderNumber);
                    }
                }
            }
        }

        private void ComboPaymetns_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ComboPaymetns.SelectedItem == null) return;
            string selOrder = ComboPaymetns.SelectedItem.ToString();
            var orderInfo = allInvoices.FirstOrDefault(a => a.OrderNumber == selOrder);
            if (orderInfo == null) return;

            IdText.Text = orderInfo.OrderNumber;
            UsersText.Text = orderInfo.ClientFIO;
            Date.SelectedDate = orderInfo.OrderDate;
            DateOfromlenia.SelectedDate = orderInfo.InvoiceDate;
            AmountTotal.Text = orderInfo.AmountToPay.ToString("F2");
        }

        private void Button_Click_8(object sender, RoutedEventArgs e)
        {
            MainWindow mainWindow = new MainWindow();
            mainWindow.Show();
            this.Close();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            string fontPath = System.IO.Path.Combine(
    Environment.GetFolderPath(Environment.SpecialFolder.Fonts), "arial.ttf");
            BaseFont baseFont = BaseFont.CreateFont(fontPath, BaseFont.IDENTITY_H, BaseFont.EMBEDDED);
            Font font = new Font(baseFont, 12, Font.NORMAL);
            Font font2 = new Font(baseFont, 18, Font.BOLD);
            try
            {
                var path = @"C:\Users\HP\source\repos\Demo\Demo\invoices\"; // дословная строка
                DateTime dateTime = DateTime.Now;
                var dateNow = dateTime.ToString("yyyyMMdd_HHmmss"); // безопасный формат
                var fileName = $"Sample_{dateNow}.pdf";
                var fullPath = System.IO.Path.Combine(path, fileName);
                Document pdfdoc = new Document(PageSize.A5); // Setting the page size for the PDF
                PdfWriter.GetInstance(pdfdoc, new FileStream(fullPath, FileMode.Create)); //Using the PDF Writer class to generate the PDF
                pdfdoc.Open(); // Opening the PDF to write the data from the textbox
                pdfdoc.Add(new iTextSharp.text.Paragraph("Счет пользователя: " + UsersText.Text, font2));
                pdfdoc.Add(new iTextSharp.text.Paragraph(" __________________________________", font2));
                pdfdoc.Add(new iTextSharp.text.Paragraph("Номер заявки: " + IdText.Text, font));
                string date_ = Date.Text;
                pdfdoc.Add(new iTextSharp.text.Paragraph("Дата заявки: " + date_, font));
                string date_2 = DateOfromlenia.Text;
                pdfdoc.Add(new iTextSharp.text.Paragraph("Дата оформления счёта: " + date_2, font));
                pdfdoc.Add(new iTextSharp.text.Paragraph("Сумма к оплате: " + AmountTotal.Text, font));

                pdfdoc.Close();
                MessageBox.Show("PDF Generation Successfull");
                MessageBox.Show("Ожидаем оплаты пользователя " + UsersText.Text, "Ожидаем оплаты", MessageBoxButton.OK);
                MainWindow mainWindow = new MainWindow();
                mainWindow.Show();
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
}


    }
}
