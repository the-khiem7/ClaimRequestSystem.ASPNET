using ClaimRequest.DAL.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using System.ComponentModel.DataAnnotations;

namespace ClaimRequest.DAL.Repositories.Implements
{
    public class UnitOfWork<TContext> : IUnitOfWork<TContext> where TContext : DbContext
    {
        public TContext Context { get; }
        private Dictionary<Type, object> _repositories;
        private IDbContextTransaction? _transaction = null;

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

        #region Transaction Management
        public void BeginTransaction()
        {
            _transaction = Context.Database.BeginTransaction();
        }

        public void CommitTransaction()
        {
            if (_transaction != null)
            {
                _transaction.Commit();
                _transaction.Dispose();
                _transaction = null;
            }
        }

        public void RollbackTransaction()
        {
            if (_transaction != null)
            {
                _transaction.Rollback();
                _transaction.Dispose();
                _transaction = null;
            }
        }
        #endregion

        #region IDisposable Implementation
        public void Dispose()
        {
            _transaction?.Dispose();
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
                throw new Exception(exceptionMessage);
            }
        }
        #endregion
    }
}
