using Npgsql;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Demo
{
    public class NewOrders
    {
        public int oldCountOrders = 0;
        public int newCountOrders = 0;

        public NewOrders(int count) 
        {
            oldCountOrders = count;
            newCountOrders = count;
        }
        public List<string> NewOrdersF()
        {
            List<string> list = new List<string>();
            using (var conn = new NpgsqlConnection(Products_is_true.Connecting))
            {
                conn.Open();

                string sql = @"select count(*) as count_ from orders";
                int old = newCountOrders, countNow = 0;

                using (var cmd = new NpgsqlCommand(sql, conn))
                {
                    countNow = Convert.ToInt32(cmd.ExecuteScalar());
                }

                if (Math.Abs(countNow - oldCountOrders) >= 1)
                {
                    string sql2 = @"SELECT persons.last_name || ' ' || persons.first_name, order_number 
                            FROM public.orders 
                            inner join persons on persons.id = orders.person_id
                            ORDER BY orders.id DESC LIMIT 1";

                    using (var cmd2 = new NpgsqlCommand(sql2, conn))
                    using (var read2 = cmd2.ExecuteReader())
                    {
                        while (read2.Read())
                        {
                            string word = read2.GetString(0) + " " + read2.GetString(1);
                            list.Add(word);
                        }
                    }
                    oldCountOrders = countNow; 
                }
                else
                {
                    return null;
                }
            }
            return list;
        }

    }
}
