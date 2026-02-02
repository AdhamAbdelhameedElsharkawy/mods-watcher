using System.Data;

namespace ModsAutomator.Data.Interfaces
{
    public interface IConnectionFactory
    {
        IDbConnection CreateConnection();
    }
}
