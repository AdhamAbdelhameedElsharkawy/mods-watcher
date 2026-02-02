using ModsAutomator.Data.Interfaces;
using System.Data;

namespace ModsAutomator.Data
{
    public abstract class BaseRepository
    {
        protected readonly IConnectionFactory _connectionFactory;

        protected BaseRepository(IConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        // Shared helper for all concrete repos
        protected async Task<T> ExecuteAsync<T>(
            Func<IDbConnection, IDbTransaction?, Task<T>> action,
            bool requiresTransaction = false,
            IDbConnection? connection = null,
            IDbTransaction? transaction = null)
        {
            bool externalConnection = connection != null;
            connection ??= _connectionFactory.CreateConnection();
            bool ownTransaction = false;

            if (requiresTransaction && transaction == null)
            {
                transaction = connection.BeginTransaction();
                ownTransaction = true;
            }

            try
            {
                var result = await action(connection, transaction);

                if (ownTransaction)
                {
                    transaction.Commit();
                }

                return result;
            }
            catch
            {
                if (ownTransaction)
                {
                    transaction?.Rollback();
                }
                throw;
            }
            finally
            {
                if (!externalConnection)
                {
                    connection?.Dispose();
                }
            }
        }
    }
}
