using System;
using System.Net.NetworkInformation;

namespace PingMonitoring
{
    public class PingDevice
    {
        private const int MaxCount = 4;
        private const int Timeout = 300;

        public int Id;
        public string Address;
        public string Name;

        public IPStatus Status;
        public int Ping = 0;

        public PingDevice(int Id,string Address,string Name)
        {
            this.Id = Id;
            this.Address = Address;
            this.Name = Name;
            TestConnection();
        }


        public void TestConnection(int Count = 1)
        {
            Console.WriteLine("Опрашиваю узел \"" + Name + "\" [" + Address + "]. Попытка №" + Count);
            try
            {
                PingReply r = Program.PingSender.Send(Address, Timeout*Count,Program.packet);
                if (r.Status != 0)
                {
                    Console.WriteLine("Ошибка: " + r.Status);
                    if (Count == MaxCount)
                    {
                        Console.WriteLine("Превышено максимальное число попыток");
                        Status = r.Status;
                        Ping = (int)r.RoundtripTime;
                    }
                    else
                    {
                        TestConnection(Count += 1);
                    }
                }
                else
                {
                    Status = r.Status;
                    Ping = (int)r.RoundtripTime;
                }
            } catch (Exception e)
            {
                Status = IPStatus.Unknown;
                Console.WriteLine("Ошибка Ping: "+e.Message);
            }
        }
    }
}
