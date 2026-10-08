using System.Linq.Expressions;
using Dapper;
using Microsoft.EntityFrameworkCore;
using TobaccoSaaS.Application.Common.Interfaces;
using TobaccoSaaS.Application.Common.Models;
using TobaccoSaaS.Domain.Common;
using TobaccoSaaS.Infrastructure.Data;

namespace TobaccoSaaS.Infrastructure.Repositories;

/// <summary>
/// The single generic repository (spec §8, AGENTS.md §10). All EF CRUD plus parameterized
/// SQL execution lives here; no per-entity repositories are created.
/// </summary>
public sealed class GenericRepository<T> : IRepository<T> where T : class
{
    private readonly AppDbContext _context;
    private readonly DbSet<T> _set;

    public GenericRepository(AppDbContext context)
    {
        _context = context;
        _set = context.Set<T>();
    }

    public async Task<T?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _set.FirstOrDefaultAsync(e => EF.Property<Guid>(e, "Id") == id, ct);

    public async Task<List<T>> ListAsync(Expression<Func<T, bool>>? predicate = null, CancellationToken ct = default)
    {
        IQueryable<T> query = _set.AsNoTracking();
        if (predicate is not null)
        {
            query = query.Where(predicate);
        }

        return await query.ToListAsync(ct);
    }

    public async Task<PagedResult<T>> PagedAsync(Expression<Func<T, bool>>? predicate, int page, int pageSize, CancellationToken ct = default)
    {
        IQueryable<T> query = _set.AsNoTracking();
        if (predicate is not null)
        {
            query = query.Where(predicate);
        }

        var total = await query.LongCountAsync(ct);
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return new PagedResult<T>(items, page, pageSize, total);
    }

    public Task<bool> AnyAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default)
        => _set.AnyAsync(predicate, ct);

    public Task<int> CountAsync(Expression<Func<T, bool>>? predicate = null, CancellationToken ct = default)
        => predicate is null ? _set.CountAsync(ct) : _set.CountAsync(predicate, ct);

    public async Task AddAsync(T entity, CancellationToken ct = default)
        => await _set.AddAsync(entity, ct);

    public void Update(T entity) => _set.Update(entity);

    public void Remove(T entity)
    {
        if (entity is BaseEntity soft)
        {
            soft.MarkDeleted();
            _set.Update(entity);
            return;
        }

        _set.Remove(entity);
    }

    public async Task<List<TResult>> ExecuteSqlQueryAsync<TResult>(string sql, object? parameters = null, CancellationToken ct = default)
    {
        var connection = _context.Database.GetDbConnection();
        var command = new CommandDefinition(sql, parameters, cancellationToken: ct);
        var rows = await connection.QueryAsync<TResult>(command);
        return rows.AsList();
    }

    public async Task<TResult?> ExecuteSqlSingleAsync<TResult>(string sql, object? parameters = null, CancellationToken ct = default)
    {
        var connection = _context.Database.GetDbConnection();
        var command = new CommandDefinition(sql, parameters, cancellationToken: ct);
        return await connection.QueryFirstOrDefaultAsync<TResult>(command);
    }
}
