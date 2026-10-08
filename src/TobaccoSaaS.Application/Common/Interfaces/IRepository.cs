using System.Linq.Expressions;
using TobaccoSaaS.Application.Common.Models;

namespace TobaccoSaaS.Application.Common.Interfaces;

/// <summary>
/// Generic data-access contract (spec §8 / AGENTS.md §10). Implemented once in Infrastructure.
/// Complex listing/reporting is handled through the parameterized SQL methods, never string concatenation.
/// </summary>
public interface IRepository<T> where T : class
{
    Task<T?> GetByIdAsync(Guid id, CancellationToken ct = default);

    Task<List<T>> ListAsync(Expression<Func<T, bool>>? predicate = null, CancellationToken ct = default);

    Task<PagedResult<T>> PagedAsync(Expression<Func<T, bool>>? predicate, int page, int pageSize, CancellationToken ct = default);

    Task<bool> AnyAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default);

    Task<int> CountAsync(Expression<Func<T, bool>>? predicate = null, CancellationToken ct = default);

    Task AddAsync(T entity, CancellationToken ct = default);

    void Update(T entity);

    void Remove(T entity);

    /// <summary>Parameterized query for complex reads/reporting (spec §11). Never concatenate inputs.</summary>
    Task<List<TResult>> ExecuteSqlQueryAsync<TResult>(string sql, object? parameters = null, CancellationToken ct = default);

    Task<TResult?> ExecuteSqlSingleAsync<TResult>(string sql, object? parameters = null, CancellationToken ct = default);
}
