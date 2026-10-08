using Microsoft.EntityFrameworkCore.Storage;
using TobaccoSaaS.Application.Common.Interfaces;
using TobaccoSaaS.Infrastructure.Data;

namespace TobaccoSaaS.Infrastructure.Repositories;

/// <summary>Coordinates repositories and transactions (spec §7, §24). No business logic lives here.</summary>
public sealed class UnitOfWork : IUnitOfWork
{
    private readonly AppDbContext _context;
    private readonly Dictionary<Type, object> _repositories = new();

    public UnitOfWork(AppDbContext context) => _context = context;

    public IRepository<T> Repository<T>() where T : class
    {
        var key = typeof(T);
        if (!_repositories.TryGetValue(key, out var repository))
        {
            repository = new GenericRepository<T>(_context);
            _repositories[key] = repository;
        }

        return (IRepository<T>)repository;
    }

    public Task<int> SaveChangesAsync(CancellationToken ct = default) => _context.SaveChangesAsync(ct);

    public async Task<ITransaction> BeginTransactionAsync(CancellationToken ct = default)
        => new EfTransaction(await _context.Database.BeginTransactionAsync(ct));

    private sealed class EfTransaction : ITransaction
    {
        private readonly IDbContextTransaction _transaction;

        public EfTransaction(IDbContextTransaction transaction) => _transaction = transaction;

        public Task CommitAsync(CancellationToken ct = default) => _transaction.CommitAsync(ct);

        public Task RollbackAsync(CancellationToken ct = default) => _transaction.RollbackAsync(ct);

        public ValueTask DisposeAsync() => _transaction.DisposeAsync();
    }
}
