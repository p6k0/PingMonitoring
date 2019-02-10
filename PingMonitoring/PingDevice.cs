using System;
using System.Net.NetworkInformation;

namespace PingMonitoring
{
    public class PingDevice
    {
        private const int MaxCount = 3;
        private const int Timeout = 1000;

        public int Id;
        public string Address;
        public string Name;

        public IPStatus Status;
        public int Ping = 0;

        public void TestConnection(int Count = 1)
        {
            Console.WriteLine("Опрашиваю узел \"" + Name + "\" [" + Address + "]. Попытка №" + Count);
            try
            {
                PingReply r = Program.PingSender.Send(Address, Timeout);
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
