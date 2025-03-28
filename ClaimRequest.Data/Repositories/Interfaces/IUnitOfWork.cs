using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace ClaimRequest.DAL.Repositories.Interfaces
{
    public interface IUnitOfWork : IGenericRepositoryFactory, IDisposable
    {

        Task<TOperation> ProcessInTransactionAsync<TOperation>(Func<Task<TOperation>> operation);
        Task ProcessInTransactionAsync(Func<Task> operation);

        int Commit();

        Task<int> CommitAsync();

        Task<IDbContextTransaction> BeginTransactionAsync();


        Task CommitTransactionAsync(IDbContextTransaction transaction);

        Task RollbackTransactionAsync(IDbContextTransaction transaction);
    }

    public interface IUnitOfWork<TContext> : IUnitOfWork where TContext : DbContext
    {
        TContext Context { get; }
    }
}
