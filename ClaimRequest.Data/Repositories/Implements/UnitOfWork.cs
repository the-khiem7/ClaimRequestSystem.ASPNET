using System.ComponentModel.DataAnnotations;
using ClaimRequest.DAL.Data.Entities;
using ClaimRequest.DAL.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace ClaimRequest.DAL.Repositories.Implements
{
    public class UnitOfWork<TContext> : IUnitOfWork<TContext> where TContext : ClaimRequestDbContext
    {
        public TContext Context { get; }
        private Dictionary<Type, object> _repositories;

        public UnitOfWork(TContext context)
        {
            Context = context;
        }

        #region Repository Management
        public IGenericRepository<TEntity> GetRepository<TEntity>() where TEntity : class
        {
            _repositories ??= new Dictionary<Type, object>();
            if (_repositories.TryGetValue(typeof(TEntity), out object repository))
            {
                return (IGenericRepository<TEntity>)repository;
            }

            repository = new GenericRepository<TEntity>(Context);
            _repositories.Add(typeof(TEntity), repository);
            return (IGenericRepository<TEntity>)repository;
        }
        #endregion

        #region Packed Transaction Management
        public async Task<TOperation> ProcessInTransactionAsync<TOperation>(Func<Task<TOperation>> operation)
        {
            var executionStrategy = Context.Database.CreateExecutionStrategy();
            return await executionStrategy.ExecuteAsync(async () =>
            {
                await using var transaction = await Context.Database.BeginTransactionAsync();
                try
                {
                    var result = await operation();
                    await Context.SaveChangesAsync();
                    await transaction.CommitAsync();
                    return result;
                }
                catch
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            });
        }

        // Overload for void operation
        public async Task ProcessInTransactionAsync(Func<Task> operation)
        {
            await ProcessInTransactionAsync(async () =>
            {
                await operation();
                return true;
            });
        }


        #endregion

        #region Transaction Management
        public async Task<IDbContextTransaction> BeginTransactionAsync()
        {
            return await Context.Database.BeginTransactionAsync();
        }

        public async Task CommitTransactionAsync(IDbContextTransaction transaction)
        {
            await transaction.CommitAsync();
        }

        public async Task RollbackTransactionAsync(IDbContextTransaction transaction)
        {
            await transaction.RollbackAsync();
        }
        #endregion

        #region IDisposable Implementation
        public void Dispose()
        {
            Context?.Dispose();
            GC.SuppressFinalize(this);
        }
        #endregion

        #region Save Changes
        public int Commit()
        {
            TrackChanges();
            return Context.SaveChanges();
        }

        public async Task<int> CommitAsync()
        {
            TrackChanges();
            return await Context.SaveChangesAsync();
        }
        #endregion

        #region Validation
        private void TrackChanges()
        {
            var validationErrors = Context.ChangeTracker.Entries<IValidatableObject>()
                .SelectMany(e => e.Entity.Validate(null))
                .Where(e => e != ValidationResult.Success)
                .ToArray();
            if (validationErrors.Any())
            {
                var exceptionMessage = string.Join(Environment.NewLine,
                    validationErrors.Select(error => $"Properties {error.MemberNames} Error: {error.ErrorMessage}"));
                throw new ValidationException(exceptionMessage);
            }
        }
        #endregion
    }
}
