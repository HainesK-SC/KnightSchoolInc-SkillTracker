using Capstone.Identity.API.Models.ReadModels;

namespace Capstone.Identity.API.Repositories
{
    public interface IUserQueryRepository
    {
        Task<IReadOnlyList<UserWithProfile>> GetAllWithProfilesAsync();
        Task<UserWithProfile?> GetByIdWithProfileAsync(Guid userId);
    }
}
