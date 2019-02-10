using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Net.NetworkInformation;
using System.Timers;

namespace PingMonitoring
{
    class Program
    {
        public static MySqlConnection conn = DBUtils.GetDBConnection();
        public static Ping PingSender = new Ping();

        static void Main(string[] args)
        {
            try
            {
                Console.WriteLine("Открываю соединение...");
                conn.Open();
                Console.WriteLine("Соединение установлено");
            }
            catch (Exception e)
            {
                Console.WriteLine("Ошибка: " + e.Message);
            }

            Timer t = new Timer(30000)
            {
                AutoReset = true,
                Enabled = true

            };
            t.Elapsed += T_Elapsed;
            T_Elapsed(null, null);
            while (true)
                Console.ReadKey();
        }

        private static void T_Elapsed(object sender, ElapsedEventArgs e)
        {
            Console.WriteLine("Запрашиваю список устройств для проверки соединения");
            using (MySqlCommand comm = conn.CreateCommand())
            {
                comm.CommandType = System.Data.CommandType.Text;
                comm.CommandText = "SELECT `Id`,`Address`,  `Name` FROM `devices` WHERE 1";
                comm.CacheAge = 30;

                List<PingDevice> pinglist = new List<PingDevice>();
                using (MySqlDataReader r = comm.ExecuteReader())
                {
                    PingDevice d;
                    while (r.Read())
                    {
                        d = new PingDevice()
                        {
                            Id = r.GetInt32(0),
                            Address = r.GetString(1),
                            Name = r.GetString(2)
                        };
                        pinglist.Add(d);
                    }
                }
                Console.WriteLine("Получено " + pinglist.Count + " устройств");
                bool Trouble = false;
                foreach (PingDevice d in pinglist)
                {
                    d.TestConnection();
                }
                Console.WriteLine("Фиксирую изменения в БД");
                if (Trouble)
                    Console.Beep(18000, 1000);

                comm.CommandText = "UPDATE `devices` SET `Ping`=@Ping,`Status`=@Status WHERE `Id`=@Id";

                using (MySqlTransaction t = conn.BeginTransaction())
                {
                    comm.Transaction = t;
                    comm.Parameters.Add(new MySqlParameter("@Ping", MySqlDbType.Int32));
                    comm.Parameters.Add(new MySqlParameter("@Status", MySqlDbType.Int32));
                    comm.Parameters.Add(new MySqlParameter("@Id", MySqlDbType.Int32));
                    foreach (PingDevice d in pinglist) {
                        comm.Parameters["@Id"].Value = d.Id;
                        comm.Parameters["@Ping"].Value = d.Ping;
                        comm.Parameters["@Status"].Value = d.Status;
                        comm.ExecuteNonQuery();
                    }
                    t.Commit();
                }
                Console.WriteLine("Изменения зафиксированы");
            }

        }
    }
}
