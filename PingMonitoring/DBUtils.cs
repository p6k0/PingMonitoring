using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;

namespace PingMonitoring
{
    class DBUtils
    {
        public static MySqlConnection GetDBConnection(string host, int port, string database, string username, string password)
        {
            return DBMySQLUtils.GetDBConnection(host, port, database, username, password);
        }

    }
}