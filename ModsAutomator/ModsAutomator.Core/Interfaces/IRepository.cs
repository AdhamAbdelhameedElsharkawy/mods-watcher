using System.Data;

namespace ModsWatcher.Core.Interfaces
{
    public interface IRepository<T, TId>
    {
        Task<T?> GetByIdAsync(TId id, IDbConnection? connection = null, IDbTransaction? transaction = null, CancellationToken cancellationToken = default);
        Task<IEnumerable<T>> QueryAllAsync(IDbConnection? connection = null, IDbTransaction? transaction = null, CancellationToken cancellationToken = default);
        Task<T?> InsertAsync(T entity, IDbConnection? connection = null, IDbTransaction? transaction = null, CancellationToken cancellationToken = default);
        Task<T?> UpdateAsync(T entity, IDbConnection? connection = null, IDbTransaction? transaction = null, CancellationToken cancellationToken = default);
        Task<bool> DeleteAsync(TId id, IDbConnection? connection = null, IDbTransaction? transaction = null, CancellationToken cancellationToken = default);
    }
}
