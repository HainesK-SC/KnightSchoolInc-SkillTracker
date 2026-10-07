using Microsoft.EntityFrameworkCore.Storage;

namespace Capstone.Identity.API.Data
{
    public interface IUnitOfWork
    {
        Task<IDbContextTransaction> BeginTransactionAsync();
        Task<int> SaveChangesAsync();
    }
}
