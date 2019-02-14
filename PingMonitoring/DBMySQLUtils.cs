using MySql.Data.MySqlClient;

namespace PingMonitoring
{
    class DBMySQLUtils
    {

        public static MySqlConnection
                 GetDBConnection(string host, int port, string database, string username, string password)
        {
            // Connection String.
            string connString = "Server=" + host + ";port=" + port + ";Database=" + database
                 + ";Uid=" + username + ";PWD=" + password + ";SslMode = none";

            MySqlConnection conn = new MySqlConnection(connString);

            return conn;
        }

    }
}