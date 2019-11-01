using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.NetworkInformation;
using System.Timers;
using System.Xml;

namespace PingMonitoring
{
    class Program
    {

        public static MySqlConnection conn;
        public static Ping PingSender = new Ping();
        public static byte[] packet = new byte[0];

        static void Main(string[] args)
        {
            XmlDocument cfg = new XmlDocument();
            cfg.Load(Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location) + @"\config.xml");

            conn = DBUtils.GetDBConnection(
                    cfg.DocumentElement.SelectSingleNode("IP").InnerText,
                    Convert.ToInt32(cfg.DocumentElement.SelectSingleNode("Port").InnerText),
                    cfg.DocumentElement.SelectSingleNode("Db").InnerText,
                    cfg.DocumentElement.SelectSingleNode("Login").InnerText,
                    cfg.DocumentElement.SelectSingleNode("Pass").InnerText

            );
            Console.WriteLine("Ip:\t" + cfg.DocumentElement.SelectSingleNode("IP").InnerText);
            Console.WriteLine("Порт:\t" + cfg.DocumentElement.SelectSingleNode("Port").InnerText);
            Console.WriteLine("БД:\t" + conn.Database);
           /* ArmdipParser.Update();
            Console.ReadKey();*/
            
            Timer t = new Timer(30000)
            {
                AutoReset = true,
                Enabled = true

            };
            t.Elapsed += Elapsed;
            Elapsed(null, null);
            while (true)
                Console.ReadKey();
        }

        private static void Elapsed(object sender, ElapsedEventArgs e)
        {
            Console.WriteLine("Открываю соединение...");
            while (true)
            {
                try
                {
                    conn.Open();
                    break;
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Ошибка: " + ex.Message);
                }
                System.Threading.Thread.Sleep(5000);
            }
            Console.WriteLine("Соединене открыто...");
            Console.WriteLine("Получаю сведения по обратному протоколу...");
            try
            {
                TrainboardState[] tsa = ArmdipParser.Update();
                using (MySqlCommand comm = conn.CreateCommand())
                {
                    comm.CommandType = System.Data.CommandType.Text;
                    comm.CommandText = "INSERT INTO armdip (Esr,Total,Damaged) VALUES(@esr,@total,@damaged) ON DUPLICATE KEY UPDATE total = @total, damaged = @damaged;";
                    comm.CacheAge = 30;
                    comm.Parameters.Add(new MySqlParameter("@esr", MySqlDbType.Int32));
                    comm.Parameters.Add(new MySqlParameter("@total", MySqlDbType.Byte));
                    comm.Parameters.Add(new MySqlParameter("@damaged", MySqlDbType.Byte));
                    foreach (TrainboardState ts in tsa)
                    {
                        comm.Parameters["@esr"].Value = ts.Esr;
                        comm.Parameters["@total"].Value = ts.Total;
                        comm.Parameters["@damaged"].Value = ts.Damaged;
                        comm.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Не удалось обновить список обратного протокола: " + ex.Message);
                using (MySqlCommand comm = conn.CreateCommand())
                {
                    comm.CommandType = System.Data.CommandType.Text;
                    comm.CommandText = "Update Damaged=127 where 1;";
                    comm.ExecuteNonQuery();
                }
            }
                                 
            Console.WriteLine("Запрашиваю список устройств для проверки соединения");
            using (MySqlCommand comm = conn.CreateCommand())
            {
                comm.CommandType = System.Data.CommandType.Text;
                comm.CommandText = "SELECT `Id`,`Ip`, `Name` FROM `stations` WHERE Ip is not null";
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

                comm.CommandText = "INSERT INTO ping(StationId, Ping, PingStatus) VALUES(@Id, @Ping, @Status) ON DUPLICATE KEY UPDATE Ping = @Ping, PingStatus = @Status;";


                using (MySqlTransaction t = conn.BeginTransaction())
                {
                    comm.Transaction = t;
                    comm.Parameters.Add(new MySqlParameter("@Ping", MySqlDbType.Int32));
                    comm.Parameters.Add(new MySqlParameter("@Status", MySqlDbType.Int32));
                    comm.Parameters.Add(new MySqlParameter("@Id", MySqlDbType.Int32));
                    foreach (PingDevice d in pinglist)
                    {
                        comm.Parameters["@Id"].Value = d.Id;
                        comm.Parameters["@Ping"].Value = d.Ping;
                        comm.Parameters["@Status"].Value = d.Status;
                        comm.ExecuteNonQuery();
                    }
                    t.Commit();
                }
                Console.WriteLine("Изменения зафиксированы");
            }
            conn.Close();
        }
    }
}
