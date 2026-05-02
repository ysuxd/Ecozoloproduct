using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.SqlClient;
using Npgsql;

namespace WpfApp.Database
{
    public class DatabaseConnection
    {

        private readonly string connectionString = "Host=localhost;port=5432;Database=Ecozoloproduct;Username=postgres;Password=123";

        public NpgsqlConnection GetConnection()
        {
            return new NpgsqlConnection(connectionString);
        }
    }
}