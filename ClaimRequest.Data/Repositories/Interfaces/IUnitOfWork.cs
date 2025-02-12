using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClaimRequest.DAL.Repositories.Interfaces
{
    public interface IUnitOfWork : IGenericRepositoryFactory, IDisposable
    {
        int Commit();

        Task<int> CommitAsync();
    }

    public interface IUnitOfWork<TContext> : IUnitOfWork where TContext : DbContext
    {
        TContext Context { get; }
        void BeginTransaction();
        void CommitTransaction();
        void RollbackTransaction();
    }
}
