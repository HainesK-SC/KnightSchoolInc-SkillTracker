using Microsoft.EntityFrameworkCore.Storage;

namespace Capstone.Identity.API.Data
{
    public sealed class UnitOfWork : IUnitOfWork
    {
        private readonly AppDbContext _appDbContext;
        
        public UnitOfWork(AppDbContext appDbContext)
        {
            _appDbContext = appDbContext;
        }

        public Task<IDbContextTransaction> BeginTransactionAsync()
        {
            return _appDbContext.Database.BeginTransactionAsync();
        }

        public Task<int> SaveChangesAsync()
        {
            return _appDbContext.SaveChangesAsync();
        }
    }
}
