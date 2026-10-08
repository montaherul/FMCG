namespace TobaccoSaaS.Application.Common.Interfaces;

/// <summary>Coordinates repositories and transactions (spec §7, AGENTS.md §18).</summary>
public interface IUnitOfWork
{
    IRepository<T> Repository<T>() where T : class;

    Task<int> SaveChangesAsync(CancellationToken ct = default);

    Task<ITransaction> BeginTransactionAsync(CancellationToken ct = default);
}

/// <summary>A database transaction scope. Commit or roll back explicitly.</summary>
public interface ITransaction : IAsyncDisposable
{
    Task CommitAsync(CancellationToken ct = default);

    Task RollbackAsync(CancellationToken ct = default);
}
