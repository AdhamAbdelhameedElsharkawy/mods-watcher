using Microsoft.Data.Sqlite;
using ModsWatcher.Data.Interfaces;
using System;
using System.Collections.Generic;
using System.Data;
using System.Text;

namespace ModsWatcher.Data.Helpers
{
    public class SqliteConnectionFactory : IConnectionFactory
    {

        private readonly string _connectionString = string.Empty;

        public SqliteConnectionFactory(string connectionString)
        {
            this._connectionString = connectionString;
        }

        public IDbConnection CreateConnection()
        {
            SqliteConnection connection = new SqliteConnection(_connectionString);
            connection.Open();
            return connection;
        }
    }
}
