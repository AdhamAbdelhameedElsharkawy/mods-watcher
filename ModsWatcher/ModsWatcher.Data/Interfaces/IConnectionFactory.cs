using System.Data;

namespace ModsWatcher.Data.Interfaces
{
    public interface IConnectionFactory
    {
        IDbConnection CreateConnection();
    }
}
