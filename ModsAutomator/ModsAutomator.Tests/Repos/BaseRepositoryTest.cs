using Dapper;
using Microsoft.Data.Sqlite;
using ModsWatcher.Data.Helpers;
using ModsWatcher.Data.Interfaces;
using Moq;

namespace ModsWatcher.Tests.Repos
{
    public abstract class BaseRepositoryTest : IDisposable
    {
        protected readonly SqliteConnection Connection;
        protected readonly Mock<IConnectionFactory> FactoryMock;

        protected BaseRepositoryTest()
        {
            // 1. Setup Connection
            Connection = new SqliteConnection("Data Source=:memory:");
            Connection.Open();

            // 2. Initialize Schema & TypeHandlers using your production code
            SqliteDbInitializer.InitializeAsync(Connection).Wait();

            // 3. Register the DateOnly handler (Crucial for SQLite/Dapper)
            // If you already have this class in your Data project, use it here
            SqlMapper.AddTypeHandler(new DateOnlyTypeHandler());

            // 3. Setup common Mock
            FactoryMock = new Mock<IConnectionFactory>();
        }

        public void Dispose()
        {
            Connection.Close();
            Connection.Dispose();
        }
    }
}